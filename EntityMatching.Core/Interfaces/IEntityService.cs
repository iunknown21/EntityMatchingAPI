using EntityMatching.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for managing entities (Person, Job, Property, Career, Major, etc.)
    /// Universal service that works across all entity types
    /// </summary>
    public interface IEntityService
    {
        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        Task<Entity?> GetEntityAsync(string id);

        /// <summary>
        /// Gets an entity by its ID for a specific user
        /// </summary>
        Task<Entity?> GetEntityAsync(string id, string userId);

        /// <summary>
        /// Gets all entities
        /// </summary>
        Task<IEnumerable<Entity>> GetAllEntitiesAsync();

        /// <summary>
        /// Gets all entities for a specific user
        /// </summary>
        Task<IEnumerable<Entity>> GetAllEntitiesAsync(string userId);

        /// <summary>
        /// Gets all entities of a specific type
        /// </summary>
        Task<IEnumerable<Entity>> GetEntitiesByTypeAsync(EntityType entityType);

        /// <summary>
        /// Gets multiple entities by their IDs in a single batch operation
        /// </summary>
        Task<IEnumerable<Entity>> GetEntitiesByIdsAsync(IEnumerable<string> entityIds);

        /// <summary>
        /// Adds a new entity
        /// </summary>
        Task AddEntityAsync(Entity entity);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        Task UpdateEntityAsync(Entity entity);

        /// <summary>
        /// Updates entity metadata without replacing the entire entity
        /// Performs a deep merge of metadata keys
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="metadata">Metadata updates to merge</param>
        /// <returns>Updated entity</returns>
        Task<Entity> UpdateEntityMetadataAsync(string id, Dictionary<string, object> metadata);

        /// <summary>
        /// Gets entity metadata
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Entity metadata or empty dictionary if none exists</returns>
        Task<Dictionary<string, object>> GetEntityMetadataAsync(string id);

        /// <summary>
        /// Deletes an entity by its ID
        /// </summary>
        Task DeleteEntityAsync(string id);

        /// <summary>
        /// Searches for entities by name or other criteria
        /// </summary>
        Task<IEnumerable<Entity>> SearchEntitiesAsync(string searchTerm);

        /// <summary>
        /// Searches for entities by name and type
        /// </summary>
        Task<IEnumerable<Entity>> SearchEntitiesAsync(string searchTerm, EntityType entityType);

        /// <summary>
        /// Initializes the storage if needed (for Cosmos DB container creation)
        /// </summary>
        Task InitializeAsync();
    }
}
