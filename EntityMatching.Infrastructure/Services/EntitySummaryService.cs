using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Core.Models.Summary;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for generating comprehensive text summaries of entities
    /// Uses strategy pattern to delegate to entity-specific summary generators
    /// </summary>
    public class EntitySummaryService : IEntitySummaryService
    {
        private readonly ILogger<EntitySummaryService> _logger;
        private readonly Dictionary<EntityType, IEntitySummaryStrategy> _strategies;
        private readonly IEntitySummaryStrategy _defaultStrategy;

        public EntitySummaryService(
            ILogger<EntitySummaryService> logger,
            IEnumerable<IEntitySummaryStrategy> strategies)
        {
            _logger = logger;
            _strategies = strategies.ToDictionary(s => s.EntityType);

            // Use PersonSummaryStrategy as default fallback
            if (!_strategies.TryGetValue(EntityType.Person, out _defaultStrategy!))
            {
                _logger.LogWarning("No PersonSummaryStrategy found, summary generation may fail for unknown entity types");
            }

            _logger.LogInformation(
                "EntitySummaryService initialized with {StrategyCount} strategies: {EntityTypes}",
                _strategies.Count,
                string.Join(", ", _strategies.Keys));
        }

        public async Task<EntitySummaryResult> GenerateSummaryAsync(
            Entity entity,
            ConversationContext? conversationContext = null)
        {
            try
            {
                _logger.LogInformation(
                    "Generating summary for entity {EntityId} of type {EntityType}",
                    entity.Id,
                    entity.EntityType);

                // Select appropriate strategy based on entity type
                if (!_strategies.TryGetValue(entity.EntityType, out var strategy))
                {
                    _logger.LogWarning(
                        "No summary strategy found for entity type {EntityType}, using default strategy",
                        entity.EntityType);
                    strategy = _defaultStrategy;
                }

                if (strategy == null)
                {
                    throw new InvalidOperationException(
                        $"No summary strategy available for entity type {entity.EntityType} and no default strategy configured");
                }

                var result = await strategy.GenerateSummaryAsync(entity, conversationContext);

                _logger.LogInformation(
                    "Successfully generated summary for entity {EntityId}: {WordCount} words, {CategoryCount} categories",
                    entity.Id,
                    result.Metadata.SummaryWordCount,
                    result.Metadata.PreferenceCategories.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary for entity {EntityId} of type {EntityType}",
                    entity.Id, entity.EntityType);
                throw;
            }
        }
    }
}
