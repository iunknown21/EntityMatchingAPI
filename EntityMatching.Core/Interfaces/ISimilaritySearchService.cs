using EntityMatching.Core.Models.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for finding similar entities using vector similarity search
    /// Supports hybrid search: semantic similarity + structured attribute filtering + metadata filtering
    /// </summary>
    public interface ISimilaritySearchService
    {
        /// <summary>
        /// Find entities similar to a given entity based on embedding vectors
        /// Supports optional attribute and metadata filtering with privacy enforcement
        /// </summary>
        /// <param name="entityId">The reference entity ID to find matches for</param>
        /// <param name="limit">Maximum number of results to return (default: 10)</param>
        /// <param name="minSimilarity">Minimum similarity score threshold 0-1 (default: 0.5)</param>
        /// <param name="includeEntities">Whether to populate full Entity objects in results (default: false)</param>
        /// <param name="attributeFilters">Optional structured filters to apply (e.g., gender, pets, education)</param>
        /// <param name="metadataFilters">Optional metadata key-value filters (e.g., {"trust_score": 0.85})</param>
        /// <param name="requestingUserId">User ID of requester for privacy enforcement (null = anonymous)</param>
        /// <param name="enforcePrivacy">Whether to enforce field visibility rules (default: true)</param>
        /// <returns>SearchResult with ranked matches and metadata</returns>
        Task<SearchResult> FindSimilarEntitiesAsync(
            string entityId,
            int limit = 10,
            float minSimilarity = 0.5f,
            bool includeEntities = false,
            FilterGroup? attributeFilters = null,
            Dictionary<string, object>? metadataFilters = null,
            string? requestingUserId = null,
            bool enforcePrivacy = true);

        /// <summary>
        /// Search for entities similar to a text query using embedding-based semantic search
        /// Supports optional attribute and metadata filtering with privacy enforcement
        /// Two-phase filtering: (1) semantic similarity candidates, (2) attribute/metadata filter evaluation
        /// </summary>
        /// <param name="query">Text query to search for (will be embedded using same model as entities)</param>
        /// <param name="limit">Maximum number of results to return (default: 10)</param>
        /// <param name="minSimilarity">Minimum similarity score threshold 0-1 (default: 0.5)</param>
        /// <param name="includeEntities">Whether to populate full Entity objects in results (default: false)</param>
        /// <param name="attributeFilters">Optional structured filters to apply (e.g., gender, pets, education)</param>
        /// <param name="metadataFilters">Optional metadata key-value filters (e.g., {"trust_score": 0.85})</param>
        /// <param name="requestingUserId">User ID of requester for privacy enforcement (null = anonymous)</param>
        /// <param name="enforcePrivacy">Whether to enforce field visibility rules (default: true)</param>
        /// <returns>SearchResult with ranked matches and metadata</returns>
        Task<SearchResult> SearchByQueryAsync(
            string query,
            int limit = 10,
            float minSimilarity = 0.5f,
            bool includeEntities = false,
            FilterGroup? attributeFilters = null,
            Dictionary<string, object>? metadataFilters = null,
            string? requestingUserId = null,
            bool enforcePrivacy = true);
    }
}
