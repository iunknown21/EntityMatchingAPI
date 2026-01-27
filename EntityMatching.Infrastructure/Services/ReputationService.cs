using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Reputation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for managing entity ratings and reputation using Cosmos DB
    /// Provides rating submission, reputation calculation, and querying capabilities
    /// </summary>
    public class ReputationService : IReputationService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseId;
        private readonly string _ratingsContainerId;
        private readonly string _reputationsContainerId;
        private readonly ILogger<ReputationService> _logger;
        private Container? _ratingsContainer;
        private Container? _reputationsContainer;

        /// <summary>
        /// Target number of ratings for full confidence (100%)
        /// Confidence = min(totalRatings / TARGET_RATINGS, 1.0)
        /// </summary>
        private const int TARGET_RATINGS_FOR_CONFIDENCE = 10;

        public ReputationService(
            CosmosClient cosmosClient,
            string databaseId,
            string ratingsContainerId,
            string reputationsContainerId,
            ILogger<ReputationService> logger)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            _databaseId = databaseId ?? throw new ArgumentNullException(nameof(databaseId));
            _ratingsContainerId = ratingsContainerId ?? throw new ArgumentNullException(nameof(ratingsContainerId));
            _reputationsContainerId = reputationsContainerId ?? throw new ArgumentNullException(nameof(reputationsContainerId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize containers on construction
            InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var database = _cosmosClient.GetDatabase(_databaseId);

                // Create ratings container (partition key: /entityId)
                var ratingsProps = new ContainerProperties
                {
                    Id = _ratingsContainerId,
                    PartitionKeyPath = "/entityId"
                };
                var ratingsResponse = await database.CreateContainerIfNotExistsAsync(ratingsProps);
                _ratingsContainer = ratingsResponse.Container;

                // Create reputations container (partition key: /entityId)
                var reputationsProps = new ContainerProperties
                {
                    Id = _reputationsContainerId,
                    PartitionKeyPath = "/entityId"
                };
                var reputationsResponse = await database.CreateContainerIfNotExistsAsync(reputationsProps);
                _reputationsContainer = reputationsResponse.Container;

                _logger.LogInformation("Reputation containers initialized: {RatingsContainer}, {ReputationsContainer}",
                    _ratingsContainerId, _reputationsContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize reputation containers");
                throw;
            }
        }

        public async Task<EntityRating> AddOrUpdateRatingAsync(EntityRating rating)
        {
            rating.LastModified = DateTime.UtcNow;

            // Check if rating already exists
            EntityRating? existing = null;
            try
            {
                var response = await _ratingsContainer!.ReadItemAsync<EntityRating>(
                    rating.Id,
                    new PartitionKey(rating.EntityId));
                existing = response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Rating doesn't exist yet, will create new
            }

            if (existing != null)
            {
                // Update existing
                await _ratingsContainer!.ReplaceItemAsync(rating, rating.Id, new PartitionKey(rating.EntityId));
                _logger.LogInformation("Updated rating {RatingId} for profile {ProfileId}", rating.Id, rating.EntityId);
            }
            else
            {
                // Create new
                rating.CreatedAt = DateTime.UtcNow;
                await _ratingsContainer!.CreateItemAsync(rating, new PartitionKey(rating.EntityId));
                _logger.LogInformation("Created rating {RatingId} for profile {ProfileId} by {RatedBy}",
                    rating.Id, rating.EntityId, rating.RatedByEntityId);
            }

            // Recalculate reputation
            await RecalculateReputationAsync(rating.EntityId);

            return rating;
        }

        public async Task<IEnumerable<EntityRating>> GetRatingsForEntityAsync(string entityId, bool includePrivate = false)
        {
            var query = includePrivate
                ? "SELECT * FROM c WHERE c.entityId = @entityId"
                : "SELECT * FROM c WHERE c.entityId = @entityId AND c.isPublic = true";

            var queryDef = new QueryDefinition(query)
                .WithParameter("@entityId", entityId);

            var iterator = _ratingsContainer!.GetItemQueryIterator<EntityRating>(queryDef);
            var ratings = new List<EntityRating>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                ratings.AddRange(response);
            }

            return ratings;
        }

        public async Task<EntityRating?> GetRatingAsync(string ratingId)
        {
            try
            {
                // Note: We don't know the partition key, so query by ID
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", ratingId);

                var iterator = _ratingsContainer!.GetItemQueryIterator<EntityRating>(query);
                var response = await iterator.ReadNextAsync();

                return response.FirstOrDefault();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task DeleteRatingAsync(string ratingId)
        {
            // First get the rating to find the entityId (partition key)
            var rating = await GetRatingAsync(ratingId);
            if (rating == null)
            {
                _logger.LogWarning("Rating {RatingId} not found for deletion", ratingId);
                return;
            }

            await _ratingsContainer!.DeleteItemAsync<EntityRating>(ratingId, new PartitionKey(rating.EntityId));
            _logger.LogInformation("Deleted rating {RatingId} for profile {ProfileId}", ratingId, rating.EntityId);

            // Recalculate reputation
            await RecalculateReputationAsync(rating.EntityId);
        }

        public async Task<EntityReputation?> GetReputationAsync(string entityId, bool forceRecalculate = false)
        {
            if (forceRecalculate)
            {
                return await RecalculateReputationAsync(entityId);
            }

            try
            {
                // Try to get cached reputation
                var response = await _reputationsContainer!.ReadItemAsync<EntityReputation>(
                    entityId,
                    new PartitionKey(entityId));

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // No cached reputation, calculate it
                return await RecalculateReputationAsync(entityId);
            }
        }

        public async Task<EntityReputation> RecalculateReputationAsync(string entityId)
        {
            // Get all ratings for this profile
            var allRatings = (await GetRatingsForEntityAsync(entityId, includePrivate: true)).ToList();

            if (!allRatings.Any())
            {
                // No ratings yet - return default reputation
                var defaultReputation = new EntityReputation
                {
                    Id = entityId,
                    EntityId = entityId,
                    OverallScore = 0,
                    TotalRatings = 0,
                    VerifiedRatings = 0,
                    VerifiedScore = null,
                    CategoryScores = new List<CategoryReputation>(),
                    ConfidenceScore = 0,
                    LastCalculated = DateTime.UtcNow
                };

                return defaultReputation;
            }

            // Calculate overall score
            var overallScore = allRatings.Average(r => r.OverallRating);

            // Calculate verified score
            var verifiedRatings = allRatings.Where(r => r.IsVerified).ToList();
            var verifiedScore = verifiedRatings.Any() ? verifiedRatings.Average(r => r.OverallRating) : (double?)null;

            // Calculate category scores
            var categoryScores = CalculateCategoryScores(allRatings);

            // Calculate confidence score
            var confidenceScore = Math.Min((double)allRatings.Count / TARGET_RATINGS_FOR_CONFIDENCE, 1.0);

            var reputation = new EntityReputation
            {
                Id = entityId,
                EntityId = entityId,
                OverallScore = overallScore,
                TotalRatings = allRatings.Count,
                VerifiedRatings = verifiedRatings.Count,
                VerifiedScore = verifiedScore,
                CategoryScores = categoryScores,
                ConfidenceScore = confidenceScore,
                LastCalculated = DateTime.UtcNow
            };

            // Save to database
            try
            {
                await _reputationsContainer!.UpsertItemAsync(reputation, new PartitionKey(entityId));
                _logger.LogInformation(
                    "Recalculated reputation for profile {ProfileId}: {OverallScore:F2} from {TotalRatings} ratings",
                    entityId, overallScore, allRatings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save reputation for profile {ProfileId}", entityId);
                throw;
            }

            return reputation;
        }

        private List<CategoryReputation> CalculateCategoryScores(List<EntityRating> ratings)
        {
            // Collect all category ratings across all ratings
            var categoryData = new Dictionary<string, List<double>>();

            foreach (var rating in ratings)
            {
                if (rating.CategoryRatings != null)
                {
                    foreach (var kvp in rating.CategoryRatings)
                    {
                        if (!categoryData.ContainsKey(kvp.Key))
                        {
                            categoryData[kvp.Key] = new List<double>();
                        }
                        categoryData[kvp.Key].Add(kvp.Value);
                    }
                }
            }

            // Calculate average for each category
            return categoryData.Select(kvp => new CategoryReputation
            {
                Category = kvp.Key,
                Score = kvp.Value.Average(),
                Count = kvp.Value.Count
            }).ToList();
        }
    }
}
