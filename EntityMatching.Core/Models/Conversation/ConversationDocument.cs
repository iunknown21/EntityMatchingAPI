using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Conversation
{
    /// <summary>
    /// Represents a single conversation document (one of potentially many for a profile).
    /// Used to split large conversations across multiple Cosmos DB documents to avoid 2MB limit.
    /// Cosmos DB Container: conversations (partition key: /profileId)
    /// </summary>
    public class ConversationDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty(PropertyName = "profileId")]
        public string ProfileId { get; set; } = "";

        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = "";

        [JsonProperty(PropertyName = "sequenceNumber")]
        public int SequenceNumber { get; set; } = 0;

        [JsonProperty(PropertyName = "isActive")]
        public bool IsActive { get; set; } = true;

        [JsonProperty(PropertyName = "conversationChunks")]
        public List<ConversationChunk> ConversationChunks { get; set; } = new();

        [JsonProperty(PropertyName = "extractedInsights")]
        public List<ExtractedInsight> ExtractedInsights { get; set; } = new();

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "estimatedSizeBytes")]
        public long EstimatedSizeBytes { get; set; } = 0;

        [JsonProperty(PropertyName = "chunkCount")]
        public int ChunkCount { get; set; } = 0;

        [JsonProperty(PropertyName = "insightCount")]
        public int InsightCount { get; set; } = 0;
    }
}
