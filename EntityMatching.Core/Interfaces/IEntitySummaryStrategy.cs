using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Core.Models.Summary;
using EntityMatching.Shared.Models;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Strategy interface for generating entity-specific summaries
    /// Enables different summary generation logic for different entity types
    /// </summary>
    public interface IEntitySummaryStrategy
    {
        /// <summary>
        /// The entity type this strategy handles
        /// </summary>
        EntityType EntityType { get; }

        /// <summary>
        /// Generate a comprehensive text summary for the entity
        /// </summary>
        /// <param name="entity">The entity to summarize</param>
        /// <param name="conversation">Optional conversation context</param>
        /// <returns>Summary result with text and metadata</returns>
        Task<EntitySummaryResult> GenerateSummaryAsync(Entity entity, ConversationContext? conversation = null);
    }
}
