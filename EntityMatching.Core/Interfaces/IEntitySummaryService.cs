using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Core.Models.Summary;
using EntityMatching.Shared.Models;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for generating comprehensive text summaries of entities
    /// Uses strategy pattern to provide entity-specific summary logic
    /// </summary>
    public interface IEntitySummaryService
    {
        /// <summary>
        /// Generate a comprehensive text summary for an entity
        /// Automatically selects the appropriate strategy based on entity type
        /// </summary>
        /// <param name="entity">The entity to summarize</param>
        /// <param name="conversationContext">Optional conversation history</param>
        /// <returns>Summary result with text and metadata</returns>
        Task<EntitySummaryResult> GenerateSummaryAsync(Entity entity, ConversationContext? conversationContext = null);
    }
}
