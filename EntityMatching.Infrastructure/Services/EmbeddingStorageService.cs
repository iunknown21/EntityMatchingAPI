using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for managing profile embeddings in Cosmos DB
    /// </summary>
    public class EmbeddingStorageService : IEmbeddingStorageService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _embeddingsContainer;
        private readonly ILogger<EmbeddingStorageService> _logger;
        private readonly string _databaseId;
        private readonly string _embeddingsContainerId;

        public EmbeddingStorageService(
            CosmosClient cosmosClient,
            IConfiguration configuration,
            ILogger<EmbeddingStorageService> logger)
        {
            _cosmosClient = cosmosClient;
            _logger = logger;

            _databaseId = configuration["CosmosDb:DatabaseId"];
            _embeddingsContainerId = configuration["CosmosDb:EmbeddingsContainerId"] ?? "embeddings";

            var database = _cosmosClient.GetDatabase(_databaseId);
            _embeddingsContainer = database.GetContainer(_embeddingsContainerId);

            // Initialize container (will create if doesn't exist)
            InitializeAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var database = _cosmosClient.GetDatabase(_databaseId);

                var containerProperties = new ContainerProperties
                {
                    Id = _embeddingsContainerId,
                    PartitionKeyPath = "/id"  // CRITICAL: Must match existing container partition key
                };

                // Serverless mode - no throughput parameter
                await database.CreateContainerIfNotExistsAsync(containerProperties);

                _logger.LogInformation("Embeddings container initialized: {ContainerName}", _embeddingsContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing embeddings container");
                throw;
            }
        }

        public async Task<EntityEmbedding?> GetEmbeddingAsync(string EntityId)
        {
            try
            {
                var id = EntityEmbedding.GenerateId(EntityId);
                var response = await _embeddingsContainer.ReadItemAsync<EntityEmbedding>(
                    id,
                    new PartitionKey(id)
                );

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding for profile {EntityId}", EntityId);
                throw;
            }
        }

        public async Task<EntityEmbedding> UpsertEmbeddingAsync(EntityEmbedding embedding)
        {
            try
            {
                var response = await _embeddingsContainer.UpsertItemAsync(
                    embedding,
                    new PartitionKey(embedding.Id)
                );

                _logger.LogInformation("Upserted embedding for profile {EntityId} with status {Status}",
                    embedding.EntityId, embedding.Status);

                return response.Resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting embedding for profile {EntityId}", embedding.EntityId);
                throw;
            }
        }

        public async Task DeleteEmbeddingAsync(string EntityId)
        {
            try
            {
                var id = EntityEmbedding.GenerateId(EntityId);
                await _embeddingsContainer.DeleteItemAsync<EntityEmbedding>(
                    id,
                    new PartitionKey(id)
                );

                _logger.LogInformation("Deleted embedding for profile {EntityId}", EntityId);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Embedding not found for profile {EntityId}", EntityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting embedding for profile {EntityId}", EntityId);
                throw;
            }
        }

        public async Task<List<EntityEmbedding>> GetEmbeddingsByStatusAsync(EmbeddingStatus status, int? limit = null)
        {
            try
            {
                var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.status = @status"
                ).WithParameter("@status", (int)status);

                var queryRequestOptions = new QueryRequestOptions();
                if (limit.HasValue && limit.Value > 0)
                {
                    queryRequestOptions.MaxItemCount = limit.Value;
                }

                var iterator = _embeddingsContainer.GetItemQueryIterator<EntityEmbedding>(query, requestOptions: queryRequestOptions);
                var results = new List<EntityEmbedding>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);

                    // If we have a limit and we've reached it, stop
                    if (limit.HasValue && limit.Value > 0 && results.Count >= limit.Value)
                    {
                        break;
                    }
                }

                // Trim to exact limit if we exceeded it
                if (limit.HasValue && limit.Value > 0 && results.Count > limit.Value)
                {
                    results = results.Take(limit.Value).ToList();
                }

                _logger.LogInformation("Found {Count} embeddings with status {Status} (limit: {Limit})",
                    results.Count, status, limit?.ToString() ?? "none");

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying embeddings by status {Status}", status);
                throw;
            }
        }

        public async Task<Dictionary<EmbeddingStatus, int>> GetEmbeddingCountsByStatusAsync()
        {
            try
            {
                var counts = new Dictionary<EmbeddingStatus, int>();

                foreach (EmbeddingStatus status in Enum.GetValues(typeof(EmbeddingStatus)))
                {
                    var query = new QueryDefinition(
                        "SELECT VALUE COUNT(1) FROM c WHERE c.status = @status"
                    ).WithParameter("@status", (int)status);

                    var iterator = _embeddingsContainer.GetItemQueryIterator<int>(query);

                    if (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        counts[status] = response.FirstOrDefault();
                    }
                    else
                    {
                        counts[status] = 0;
                    }
                }

                return counts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding counts by status");
                throw;
            }
        }
    }
}
