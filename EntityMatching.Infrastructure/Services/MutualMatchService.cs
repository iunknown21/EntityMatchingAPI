using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Search;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for finding mutual matches between entities
    /// Implements bidirectional matching where both entities must match each other
    /// </summary>
    public class MutualMatchService : IMutualMatchService
    {
        private readonly ISimilaritySearchService _searchService;
        private readonly ILogger<MutualMatchService> _logger;

        public MutualMatchService(
            ISimilaritySearchService searchService,
            ILogger<MutualMatchService> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Find mutual matches where both entities match each other above threshold
        /// Process:
        /// 1. Find all entities that source entity matches (forward search)
        /// 2. For each candidate, check if candidate also matches source (reverse search)
        /// 3. Return only bidirectional matches
        /// </summary>
        public async Task<MutualMatchResult> FindMutualMatchesAsync(
            string entityId,
            float minSimilarity = 0.8f,
            EntityType? targetEntityType = null,
            int limit = 50)
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Finding mutual matches for entity {EntityId} (minSim={MinSimilarity}, targetType={TargetType}, limit={Limit})",
                entityId, minSimilarity, targetEntityType, limit);

            try
            {
                // Step 1: Build attribute filter for target entity type if specified
                FilterGroup? attributeFilters = null;
                if (targetEntityType.HasValue)
                {
                    attributeFilters = new FilterGroup
                    {
                        LogicalOperator = LogicalOperator.And,
                        Filters = new List<AttributeFilter>
                        {
                            new AttributeFilter
                            {
                                FieldPath = "entityType",
                                Operator = FilterOperator.Equals,
                                Value = (int)targetEntityType.Value
                            }
                        }
                    };
                }

                // Step 2: Find all entities that source entity matches (forward search)
                // Over-fetch since many won't be mutual matches
                var candidateLimit = limit * 3;
                var forwardMatches = await _searchService.FindSimilarEntitiesAsync(
                    entityId,
                    limit: candidateLimit,
                    minSimilarity: minSimilarity,
                    includeEntities: false,
                    attributeFilters: attributeFilters);

                _logger.LogDebug(
                    "Forward search found {CandidateCount} candidates for entity {EntityId}",
                    forwardMatches.Matches.Count, entityId);

                if (!forwardMatches.Matches.Any())
                {
                    _logger.LogInformation("No forward matches found, returning empty result");
                    return new MutualMatchResult
                    {
                        Matches = new List<MutualMatch>(),
                        TotalMutualMatches = 0,
                        Metadata = new MutualMatchMetadata
                        {
                            SearchedAt = DateTime.UtcNow,
                            TargetEntityType = targetEntityType,
                            CandidatesEvaluated = 0,
                            ReverseLookups = 0,
                            SearchDurationMs = stopwatch.ElapsedMilliseconds,
                            MinSimilarity = minSimilarity
                        }
                    };
                }

                // Step 3: For each candidate, check if they also match the source (reverse search)
                var mutualMatches = new List<MutualMatch>();
                var reverseLookupCount = 0;

                // Process candidates in parallel for performance
                var tasks = forwardMatches.Matches.Select(async candidate =>
                {
                    try
                    {
                        reverseLookupCount++;

                        // Reverse search: does candidate match source?
                        var reverseMatches = await _searchService.FindSimilarEntitiesAsync(
                            candidate.EntityId,
                            limit: 100, // Look through enough results to find source
                            minSimilarity: minSimilarity,
                            includeEntities: false);

                        // Check if source entity is in candidate's matches
                        var reverseMatch = reverseMatches.Matches.FirstOrDefault(m => m.EntityId == entityId);

                        if (reverseMatch != null)
                        {
                            // Mutual match found!
                            _logger.LogDebug(
                                "Mutual match detected: {EntityA} <-> {EntityB} (scores: {AtoB:F3} / {BtoA:F3})",
                                entityId, candidate.EntityId,
                                candidate.SimilarityScore, reverseMatch.SimilarityScore);

                            return new MutualMatch
                            {
                                EntityAId = entityId,
                                EntityBId = candidate.EntityId,
                                EntityAName = "", // Will be populated if needed
                                EntityBName = candidate.EntityName,
                                AToB_Score = candidate.SimilarityScore,
                                BToA_Score = reverseMatch.SimilarityScore,
                                MutualScore = (candidate.SimilarityScore + reverseMatch.SimilarityScore) / 2f,
                                MatchType = "Mutual",
                                DetectedAt = DateTime.UtcNow,
                                MatchedAttributes = candidate.MatchedAttributes
                            };
                        }

                        return null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Error checking reverse match for candidate {CandidateId}",
                            candidate.EntityId);
                        return null;
                    }
                });

                var results = await Task.WhenAll(tasks);
                mutualMatches = results.Where(m => m != null).Cast<MutualMatch>().ToList();

                // Step 4: Sort by mutual score and limit results
                mutualMatches = mutualMatches
                    .OrderByDescending(m => m.MutualScore)
                    .Take(limit)
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "Found {MutualCount} mutual matches out of {CandidateCount} candidates in {DurationMs}ms " +
                    "(average mutual score: {AvgScore:F3}, reverse lookups: {ReverseLookups})",
                    mutualMatches.Count,
                    forwardMatches.Matches.Count,
                    stopwatch.ElapsedMilliseconds,
                    mutualMatches.Any() ? mutualMatches.Average(m => m.MutualScore) : 0f,
                    reverseLookupCount);

                return new MutualMatchResult
                {
                    Matches = mutualMatches,
                    TotalMutualMatches = mutualMatches.Count,
                    Metadata = new MutualMatchMetadata
                    {
                        SearchedAt = DateTime.UtcNow,
                        TargetEntityType = targetEntityType,
                        CandidatesEvaluated = forwardMatches.Matches.Count,
                        ReverseLookups = reverseLookupCount,
                        SearchDurationMs = stopwatch.ElapsedMilliseconds,
                        MinSimilarity = minSimilarity
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Error finding mutual matches for entity {EntityId}",
                    entityId);
                throw;
            }
        }
    }
}
