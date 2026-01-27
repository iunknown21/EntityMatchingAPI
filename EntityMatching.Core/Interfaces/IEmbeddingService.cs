using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for generating vector embeddings from text
    /// Placeholder interface until embedding provider is chosen
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generate a vector embedding from text
        /// </summary>
        /// <param name="text">The text to embed</param>
        /// <returns>Vector embedding as float array, or null if not implemented</returns>
        Task<float[]?> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Get the name of the embedding model being used
        /// </summary>
        string ModelName { get; }

        /// <summary>
        /// Get the dimensionality of the embeddings produced
        /// </summary>
        int? Dimensions { get; }
    }
}
