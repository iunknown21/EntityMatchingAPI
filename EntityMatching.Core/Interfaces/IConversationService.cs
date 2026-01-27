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
        Task<ConversationResponse> ProcessUserMessageAsync(string profileId, string userId, string message);

        /// <summary>
        /// Get conversation history for a profile (aggregates all documents)
        /// </summary>
        Task<ConversationContext?> GetConversationHistoryAsync(string profileId);

        /// <summary>
        /// Clear conversation history for a profile (deletes all documents and metadata)
        /// </summary>
        Task ClearConversationHistoryAsync(string profileId);

        /// <summary>
        /// Get summarized insights for use in prompts
        /// </summary>
        Task<string> GetInsightsSummaryAsync(string profileId);

        /// <summary>
        /// Get conversation documents for a profile with optional pagination
        /// </summary>
        Task<List<ConversationDocument>> GetConversationDocumentsAsync(
            string profileId,
            int? startSequence = null,
            int? limit = null);

        /// <summary>
        /// Get conversation metadata for a profile
        /// </summary>
        Task<ConversationMetadata?> GetConversationMetadataAsync(string profileId);
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
