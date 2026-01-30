using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EntityMatching.Core.Models.Embedding
{
    /// <summary>
    /// Status of the embedding generation process
    /// </summary>
    public enum EmbeddingStatus
    {
        Pending,   // Summary generated, waiting for embedding
        Generated, // Embedding successfully generated
        Failed     // Embedding generation failed
    }

    /// <summary>
    /// Stores entity embeddings for semantic search and matching
    /// Cosmos DB Container: embeddings (partition key: /id)
    /// </summary>
    public class EntityEmbedding
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = "";

        [JsonProperty(PropertyName = "entityId")]
        public string EntityId { get; set; } = "";

        /// <summary>
        /// Vector embedding of the entity summary (null until embedding provider is chosen)
        /// </summary>
        [JsonProperty(PropertyName = "embedding")]
        public float[]? Embedding { get; set; } = null;

        /// <summary>
        /// Name of the embedding model used (e.g., "text-embedding-3-small", "custom-model")
        /// </summary>
        [JsonProperty(PropertyName = "embeddingModel")]
        public string? EmbeddingModel { get; set; } = null;

        /// <summary>
        /// When this embedding document was generated/updated
        /// </summary>
        [JsonProperty(PropertyName = "generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// LastModified timestamp from the source entity when this summary was generated
        /// Used to detect if entity has been updated
        /// </summary>
        [JsonProperty(PropertyName = "entityLastModified")]
        public DateTime EntityLastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Full text summary used for embedding generation
        /// Stored so we can regenerate embeddings later without re-querying entitys
        /// </summary>
        [JsonProperty(PropertyName = "entitySummary")]
        public string EntitySummary { get; set; } = "";

        /// <summary>
        /// SHA256 hash of the entity summary
        /// Used to detect if summary content changed even if entity was "modified"
        /// </summary>
        [JsonProperty(PropertyName = "summaryHash")]
        public string SummaryHash { get; set; } = "";

        /// <summary>
        /// Dimensionality of the embedding vector (e.g., 1536 for OpenAI ada-002)
        /// </summary>
        [JsonProperty(PropertyName = "dimensions")]
        public int? Dimensions { get; set; } = null;

        /// <summary>
        /// Current status of the embedding
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public EmbeddingStatus Status { get; set; } = EmbeddingStatus.Pending;

        /// <summary>
        /// Error message if Status is Failed
        /// </summary>
        [JsonProperty(PropertyName = "errorMessage")]
        public string? ErrorMessage { get; set; } = null;

        /// <summary>
        /// Number of retry attempts for failed embeddings
        /// </summary>
        [JsonProperty(PropertyName = "retryCount")]
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Metadata about what was included in the summary
        /// </summary>
        [JsonProperty(PropertyName = "summaryMetadata")]
        public SummaryMetadata SummaryMetadata { get; set; } = new SummaryMetadata();

        /// <summary>
        /// Helper method to create ID from entityId
        /// </summary>
        public static string GenerateId(string entityId) => $"embedding_{entityId}";

        /// <summary>
        /// Helper method to compute SHA256 hash of a string
        /// </summary>
        public static string ComputeHash(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Check if the entity summary needs regeneration based on last modified time
        /// </summary>
        public bool NeedsRegeneration(DateTime entityLastModified)
        {
            return entityLastModified > EntityLastModified;
        }

        /// <summary>
        /// Check if the summary content has actually changed by comparing hashes
        /// </summary>
        public bool SummaryChanged(string newSummary)
        {
            var newHash = ComputeHash(newSummary);
            return newHash != SummaryHash;
        }
    }

    /// <summary>
    /// Metadata about what was included in the entity summary
    /// </summary>
    public class SummaryMetadata
    {
        [JsonProperty(PropertyName = "hasConversationData")]
        public bool HasConversationData { get; set; } = false;

        [JsonProperty(PropertyName = "conversationChunksCount")]
        public int ConversationChunksCount { get; set; } = 0;

        [JsonProperty(PropertyName = "extractedInsightsCount")]
        public int ExtractedInsightsCount { get; set; } = 0;

        [JsonProperty(PropertyName = "preferenceCategories")]
        public List<string> PreferenceCategories { get; set; } = new();

        [JsonProperty(PropertyName = "hasPersonalityData")]
        public bool HasPersonalityData { get; set; } = false;

        [JsonProperty(PropertyName = "summaryWordCount")]
        public int SummaryWordCount { get; set; } = 0;
    }
}
