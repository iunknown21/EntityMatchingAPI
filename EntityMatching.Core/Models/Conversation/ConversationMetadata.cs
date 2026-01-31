using Newtonsoft.Json;
using System;

namespace EntityMatching.Core.Models.Conversation
{
    /// <summary>
    /// Metadata document tracking conversation state for an entity.
    /// Provides fast access to active document and aggregate statistics without querying all documents.
    /// Cosmos DB Container: conversations (partition key: /entityId)
    /// </summary>
    public class ConversationMetadata
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = ""; // Format: "convmeta_{entityId}"

        [JsonProperty(PropertyName = "entityId")]
        public string EntityId { get; set; } = "";

        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = "";

        [JsonProperty(PropertyName = "activeDocumentId")]
        public string ActiveDocumentId { get; set; } = "";

        [JsonProperty(PropertyName = "activeSequenceNumber")]
        public int ActiveSequenceNumber { get; set; } = 0;

        [JsonProperty(PropertyName = "totalDocuments")]
        public int TotalDocuments { get; set; } = 0;

        [JsonProperty(PropertyName = "totalChunks")]
        public int TotalChunks { get; set; } = 0;

        [JsonProperty(PropertyName = "totalInsights")]
        public int TotalInsights { get; set; } = 0;

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Generate metadata document ID for an entity
        /// </summary>
        public static string GenerateId(string entityId) => $"convmeta_{entityId}";
    }
}
