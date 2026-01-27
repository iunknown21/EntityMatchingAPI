using EntityMatching.Core.Models.Reputation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for managing entity ratings and reputation
    /// Supports adding ratings, calculating reputation scores, and querying reputation data
    /// </summary>
    public interface IReputationService
    {
        /// <summary>
        /// Add or update a rating for an entity
        /// Automatically recalculates reputation after rating is saved
        /// </summary>
        /// <param name="rating">The rating to add or update</param>
        /// <returns>The saved rating</returns>
        Task<EntityRating> AddOrUpdateRatingAsync(EntityRating rating);

        /// <summary>
        /// Get all ratings for a specific entity
        /// </summary>
        /// <param name="entityId">ID of the entity to get ratings for</param>
        /// <param name="includePrivate">Whether to include private ratings (default: false)</param>
        /// <returns>List of ratings</returns>
        Task<IEnumerable<EntityRating>> GetRatingsForEntityAsync(string entityId, bool includePrivate = false);

        /// <summary>
        /// Get a specific rating by ID
        /// </summary>
        /// <param name="ratingId">Rating ID</param>
        /// <returns>The rating or null if not found</returns>
        Task<EntityRating?> GetRatingAsync(string ratingId);

        /// <summary>
        /// Delete a rating
        /// Automatically recalculates reputation after deletion
        /// </summary>
        /// <param name="ratingId">ID of the rating to delete</param>
        Task DeleteRatingAsync(string ratingId);

        /// <summary>
        /// Get the calculated reputation for an entity
        /// Returns cached reputation if recently calculated, otherwise recalculates
        /// </summary>
        /// <param name="entityId">Entity ID</param>
        /// <param name="forceRecalculate">Force recalculation even if cached (default: false)</param>
        /// <returns>Entity reputation or null if no ratings exist</returns>
        Task<EntityReputation?> GetReputationAsync(string entityId, bool forceRecalculate = false);

        /// <summary>
        /// Recalculate reputation for an entity from all ratings
        /// Called automatically when ratings are added/updated/deleted
        /// Can also be called manually to refresh reputation
        /// </summary>
        /// <param name="entityId">Entity ID</param>
        /// <returns>Calculated reputation</returns>
        Task<EntityReputation> RecalculateReputationAsync(string entityId);

        /// <summary>
        /// Initialize the storage containers if needed
        /// </summary>
        Task InitializeAsync();
    }
}
