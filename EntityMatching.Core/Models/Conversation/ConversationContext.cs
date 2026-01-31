using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityMatching.Core.Models.Conversation
{
    /// <summary>
    /// Represents a chunk of conversation between user and AI
    /// </summary>
    public class ConversationChunk
    {
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; } = "";

        [JsonProperty(PropertyName = "timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "speaker")]
        public string Speaker { get; set; } = "user"; // "user" or "ai"

        [JsonProperty(PropertyName = "context")]
        public string Context { get; set; } = ""; // Optional contextual info (e.g., "discussing hobbies")
    }

    /// <summary>
    /// Represents an insight extracted from conversation
    /// </summary>
    public class ExtractedInsight
    {
        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; } = ""; // e.g., "hobby", "preference", "restriction", "personality"

        [JsonProperty(PropertyName = "insight")]
        public string Insight { get; set; } = "";

        [JsonProperty(PropertyName = "confidence")]
        public float Confidence { get; set; } = 0.0f; // 0.0 to 1.0

        [JsonProperty(PropertyName = "sourceChunk")]
        public string SourceChunk { get; set; } = ""; // Reference to original conversation text

        [JsonProperty(PropertyName = "extractedAt")]
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Stores conversation history and extracted insights for an entity
    /// Cosmos DB Container: conversations (partition key: /entityId)
    /// </summary>
    public class ConversationContext
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty(PropertyName = "entityId")]
        public string EntityId { get; set; } = "";

        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = ""; // Owner of the conversation

        [JsonProperty(PropertyName = "conversationChunks")]
        public List<ConversationChunk> ConversationChunks { get; set; } = new();

        [JsonProperty(PropertyName = "extractedInsights")]
        public List<ExtractedInsight> ExtractedInsights { get; set; } = new();

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get a summary of the conversation for AI context
        /// </summary>
        public string GetConversationSummary(int maxChunks = 10)
        {
            var recentChunks = ConversationChunks
                .OrderByDescending(c => c.Timestamp)
                .Take(maxChunks)
                .Reverse();

            return string.Join("\n", recentChunks.Select(c => $"{c.Speaker}: {c.Text}"));
        }

        /// <summary>
        /// Get insights as formatted text for AI prompts
        /// </summary>
        public string GetInsightsSummary()
        {
            if (!ExtractedInsights.Any())
                return "";

            var grouped = ExtractedInsights
                .Where(i => i.Confidence >= 0.6f) // Only include high-confidence insights
                .GroupBy(i => i.Category);

            var summary = new System.Text.StringBuilder();
            foreach (var group in grouped)
            {
                summary.AppendLine($"{group.Key}:");
                foreach (var insight in group)
                {
                    summary.AppendLine($"  - {insight.Insight}");
                }
            }

            return summary.ToString();
        }

        /// <summary>
        /// Aggregate multiple conversation documents into a single context.
        /// Used when retrieving conversation history that spans multiple documents.
        /// </summary>
        /// <param name="documents">List of conversation documents to aggregate</param>
        /// <returns>Aggregated ConversationContext, or null if no documents</returns>
        public static ConversationContext? Aggregate(List<ConversationDocument> documents)
        {
            if (documents == null || !documents.Any())
                return null;

            // Sort by sequence number to maintain chronological order
            var sorted = documents.OrderBy(d => d.SequenceNumber).ToList();
            var first = sorted.First();

            var context = new ConversationContext
            {
                Id = first.Id,
                EntityId = first.EntityId,
                UserId = first.UserId,
                CreatedAt = first.CreatedAt,
                LastUpdated = sorted.Last().LastUpdated,
                ConversationChunks = new List<ConversationChunk>(),
                ExtractedInsights = new List<ExtractedInsight>()
            };

            // Combine all chunks and insights in chronological order
            foreach (var doc in sorted)
            {
                context.ConversationChunks.AddRange(doc.ConversationChunks);
                context.ExtractedInsights.AddRange(doc.ExtractedInsights);
            }

            return context;
        }
    }
}
