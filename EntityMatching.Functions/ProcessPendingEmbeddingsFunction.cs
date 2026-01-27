using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Timer-triggered function that processes pending embeddings
    /// Runs every 30 minutes to generate vector embeddings from entity summaries
    /// </summary>
    public class ProcessPendingEmbeddingsFunction
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IEmbeddingStorageService _embeddingStorage;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProcessPendingEmbeddingsFunction> _logger;

        public ProcessPendingEmbeddingsFunction(
            IEmbeddingService embeddingService,
            IEmbeddingStorageService embeddingStorage,
            IConfiguration configuration,
            ILogger<ProcessPendingEmbeddingsFunction> logger)
        {
            _embeddingService = embeddingService;
            _embeddingStorage = embeddingStorage;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Process pending embeddings every 30 minutes
        /// Timer trigger: "0 */30 * * * *" = minute 0 and 30 of every hour
        /// </summary>
        [Function("ProcessPendingEmbeddings")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("ProcessPendingEmbeddings triggered at {Time}", DateTime.UtcNow);

            // Check if feature is enabled
            var enabled = _configuration.GetValue<bool>("EMBEDDING_INFRASTRUCTURE_ENABLED", true);
            if (!enabled)
            {
                _logger.LogInformation("Embedding infrastructure is disabled. Skipping processing.");
                return;
            }

            // Get batch size from configuration
            var batchSize = _configuration.GetValue<int>("EMBEDDING_PROCESSING_BATCH_SIZE", 50);
            var maxRetries = _configuration.GetValue<int>("EMBEDDING_MAX_RETRIES", 3);

            _logger.LogInformation("Processing embeddings with batch size {BatchSize}, max retries {MaxRetries}",
                batchSize, maxRetries);

            // Statistics tracking
            int totalProcessed = 0;
            int successCount = 0;
            int failedCount = 0;
            int skippedCount = 0;

            try
            {
                // 1. Get pending embeddings (limit to batch size)
                var pendingEmbeddings = await _embeddingStorage.GetEmbeddingsByStatusAsync(
                    EmbeddingStatus.Pending,
                    limit: batchSize);

                _logger.LogInformation("Found {Count} pending embeddings", pendingEmbeddings.Count);

                // 2. Get failed embeddings that haven't exceeded max retries
                var failedEmbeddings = await _embeddingStorage.GetEmbeddingsByStatusAsync(
                    EmbeddingStatus.Failed,
                    limit: batchSize - pendingEmbeddings.Count);

                var retryableEmbeddings = failedEmbeddings
                    .Where(e => e.RetryCount < maxRetries)
                    .ToList();

                _logger.LogInformation("Found {Count} retryable failed embeddings (out of {Total} failed)",
                    retryableEmbeddings.Count, failedEmbeddings.Count);

                // 3. Combine both lists
                var toProcess = pendingEmbeddings.Concat(retryableEmbeddings).ToList();

                if (toProcess.Count == 0)
                {
                    _logger.LogInformation("No embeddings to process");
                    return;
                }

                _logger.LogInformation("Processing {Total} embeddings total", toProcess.Count);

                // 4. Process each embedding
                foreach (var embedding in toProcess)
                {
                    totalProcessed++;

                    try
                    {
                        // Skip if summary is empty
                        if (string.IsNullOrWhiteSpace(embedding.EntitySummary))
                        {
                            _logger.LogWarning("Skipping embedding {EntityId} - empty summary", embedding.EntityId);
                            skippedCount++;
                            continue;
                        }

                        _logger.LogDebug("Processing embedding for entity {EntityId} (attempt {RetryCount})",
                            embedding.EntityId, embedding.RetryCount + 1);

                        // Generate vector from summary
                        var vector = await _embeddingService.GenerateEmbeddingAsync(embedding.EntitySummary);

                        if (vector != null && vector.Length > 0)
                        {
                            // Success - update embedding
                            embedding.Embedding = vector;
                            embedding.Dimensions = vector.Length;
                            embedding.EmbeddingModel = _embeddingService.ModelName;
                            embedding.Status = EmbeddingStatus.Generated;
                            embedding.ErrorMessage = null;
                            embedding.RetryCount = 0; // Reset retry count on success

                            _logger.LogInformation("Successfully generated {Dimensions}-dimensional embedding for entity {EntityId}",
                                vector.Length, embedding.EntityId);

                            successCount++;
                        }
                        else
                        {
                            // Null response - mark as failed
                            embedding.Status = EmbeddingStatus.Failed;
                            embedding.ErrorMessage = "Embedding service returned null or empty vector";
                            embedding.RetryCount++;

                            _logger.LogWarning("Failed to generate embedding for entity {EntityId}: null response (retry {RetryCount}/{MaxRetries})",
                                embedding.EntityId, embedding.RetryCount, maxRetries);

                            failedCount++;
                        }

                        // Upsert to storage
                        await _embeddingStorage.UpsertEmbeddingAsync(embedding);
                    }
                    catch (Exception ex)
                    {
                        // Error - update retry count and status
                        embedding.Status = EmbeddingStatus.Failed;
                        embedding.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
                        embedding.RetryCount++;

                        _logger.LogError(ex, "Error generating embedding for entity {EntityId} (retry {RetryCount}/{MaxRetries})",
                            embedding.EntityId, embedding.RetryCount, maxRetries);

                        failedCount++;

                        // Still save the failed state
                        try
                        {
                            await _embeddingStorage.UpsertEmbeddingAsync(embedding);
                        }
                        catch (Exception saveEx)
                        {
                            _logger.LogError(saveEx, "Failed to save error state for entity {EntityId}", embedding.EntityId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in ProcessPendingEmbeddings");
                throw;
            }

            stopwatch.Stop();

            // 5. Log final statistics
            _logger.LogInformation(
                "Embedding processing completed in {Duration}ms. Stats: Total={Total}, Success={Success}, Failed={Failed}, Skipped={Skipped}",
                stopwatch.ElapsedMilliseconds,
                totalProcessed,
                successCount,
                failedCount,
                skippedCount);
        }
    }
}
