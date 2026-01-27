using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for managing entities (Person, Job, Property, Career, Major, etc.) in Cosmos DB
    /// Universal service that works across all entity types
    /// </summary>
    public class EntityService : IEntityService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseId;
        private readonly string _containerId;
        private readonly ILogger<EntityService> _logger;
        private Container? _container;

        public EntityService(
            CosmosClient cosmosClient,
            string databaseId,
            string containerId,
            ILogger<EntityService> logger)
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

                var containerProperties = new ContainerProperties
                {
                    Id = _containerId,
                    PartitionKeyPath = "/id"
                };

                // CRITICAL: Serverless mode - no throughput parameter
                var containerResponse = await database.CreateContainerIfNotExistsAsync(containerProperties);
                _container = containerResponse.Container;

                _logger.LogInformation("Entity container initialized: {ContainerId}", _containerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize entity container");
                throw;
            }
        }

        public async Task<Entity?> GetEntityAsync(string id)
        {
            try
            {
                var response = await _container!.ReadItemAsync<Entity>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<Entity?> GetEntityAsync(string id, string userId)
        {
            var entity = await GetEntityAsync(id);

            // Verify ownership
            if (entity != null && entity.OwnedByUserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to access entity {EntityId} owned by {OwnerId}",
                    userId, id, entity.OwnedByUserId);
                return null;
            }

            return entity;
        }

        public async Task<IEnumerable<Entity>> GetAllEntitiesAsync()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = _container!.GetItemQueryIterator<Entity>(query);

            var results = new List<Entity>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<IEnumerable<Entity>> GetAllEntitiesAsync(string userId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ownedByUserId = @userId")
                .WithParameter("@userId", userId);

            var iterator = _container!.GetItemQueryIterator<Entity>(query);

            var results = new List<Entity>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<IEnumerable<Entity>> GetEntitiesByTypeAsync(EntityType entityType)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.entityType = @entityType")
                .WithParameter("@entityType", (int)entityType);

            var iterator = _container!.GetItemQueryIterator<Entity>(query);

            var results = new List<Entity>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<IEnumerable<Entity>> GetEntitiesByIdsAsync(IEnumerable<string> entityIds)
        {
            var results = new List<Entity>();

            foreach (var id in entityIds)
            {
                var entity = await GetEntityAsync(id);
                if (entity != null)
                {
                    results.Add(entity);
                }
            }

            return results;
        }

        public async Task AddEntityAsync(Entity entity)
        {
            entity.LastModified = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;

            await _container!.CreateItemAsync(entity, new PartitionKey(entity.Id.ToString()));
            _logger.LogInformation("Created entity {EntityId} (Type: {EntityType}) for user {UserId}",
                entity.Id, entity.EntityType, entity.OwnedByUserId);
        }

        public async Task UpdateEntityAsync(Entity entity)
        {
            entity.LastModified = DateTime.UtcNow;

            await _container!.ReplaceItemAsync(entity, entity.Id.ToString(), new PartitionKey(entity.Id.ToString()));
            _logger.LogInformation("Updated entity {EntityId} (Type: {EntityType})", entity.Id, entity.EntityType);
        }

        public async Task<Entity> UpdateEntityMetadataAsync(string id, Dictionary<string, object> metadata)
        {
            var entity = await GetEntityAsync(id);
            if (entity == null)
            {
                throw new InvalidOperationException($"Entity {id} not found");
            }

            // Initialize metadata if it doesn't exist
            if (entity.Metadata == null)
            {
                entity.Metadata = new Dictionary<string, object>();
            }

            // Deep merge metadata
            entity.Metadata = MergeMetadata(entity.Metadata, metadata);
            entity.LastModified = DateTime.UtcNow;

            await _container!.ReplaceItemAsync(entity, entity.Id.ToString(), new PartitionKey(entity.Id.ToString()));
            _logger.LogInformation("Updated metadata for entity {EntityId}", entity.Id);

            return entity;
        }

        public async Task<Dictionary<string, object>> GetEntityMetadataAsync(string id)
        {
            var entity = await GetEntityAsync(id);
            return entity?.Metadata ?? new Dictionary<string, object>();
        }

        private Dictionary<string, object> MergeMetadata(Dictionary<string, object> existing, Dictionary<string, object> updates)
        {
            var merged = new Dictionary<string, object>(existing);

            foreach (var kvp in updates)
            {
                if (kvp.Value == null)
                {
                    // Null value means delete the key
                    merged.Remove(kvp.Key);
                }
                else if (merged.ContainsKey(kvp.Key) &&
                         merged[kvp.Key] is Dictionary<string, object> existingDict &&
                         kvp.Value is Dictionary<string, object> updateDict)
                {
                    // Deep merge nested dictionaries
                    merged[kvp.Key] = MergeMetadata(existingDict, updateDict);
                }
                else
                {
                    // Replace or add the value
                    merged[kvp.Key] = kvp.Value;
                }
            }

            return merged;
        }

        public async Task DeleteEntityAsync(string id)
        {
            await _container!.DeleteItemAsync<Entity>(id, new PartitionKey(id));
            _logger.LogInformation("Deleted entity {EntityId}", id);
        }

        public async Task<IEnumerable<Entity>> SearchEntitiesAsync(string searchTerm)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE CONTAINS(LOWER(c.name), @searchTerm)")
                .WithParameter("@searchTerm", searchTerm.ToLower());

            var iterator = _container!.GetItemQueryIterator<Entity>(query);

            var results = new List<Entity>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<IEnumerable<Entity>> SearchEntitiesAsync(string searchTerm, EntityType entityType)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE CONTAINS(LOWER(c.name), @searchTerm) AND c.entityType = @entityType")
                .WithParameter("@searchTerm", searchTerm.ToLower())
                .WithParameter("@entityType", (int)entityType);

            var iterator = _container!.GetItemQueryIterator<Entity>(query);

            var results = new List<Entity>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
    }
}
