using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Search;
using EntityMatching.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for finding similar entities using vector similarity search
    /// Supports hybrid search: semantic similarity + structured attribute filtering
    /// Uses cosine similarity with in-memory comparison (suitable for < 10k entities)
    /// </summary>
    public class SimilaritySearchService : ISimilaritySearchService
    {
        private readonly IEmbeddingStorageService _embeddingStorage;
        private readonly IEmbeddingService _embeddingService;
        private readonly IEntityService _entityService;
        private readonly IAttributeFilterService _attributeFilterService;
        private readonly ILogger<SimilaritySearchService> _logger;

        public SimilaritySearchService(
            IEmbeddingStorageService embeddingStorage,
            IEmbeddingService embeddingService,
            IEntityService entityService,
            IAttributeFilterService attributeFilterService,
            ILogger<SimilaritySearchService> logger)
        {
            _embeddingStorage = embeddingStorage;
            _embeddingService = embeddingService;
            _entityService = entityService;
            _attributeFilterService = attributeFilterService;
            _logger = logger;
        }

        /// <summary>
        /// Find entities similar to a given entity
        /// Supports attribute and metadata filtering with privacy enforcement
        /// </summary>
        public async Task<SearchResult> FindSimilarEntitiesAsync(
            string entityId,
            int limit = 10,
            float minSimilarity = 0.5f,
            bool includeEntities = false,
            FilterGroup? attributeFilters = null,
            Dictionary<string, object>? metadataFilters = null,
            string? requestingUserId = null,
            bool enforcePrivacy = true)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Finding similar entities for {EntityId} (limit={Limit}, minSim={MinSimilarity}, hasAttrFilters={HasAttrFilters}, hasMetadataFilters={HasMetadataFilters})",
                entityId, limit, minSimilarity, attributeFilters?.HasFilters ?? false, (metadataFilters?.Count ?? 0) > 0);

            // 1. Get reference embedding
            var referenceEmbedding = await _embeddingStorage.GetEmbeddingAsync(entityId);
            if (referenceEmbedding == null)
            {
                throw new InvalidOperationException($"No embedding found for entity {entityId}");
            }

            if (referenceEmbedding.Status != EmbeddingStatus.Generated)
            {
                throw new InvalidOperationException($"Embedding for entity {entityId} is not generated (status: {referenceEmbedding.Status})");
            }

            if (referenceEmbedding.Embedding == null || referenceEmbedding.Embedding.Length == 0)
            {
                throw new InvalidOperationException($"Embedding vector for entity {entityId} is null or empty");
            }

            // 2. Get all other generated embeddings (excluding reference)
            var allEmbeddings = await GetAllGeneratedEmbeddingsExcluding(entityId);

            _logger.LogDebug("Comparing against {Count} other embeddings", allEmbeddings.Count);

            // 3. Calculate similarities in parallel (over-fetch if using filters)
            var hasFilters = (attributeFilters?.HasFilters ?? false) || ((metadataFilters?.Count ?? 0) > 0);
            var candidateLimit = hasFilters ? limit * 2 : limit;

            var candidates = allEmbeddings
                .AsParallel()
                .Select(embedding => new EntityMatch
                {
                    EntityId = embedding.EntityId,
                    SimilarityScore = VectorMath.CosineSimilarity(
                        referenceEmbedding.Embedding!,
                        embedding.Embedding!),
                    EmbeddingDimensions = embedding.Dimensions
                })
                .Where(match => match.SimilarityScore >= minSimilarity)
                .OrderByDescending(match => match.SimilarityScore)
                .Take(candidateLimit)
                .ToList();

            // 4. Apply attribute/metadata filters if provided
            List<EntityMatch> matches;
            if (hasFilters)
            {
                matches = await ApplyFilters(
                    candidates,
                    attributeFilters,
                    metadataFilters,
                    requestingUserId,
                    enforcePrivacy,
                    limit);
            }
            else
            {
                matches = candidates.Take(limit).ToList();
            }

            // 5. Optionally populate full entities
            if (includeEntities)
            {
                await PopulateEntitiesAsync(matches);
            }

            stopwatch.Stop();

            _logger.LogInformation("Found {MatchCount} similar entities in {DurationMs}ms",
                matches.Count, stopwatch.ElapsedMilliseconds);

            return new SearchResult
            {
                Matches = matches,
                TotalMatches = matches.Count,
                Metadata = new SearchMetadata
                {
                    SearchedAt = DateTime.UtcNow,
                    TotalEmbeddingsSearched = allEmbeddings.Count,
                    MinSimilarity = minSimilarity,
                    RequestedLimit = limit,
                    SearchDurationMs = stopwatch.ElapsedMilliseconds
                }
            };
        }

        /// <summary>
        /// Search for entities similar to a text query
        /// Supports hybrid search: semantic similarity + structured attribute filtering + metadata filtering
        /// </summary>
        public async Task<SearchResult> SearchByQueryAsync(
            string query,
            int limit = 10,
            float minSimilarity = 0.5f,
            bool includeEntities = false,
            FilterGroup? attributeFilters = null,
            Dictionary<string, object>? metadataFilters = null,
            string? requestingUserId = null,
            bool enforcePrivacy = true)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Searching entities with query: '{Query}' (limit={Limit}, minSim={MinSimilarity}, hasAttrFilters={HasAttrFilters}, hasMetadataFilters={HasMetadataFilters})",
                query, limit, minSimilarity, attributeFilters?.HasFilters ?? false, (metadataFilters?.Count ?? 0) > 0);

            // 1. Generate embedding for query
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query);
            if (queryVector == null || queryVector.Length == 0)
            {
                throw new InvalidOperationException("Failed to generate embedding for query");
            }

            _logger.LogDebug("Generated {Dimensions}-dimensional query embedding", queryVector.Length);

            // 2. Get all generated embeddings
            var allEmbeddings = await GetAllGeneratedEmbeddings();

            _logger.LogDebug("Comparing against {Count} entity embeddings", allEmbeddings.Count);

            // 3. Calculate similarities in parallel (over-fetch if using filters)
            var hasFilters = (attributeFilters?.HasFilters ?? false) || ((metadataFilters?.Count ?? 0) > 0);
            var candidateLimit = hasFilters ? limit * 2 : limit;

            var candidates = allEmbeddings
                .AsParallel()
                .Select(embedding => new EntityMatch
                {
                    EntityId = embedding.EntityId,
                    SimilarityScore = VectorMath.CosineSimilarity(queryVector, embedding.Embedding!),
                    EmbeddingDimensions = embedding.Dimensions
                })
                .Where(match => match.SimilarityScore >= minSimilarity)
                .OrderByDescending(match => match.SimilarityScore)
                .Take(candidateLimit)
                .ToList();

            // 4. Apply attribute/metadata filters if provided
            List<EntityMatch> matches;
            if (hasFilters)
            {
                matches = await ApplyFilters(
                    candidates,
                    attributeFilters,
                    metadataFilters,
                    requestingUserId,
                    enforcePrivacy,
                    limit);
            }
            else
            {
                matches = candidates.Take(limit).ToList();
            }

            // 5. Optionally populate full entities
            if (includeEntities)
            {
                await PopulateEntitiesAsync(matches);
            }

            stopwatch.Stop();

            _logger.LogInformation("Found {MatchCount} matching entities for query in {DurationMs}ms",
                matches.Count, stopwatch.ElapsedMilliseconds);

            return new SearchResult
            {
                Matches = matches,
                TotalMatches = matches.Count,
                Metadata = new SearchMetadata
                {
                    SearchedAt = DateTime.UtcNow,
                    TotalEmbeddingsSearched = allEmbeddings.Count,
                    MinSimilarity = minSimilarity,
                    RequestedLimit = limit,
                    SearchDurationMs = stopwatch.ElapsedMilliseconds
                }
            };
        }

        // ============= PRIVATE HELPER METHODS =============

        /// <summary>
        /// Apply attribute and metadata filters to candidate matches with privacy enforcement
        /// Two-phase filtering: semantic similarity → attribute/metadata filtering
        /// </summary>
        private async Task<List<EntityMatch>> ApplyFilters(
            List<EntityMatch> candidates,
            FilterGroup? attributeFilters,
            Dictionary<string, object>? metadataFilters,
            string? requestingUserId,
            bool enforcePrivacy,
            int limit)
        {
            var filtered = new List<EntityMatch>();
            var candidatesEvaluated = 0;

            _logger.LogDebug(
                "Applying filters to {CandidateCount} candidates (limit={Limit}, hasAttrFilters={HasAttrFilters}, hasMetadataFilters={HasMetadataFilters}, enforcePrivacy={EnforcePrivacy})",
                candidates.Count, limit, attributeFilters?.HasFilters ?? false, metadataFilters?.Count > 0, enforcePrivacy);

            foreach (var candidate in candidates)
            {
                candidatesEvaluated++;

                try
                {
                    // Fetch full entity
                    var entity = await _entityService.GetEntityAsync(candidate.EntityId);
                    if (entity == null)
                    {
                        _logger.LogWarning("Entity {EntityId} not found during filtering", candidate.EntityId);
                        continue;
                    }

                    // Check if entity is searchable
                    if (!entity.IsSearchable)
                    {
                        _logger.LogDebug("Entity {EntityId} is not searchable (IsSearchable=false)", candidate.EntityId);
                        continue;
                    }

                    var passesFilters = true;

                    // Evaluate attribute filters if provided
                    if (attributeFilters?.HasFilters ?? false)
                    {
                        passesFilters = _attributeFilterService.EvaluateFilters(
                            entity,
                            attributeFilters,
                            requestingUserId,
                            enforcePrivacy);

                        if (!passesFilters)
                        {
                            _logger.LogDebug("Entity {EntityId} did not match attribute filters", candidate.EntityId);
                            continue;
                        }
                    }

                    // Evaluate metadata filters if provided
                    if (metadataFilters != null && metadataFilters.Count > 0)
                    {
                        passesFilters = MatchesMetadataFilters(entity.Metadata, metadataFilters);

                        if (!passesFilters)
                        {
                            _logger.LogDebug("Entity {EntityId} did not match metadata filters", candidate.EntityId);
                            continue;
                        }
                    }

                    // Entity passed all filters
                    if (passesFilters)
                    {
                        // Populate matched attributes for transparency
                        if (attributeFilters?.HasFilters ?? false)
                        {
                            candidate.MatchedAttributes = _attributeFilterService.GetMatchedAttributes(
                                entity,
                                attributeFilters,
                                requestingUserId,
                                enforcePrivacy);
                        }

                        // Populate entity name/metadata for display
                        candidate.EntityName = entity.Name ?? "";
                        candidate.EntityLastModified = entity.LastModified;

                        filtered.Add(candidate);

                        _logger.LogDebug(
                            "Entity {EntityId} matched all filters (similarity={Score:F4}, matchedAttrs={AttrCount})",
                            candidate.EntityId, candidate.SimilarityScore, candidate.MatchedAttributes?.Count ?? 0);

                        // Stop once we reach the limit
                        if (filtered.Count >= limit)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating filters for entity {EntityId}", candidate.EntityId);
                    // Continue - don't fail entire search if one entity evaluation fails
                }
            }

            _logger.LogInformation(
                "Filtering complete: {FilteredCount} matches from {EvaluatedCount} candidates (out of {TotalCandidates} total)",
                filtered.Count, candidatesEvaluated, candidates.Count);

            return filtered;
        }

        /// <summary>
        /// Check if profile metadata matches the provided filters
        /// Supports nested key matching (e.g., verification.email_verified)
        /// </summary>
        private bool MatchesMetadataFilters(Dictionary<string, object>? profileMetadata, Dictionary<string, object> filters)
        {
            if (profileMetadata == null || profileMetadata.Count == 0)
            {
                return false; // Entity has no metadata, cannot match filters
            }

            foreach (var filter in filters)
            {
                if (!profileMetadata.ContainsKey(filter.Key))
                {
                    return false; // Required key missing
                }

                var profileValue = profileMetadata[filter.Key];
                var filterValue = filter.Value;

                // Handle nested dictionary matching
                if (profileValue is Dictionary<string, object> profileDict &&
                    filterValue is Dictionary<string, object> filterDict)
                {
                    if (!MatchesMetadataFilters(profileDict, filterDict))
                    {
                        return false;
                    }
                }
                // Handle direct value comparison
                else if (!ValuesEqual(profileValue, filterValue))
                {
                    return false;
                }
            }

            return true; // All filters matched
        }

        /// <summary>
        /// Compare two values for equality, handling type conversions
        /// </summary>
        private bool ValuesEqual(object? profileValue, object? filterValue)
        {
            if (profileValue == null && filterValue == null) return true;
            if (profileValue == null || filterValue == null) return false;

            // Handle numeric comparisons (int64 vs int32, double vs decimal, etc.)
            if (IsNumeric(profileValue) && IsNumeric(filterValue))
            {
                var profileNumber = Convert.ToDouble(profileValue);
                var filterNumber = Convert.ToDouble(filterValue);
                return Math.Abs(profileNumber - filterNumber) < 0.0001;
            }

            // Handle boolean
            if (profileValue is bool && filterValue is bool)
            {
                return (bool)profileValue == (bool)filterValue;
            }

            // Handle string comparison (case-insensitive)
            if (profileValue is string profileStr && filterValue is string filterStr)
            {
                return string.Equals(profileStr, filterStr, StringComparison.OrdinalIgnoreCase);
            }

            // Fallback to object.Equals
            return profileValue.Equals(filterValue);
        }

        private bool IsNumeric(object value)
        {
            return value is int || value is long || value is float || value is double || value is decimal;
        }

        /// <summary>
        /// Get all embeddings with Status=Generated
        /// </summary>
        private async Task<List<EntityEmbedding>> GetAllGeneratedEmbeddings()
        {
            var embeddings = await _embeddingStorage.GetEmbeddingsByStatusAsync(
                EmbeddingStatus.Generated,
                limit: null); // No limit - get all

            return embeddings.Where(e => e.Embedding != null && e.Embedding.Length > 0).ToList();
        }

        /// <summary>
        /// Get all generated embeddings excluding a specific profile
        /// </summary>
        private async Task<List<EntityEmbedding>> GetAllGeneratedEmbeddingsExcluding(string excludeProfileId)
        {
            var all = await GetAllGeneratedEmbeddings();
            return all.Where(e => e.EntityId != excludeProfileId).ToList();
        }

        /// <summary>
        /// Populate full Entity objects for each match
        /// </summary>
        private async Task PopulateEntitiesAsync(List<EntityMatch> matches)
        {
            foreach (var match in matches)
            {
                try
                {
                    var entity = await _entityService.GetEntityAsync(match.EntityId);
                    if (entity != null)
                    {
                        match.Entity = entity;
                        match.EntityName = entity.Name ?? "";
                        match.EntityLastModified = entity.LastModified;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load entity {EntityId} for match", match.EntityId);
                    // Continue - don't fail entire search if one entity load fails
                }
            }
        }
    }
}
