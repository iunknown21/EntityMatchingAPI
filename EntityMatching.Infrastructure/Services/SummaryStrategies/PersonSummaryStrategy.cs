using EntityMatching.Core.Models.Summary;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services.SummaryStrategies
{
    /// <summary>
    /// Summary strategy for Person entities
    /// Generates comprehensive summaries including personality, preferences, and conversation insights
    /// </summary>
    public class PersonSummaryStrategy : IEntitySummaryStrategy
    {
        private readonly ILogger<PersonSummaryStrategy> _logger;

        public EntityType EntityType => EntityType.Person;

        public PersonSummaryStrategy(ILogger<PersonSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public async Task<EntitySummaryResult> GenerateSummaryAsync(Entity entity, ConversationContext? conversation = null)
        {
            var summary = new StringBuilder();
            var metadata = new SummaryMetadata();

            // Basic Information
            summary.AppendLine($"Person: {entity.Name}");

            if (!string.IsNullOrEmpty(entity.Description))
            {
                summary.AppendLine($"Bio: {entity.Description}");
            }

            // Extract person-specific data from attributes dictionary
            AppendFromAttributes(summary, entity, metadata);

            // Conversation Insights (universal for all entities)
            if (conversation?.ExtractedInsights?.Any() == true)
            {
                metadata.HasConversationData = true;
                metadata.ConversationChunksCount = conversation.ConversationChunks?.Count ?? 0;
                metadata.ExtractedInsightsCount = conversation.ExtractedInsights.Count;

                summary.AppendLine();
                summary.AppendLine("=== Additional Insights from Conversations ===");
                var insightsSummary = conversation.GetInsightsSummary();
                if (!string.IsNullOrEmpty(insightsSummary))
                {
                    summary.AppendLine(insightsSummary);
                }
            }

            var summaryText = summary.ToString();
            metadata.SummaryWordCount = summaryText.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            return await Task.FromResult(new EntitySummaryResult
            {
                Summary = summaryText,
                Metadata = metadata
            });
        }

        private void AppendFromAttributes(StringBuilder summary, Entity entity, SummaryMetadata metadata)
        {
            // Extract person data from attributes dictionary if available
            if (entity.Attributes.TryGetValue("age", out var age))
            {
                summary.AppendLine($"Age: {age}");
            }

            if (entity.Attributes.TryGetValue("location", out var location))
            {
                summary.AppendLine($"Location: {location}");
            }

            if (entity.Attributes.TryGetValue("skills", out var skills))
            {
                summary.AppendLine($"Skills: {string.Join(", ", (string[])skills)}");
            }

            if (entity.Attributes.TryGetValue("interests", out var interests))
            {
                summary.AppendLine($"Interests: {string.Join(", ", (string[])interests)}");
            }
        }
    }
}
