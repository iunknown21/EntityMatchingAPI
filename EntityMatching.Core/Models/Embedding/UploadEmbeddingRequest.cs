using Newtonsoft.Json;
using System;

namespace EntityMatching.Core.Models.Embedding
{
    /// <summary>
    /// Request model for uploading pre-computed client-side embeddings
    /// Privacy-first: Client generates embeddings locally, uploads only vectors
    /// </summary>
    public class UploadEmbeddingRequest
    {
        /// <summary>
        /// Pre-computed embedding vector (must be 1536 dimensions for OpenAI text-embedding-3-small)
        /// </summary>
        [JsonProperty(PropertyName = "embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Name of the embedding model used by client (e.g., "text-embedding-3-small")
        /// Optional - defaults to "text-embedding-3-small"
        /// </summary>
        [JsonProperty(PropertyName = "embeddingModel")]
        public string? EmbeddingModel { get; set; } = "text-embedding-3-small";

        /// <summary>
        /// Optional metadata about client-side processing
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public ClientEmbeddingMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Metadata about client-side embedding generation
    /// </summary>
    public class ClientEmbeddingMetadata
    {
        /// <summary>
        /// Timestamp when client generated the embedding
        /// </summary>
        [JsonProperty(PropertyName = "generatedAt")]
        public DateTime? GeneratedAt { get; set; }

        /// <summary>
        /// Client version/identifier for debugging
        /// </summary>
        [JsonProperty(PropertyName = "clientVersion")]
        public string? ClientVersion { get; set; }
    }
}
