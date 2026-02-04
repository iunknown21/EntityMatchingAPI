using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Conversation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for managing conversational context and insight extraction
    /// </summary>
    public interface IConversationService
    {
        /// <summary>
        /// Process a user message, generate AI response, and extract insights
        /// </summary>
        /// <param name="systemPrompt">Required for new conversations. Stored in metadata and reused for subsequent messages.</param>
        Task<ConversationResponse> ProcessUserMessageAsync(string entityId, string userId, string message, string? systemPrompt = null);

        /// <summary>
        /// Get conversation history for an entity (aggregates all documents)
        /// </summary>
        Task<ConversationContext?> GetConversationHistoryAsync(string entityId);

        /// <summary>
        /// Clear conversation history for an entity (deletes all documents and metadata)
        /// </summary>
        Task ClearConversationHistoryAsync(string entityId);

        /// <summary>
        /// Get summarized insights for use in prompts
        /// </summary>
        Task<string> GetInsightsSummaryAsync(string entityId);

        /// <summary>
        /// Get conversation documents for an entity with optional pagination
        /// </summary>
        Task<List<ConversationDocument>> GetConversationDocumentsAsync(
            string entityId,
            int? startSequence = null,
            int? limit = null);

        /// <summary>
        /// Get conversation metadata for an entity
        /// </summary>
        Task<ConversationMetadata?> GetConversationMetadataAsync(string entityId);
    }

    /// <summary>
    /// Response from processing a conversation message
    /// </summary>
    public class ConversationResponse
    {
        public string AiResponse { get; set; } = "";
        public List<ExtractedInsight> NewInsights { get; set; } = new();
        public string ConversationId { get; set; } = "";
    }
}
