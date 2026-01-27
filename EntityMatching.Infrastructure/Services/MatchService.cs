using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Matching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for managing match requests and connections
    /// Implements status validation and lifecycle management
    /// </summary>
    public class MatchService : IMatchService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseId;
        private readonly string _containerId;
        private readonly ILogger<MatchService> _logger;
        private Container? _container;

        public MatchService(
            CosmosClient cosmosClient,
            string databaseId,
            string containerId,
            ILogger<MatchService> logger)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            _databaseId = databaseId ?? throw new ArgumentNullException(nameof(databaseId));
            _containerId = containerId ?? throw new ArgumentNullException(nameof(containerId));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize container on construction
            InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var database = _cosmosClient.GetDatabase(_databaseId);

                // Create matches container (partition key: /targetId for efficient incoming request queries)
                var containerProps = new ContainerProperties
                {
                    Id = _containerId,
                    PartitionKeyPath = "/targetId"
                };
                var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProps);
                _container = containerResponse.Container;

                _logger.LogInformation("Match requests container initialized: {ContainerId}", _containerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize match requests container");
                throw;
            }
        }

        public async Task<MatchRequest> CreateMatchRequestAsync(MatchRequest request)
        {
            request.CreatedAt = DateTime.UtcNow;
            request.LastStatusChangeAt = DateTime.UtcNow;
            request.Status = MatchStatus.Pending;

            await _container!.CreateItemAsync(request, new PartitionKey(request.TargetId));
            _logger.LogInformation("Created match request {RequestId} from {RequesterId} to {TargetId}",
                request.Id, request.RequesterId, request.TargetId);

            return request;
        }

        public async Task<MatchRequest> UpdateMatchStatusAsync(string requestId, MatchStatus newStatus, string? responseMessage = null)
        {
            // Get current request
            var request = await GetMatchRequestAsync(requestId);
            if (request == null)
            {
                throw new InvalidOperationException($"Match request {requestId} not found");
            }

            // Validate status transition
            ValidateStatusTransition(request.Status, newStatus);

            // Update status
            var oldStatus = request.Status;
            request.Status = newStatus;
            request.LastStatusChangeAt = DateTime.UtcNow;

            // Set viewedAt timestamp if transitioning to Viewed or later (and not already set)
            if (request.ViewedAt == null &&
                (newStatus == MatchStatus.Viewed ||
                 newStatus == MatchStatus.Interested ||
                 newStatus == MatchStatus.Declined ||
                 newStatus == MatchStatus.Connected))
            {
                request.ViewedAt = DateTime.UtcNow;
            }

            // Set response message if provided
            if (!string.IsNullOrEmpty(responseMessage))
            {
                request.ResponseMessage = responseMessage;
            }

            await _container!.ReplaceItemAsync(request, request.Id, new PartitionKey(request.TargetId));
            _logger.LogInformation(
                "Updated match request {RequestId} status: {OldStatus} → {NewStatus}",
                requestId, oldStatus, newStatus);

            return request;
        }

        public async Task<MatchRequest?> GetMatchRequestAsync(string requestId)
        {
            try
            {
                // Query by ID (don't know partition key)
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", requestId);

                var iterator = _container!.GetItemQueryIterator<MatchRequest>(query);
                var response = await iterator.ReadNextAsync();

                return response.FirstOrDefault();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<MatchRequest>> GetIncomingMatchRequestsAsync(string targetId, bool includeResolved = false)
        {
            var query = includeResolved
                ? "SELECT * FROM c WHERE c.targetId = @targetId ORDER BY c.createdAt DESC"
                : "SELECT * FROM c WHERE c.targetId = @targetId AND (c.status = 0 OR c.status = 1 OR c.status = 2) ORDER BY c.createdAt DESC";

            var queryDef = new QueryDefinition(query)
                .WithParameter("@targetId", targetId);

            var iterator = _container!.GetItemQueryIterator<MatchRequest>(queryDef);
            var requests = new List<MatchRequest>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                requests.AddRange(response);
            }

            return requests;
        }

        public async Task<IEnumerable<MatchRequest>> GetOutgoingMatchRequestsAsync(string requesterId, bool includeResolved = false)
        {
            var query = includeResolved
                ? "SELECT * FROM c WHERE c.requesterId = @requesterId ORDER BY c.createdAt DESC"
                : "SELECT * FROM c WHERE c.requesterId = @requesterId AND (c.status = 0 OR c.status = 1 OR c.status = 2) ORDER BY c.createdAt DESC";

            var queryDef = new QueryDefinition(query)
                .WithParameter("@requesterId", requesterId);

            var iterator = _container!.GetItemQueryIterator<MatchRequest>(queryDef);
            var requests = new List<MatchRequest>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                requests.AddRange(response);
            }

            return requests;
        }

        public async Task<int> ExpireOldMatchRequestsAsync()
        {
            // Find all active requests that have passed their expiration date
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.expiresAt != null AND c.expiresAt < @now AND (c.status = 0 OR c.status = 1 OR c.status = 2)")
                .WithParameter("@now", DateTime.UtcNow);

            var iterator = _container!.GetItemQueryIterator<MatchRequest>(query);
            var expiredRequests = new List<MatchRequest>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                expiredRequests.AddRange(response);
            }

            // Update each expired request
            var expiredCount = 0;
            foreach (var request in expiredRequests)
            {
                try
                {
                    request.Status = MatchStatus.Expired;
                    request.LastStatusChangeAt = DateTime.UtcNow;

                    await _container!.ReplaceItemAsync(request, request.Id, new PartitionKey(request.TargetId));
                    expiredCount++;

                    _logger.LogInformation("Expired match request {RequestId} (expired at {ExpiresAt})",
                        request.Id, request.ExpiresAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to expire match request {RequestId}", request.Id);
                }
            }

            if (expiredCount > 0)
            {
                _logger.LogInformation("Expired {Count} match requests", expiredCount);
            }

            return expiredCount;
        }

        /// <summary>
        /// Validate that a status transition is allowed
        /// Implements the state machine rules
        /// </summary>
        private void ValidateStatusTransition(MatchStatus currentStatus, MatchStatus newStatus)
        {
            // Allow same status (no-op)
            if (currentStatus == newStatus)
            {
                return;
            }

            // Terminal states cannot transition to anything
            if (currentStatus == MatchStatus.Connected ||
                currentStatus == MatchStatus.Declined ||
                currentStatus == MatchStatus.Expired ||
                currentStatus == MatchStatus.Withdrawn)
            {
                throw new InvalidOperationException(
                    $"Cannot transition from terminal state {currentStatus} to {newStatus}");
            }

            // Validate allowed transitions
            var allowedTransitions = currentStatus switch
            {
                MatchStatus.Pending => new[] { MatchStatus.Viewed, MatchStatus.Interested, MatchStatus.Declined, MatchStatus.Withdrawn, MatchStatus.Expired },
                MatchStatus.Viewed => new[] { MatchStatus.Interested, MatchStatus.Declined, MatchStatus.Withdrawn, MatchStatus.Expired },
                MatchStatus.Interested => new[] { MatchStatus.Connected, MatchStatus.Declined, MatchStatus.Withdrawn, MatchStatus.Expired },
                _ => Array.Empty<MatchStatus>()
            };

            if (!allowedTransitions.Contains(newStatus))
            {
                throw new InvalidOperationException(
                    $"Invalid status transition: {currentStatus} → {newStatus}. " +
                    $"Allowed transitions: {string.Join(", ", allowedTransitions)}");
            }
        }
    }
}
