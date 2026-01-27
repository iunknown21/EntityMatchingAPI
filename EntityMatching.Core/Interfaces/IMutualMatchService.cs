using EntityMatching.Core.Models.Search;
using EntityMatching.Shared.Models;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for finding mutual matches between entities
    /// Enables bidirectional matching: both entities match each other
    /// </summary>
    public interface IMutualMatchService
    {
        /// <summary>
        /// Find all entities that mutually match the given entity
        /// Returns only matches where BOTH entities match each other above the threshold
        /// </summary>
        /// <param name="entityId">The entity to find mutual matches for</param>
        /// <param name="minSimilarity">Minimum similarity score threshold (0-1, default 0.8)</param>
        /// <param name="targetEntityType">Optional: filter to only match specific entity types</param>
        /// <param name="limit">Maximum number of mutual matches to return</param>
        /// <returns>List of mutual matches with bidirectional scores</returns>
        Task<MutualMatchResult> FindMutualMatchesAsync(
            string entityId,
            float minSimilarity = 0.8f,
            EntityType? targetEntityType = null,
            int limit = 50);
    }
}
