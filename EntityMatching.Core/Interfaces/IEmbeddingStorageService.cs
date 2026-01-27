using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Embedding;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for managing profile embeddings in Cosmos DB
    /// </summary>
    public interface IEmbeddingStorageService
    {
        /// <summary>
        /// Get an embedding document by profile ID
        /// </summary>
        Task<EntityEmbedding?> GetEmbeddingAsync(string profileId);

        /// <summary>
        /// Create or update an embedding document
        /// </summary>
        Task<EntityEmbedding> UpsertEmbeddingAsync(EntityEmbedding embedding);

        /// <summary>
        /// Delete an embedding document
        /// </summary>
        Task DeleteEmbeddingAsync(string profileId);

        /// <summary>
        /// Get all embeddings with a specific status
        /// </summary>
        /// <param name="status">The embedding status to filter by</param>
        /// <param name="limit">Optional maximum number of embeddings to return</param>
        Task<List<EntityEmbedding>> GetEmbeddingsByStatusAsync(EmbeddingStatus status, int? limit = null);

        /// <summary>
        /// Get count of embeddings by status
        /// </summary>
        Task<Dictionary<EmbeddingStatus, int>> GetEmbeddingCountsByStatusAsync();
    }
}
