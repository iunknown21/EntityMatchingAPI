using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Nightly scheduled function to generate and update entity summaries
    /// Runs at 2 AM UTC every night
    /// Detects entity changes via hash comparison and only regenerates when needed
    /// </summary>
    public class GenerateEntitySummariesFunction
    {
        private readonly IEntitySummaryService _summaryService;
        private readonly IEmbeddingStorageService _embeddingStorage;
        private readonly IConversationService? _conversationService;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GenerateEntitySummariesFunction> _logger;

        public GenerateEntitySummariesFunction(
            IEntitySummaryService summaryService,
            IEmbeddingStorageService embeddingStorage,
            CosmosClient cosmosClient,
            IConfiguration configuration,
            ILogger<GenerateEntitySummariesFunction> logger,
            IConversationService? conversationService = null)
        {
            _summaryService = summaryService;
            _embeddingStorage = embeddingStorage;
            _conversationService = conversationService;
            _cosmosClient = cosmosClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Timer trigger that runs nightly at 2 AM UTC
        /// Schedule: "0 0 2 * * *" (minute hour day-of-month month day-of-week)
        /// Set EMBEDDING_INFRASTRUCTURE_ENABLED=false to disable
        /// </summary>
        [Function("GenerateEntitySummaries")]
        public async Task Run(
            [TimerTrigger("0 0 2 * * *")] TimerInfo timerInfo)
        {
            var stopwatch = Stopwatch.StartNew();

            // Check if feature is enabled
            var enabled = _configuration.GetValue<bool>("EMBEDDING_INFRASTRUCTURE_ENABLED", true);
            if (!enabled)
            {
                _logger.LogInformation("Embedding infrastructure is disabled via configuration");
                return;
            }

            _logger.LogInformation("Starting nightly entity summary generation at {Time}", DateTime.UtcNow);

            try
            {
                var stats = new ProcessingStatistics();
                var batchSize = _configuration.GetValue<int>("SUMMARY_GENERATION_BATCH_SIZE", 10);

                // Step 1: Get all entities with minimal data (id and lastModified)
                var entities = await GetAllEntityMetadataAsync();
                stats.TotalEntities = entities.Count;

                _logger.LogInformation("Found {Count} total entities to process", entities.Count);

                // Step 2: Determine which entities need processing
                var entitiesToProcess = new List<EntityMetadata>();

                foreach (var entity in entities)
                {
                    try
                    {
                        // Check if embedding document exists
                        var existing = await _embeddingStorage.GetEmbeddingAsync(entity.Id);

                        if (existing == null)
                        {
                            // No embedding document - needs creation
                            entitiesToProcess.Add(entity);
                            stats.NewSummaries++;
                        }
                        else if (existing.EntitySummary == "[CLIENT_UPLOADED]")
                        {
                            // Client-uploaded embedding - never regenerate
                            _logger.LogDebug("Skipping entity {EntityId} - has client-uploaded embedding", entity.Id);
                            stats.SkippedEntities++;
                        }
                        else if (existing.NeedsRegeneration(entity.LastModified))
                        {
                            // Entity was modified after embedding was generated
                            entitiesToProcess.Add(entity);
                            stats.UpdatedSummaries++;
                        }
                        else
                        {
                            // Embedding is current
                            stats.SkippedEntities++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking embedding status for entity {EntityId}", entity.Id);
                        stats.Errors++;
                    }
                }

                _logger.LogInformation("Identified {Count} entities needing summary generation/update",
                    entitiesToProcess.Count);

                // Step 3: Process entities in batches
                for (int i = 0; i < entitiesToProcess.Count; i += batchSize)
                {
                    var batch = entitiesToProcess.Skip(i).Take(batchSize).ToList();
                    _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({Count} entities)",
                        (i / batchSize) + 1,
                        (entitiesToProcess.Count + batchSize - 1) / batchSize,
                        batch.Count);

                    foreach (var entityMeta in batch)
                    {
                        try
                        {
                            await ProcessEntityAsync(entityMeta, stats);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing entity {EntityId}", entityMeta.Id);
                            stats.Errors++;
                        }
                    }

                    // Small delay between batches to be respectful of resources
                    if (i + batchSize < entitiesToProcess.Count)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }

                stopwatch.Stop();

                // Step 4: Log final statistics
                _logger.LogInformation(
                    "Entity summary generation completed in {Duration}ms. " +
                    "Stats: Total={Total}, New={New}, Updated={Updated}, Skipped={Skipped}, Errors={Errors}",
                    stopwatch.ElapsedMilliseconds,
                    stats.TotalEntities,
                    stats.NewSummaries,
                    stats.UpdatedSummaries,
                    stats.SkippedEntities,
                    stats.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in entity summary generation");
                throw;
            }
        }

        /// <summary>
        /// Get minimal entity metadata (id and lastModified) for all entities
        /// This is an efficient query that costs very few RUs
        /// </summary>
        private async Task<List<EntityMetadata>> GetAllEntityMetadataAsync()
        {
            var databaseId = _configuration["CosmosDb:DatabaseId"];
            var entitiesContainerId = _configuration["CosmosDb:EntitiesContainerId"] ?? "entities";

            var database = _cosmosClient.GetDatabase(databaseId);
            var container = database.GetContainer(entitiesContainerId);

            // Query just id and lastModified for minimal RU cost
            // IMPORTANT: Use PascalCase to match Cosmos DB storage
            var query = "SELECT c.id, c.lastModified FROM c";
            var queryDefinition = new QueryDefinition(query);

            var iterator = container.GetItemQueryIterator<EntityMetadata>(queryDefinition);
            var results = new List<EntityMetadata>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        /// <summary>
        /// Process a single entity: load full entity, generate summary, create/update embedding document
        /// </summary>
        private async Task ProcessEntityAsync(EntityMetadata entityMeta, ProcessingStatistics stats)
        {
            // Load full entity
            var entity = await GetFullEntityAsync(entityMeta.Id);
            if (entity == null)
            {
                _logger.LogWarning("Entity {EntityId} not found when loading full entity", entityMeta.Id);
                stats.Errors++;
                return;
            }

            // Load conversation context if it exists
            ConversationContext? conversation = null;
            if (_conversationService != null)
            {
                try
                {
                    conversation = await _conversationService.GetConversationHistoryAsync(entityMeta.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load conversation for entity {EntityId}, continuing without it", entityMeta.Id);
                    // Continue without conversation - not fatal
                }
            }
            else
            {
                _logger.LogDebug("ConversationService not available, skipping conversation context for entity {EntityId}", entityMeta.Id);
            }

            // Generate summary
            var summaryResult = await _summaryService.GenerateSummaryAsync(entity, conversation);
            var summaryHash = EntityEmbedding.ComputeHash(summaryResult.Summary);

            // Check if summary actually changed (by comparing hash)
            var existingEmbedding = await _embeddingStorage.GetEmbeddingAsync(entityMeta.Id);
            if (existingEmbedding != null && existingEmbedding.SummaryHash == summaryHash)
            {
                _logger.LogInformation("Entity {EntityId} was modified but summary unchanged, skipping update", entityMeta.Id);
                stats.SkippedEntities++;
                return;
            }

            // Create or update embedding document
            var embedding = existingEmbedding ?? new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(entityMeta.Id),
                EntityId = entityMeta.Id
            };

            embedding.EntitySummary = summaryResult.Summary;
            embedding.SummaryHash = summaryHash;
            embedding.EntityLastModified = entity.LastModified;
            embedding.GeneratedAt = DateTime.UtcNow;
            embedding.Status = EmbeddingStatus.Pending; // Ready for embedding generation when provider is chosen
            embedding.SummaryMetadata = summaryResult.Metadata;
            embedding.ErrorMessage = null; // Clear any previous errors

            await _embeddingStorage.UpsertEmbeddingAsync(embedding);

            _logger.LogInformation("Generated summary for entity {EntityId} ({WordCount} words)",
                entityMeta.Id, summaryResult.Metadata.SummaryWordCount);
        }

        /// <summary>
        /// Load a full entity by ID
        /// </summary>
        private async Task<Entity?> GetFullEntityAsync(string id)
        {
            try
            {
                var databaseId = _configuration["CosmosDb:DatabaseId"];
                var entitiesContainerId = _configuration["CosmosDb:EntitiesContainerId"] ?? "entities";

                var database = _cosmosClient.GetDatabase(databaseId);
                var container = database.GetContainer(entitiesContainerId);

                var response = await container.ReadItemAsync<Entity>(
                    id,
                    new PartitionKey(id)
                );

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Minimal entity metadata for efficient querying
        /// </summary>
        private class EntityMetadata
        {
            public string Id { get; set; } = "";
            public DateTime LastModified { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Statistics for the processing run
        /// </summary>
        private class ProcessingStatistics
        {
            public int TotalEntities { get; set; }
            public int NewSummaries { get; set; }
            public int UpdatedSummaries { get; set; }
            public int SkippedEntities { get; set; }
            public int Errors { get; set; }
        }
    }
}
