using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Search;
using System.Collections.Generic;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Generic strategy for converting person entities into targeted search queries and scoring
    /// Works with any "thing" type: events, gifts, jobs, housing, travel, etc.
    /// </summary>
    /// <typeparam name="TParams">Type of search parameters (e.g., EventSearchParams, GiftSearchParams)</typeparam>
    /// <typeparam name="TResult">Type of result being searched for (e.g., Event, Gift, Job)</typeparam>
    public interface IThingSearchStrategy<TParams, TResult>
    {
        /// <summary>
        /// Generate targeted search queries based on person entity analysis
        /// Converts rich person data into 6-8 specific search terms
        /// </summary>
        /// <param name="entity">Person entity with preferences and requirements</param>
        /// <param name="parameters">Search parameters (location, date, category, etc.)</param>
        /// <returns>List of targeted search queries optimized for web search</returns>
        List<string> GenerateSearchQueries(Entity entity, TParams parameters);

        /// <summary>
        /// Extract critical safety and accommodation requirements from person entity
        /// These must be validated before showing results to user
        /// </summary>
        /// <param name="entity">Person entity to analyze</param>
        /// <returns>List of requirements ordered by importance (Critical first)</returns>
        List<SafetyRequirement> GetCriticalSafetyRequirements(Entity entity);

        /// <summary>
        /// Get scoring weights for multi-dimensional evaluation
        /// Weights should sum to 1.0 and represent importance of each dimension
        /// </summary>
        /// <param name="entity">Person entity to analyze for weight adjustments</param>
        /// <returns>Dictionary of dimension names to weights (e.g., {"Safety": 0.35, "Social": 0.25})</returns>
        Dictionary<string, double> GetScoringWeights(Entity entity);

        /// <summary>
        /// Calculate match score for a result against a person entity
        /// Uses multi-dimensional scoring based on weights
        /// </summary>
        /// <param name="result">The thing being scored (event, gift, job, etc.)</param>
        /// <param name="entity">Person entity to match against</param>
        /// <param name="weights">Scoring weights for each dimension</param>
        /// <returns>Match score from 0.0 to 1.0</returns>
        double CalculateMatchScore(TResult result, Entity entity, Dictionary<string, double> weights);

        /// <summary>
        /// Generate human-readable person entity summary for AI prompts
        /// Summarizes key preferences and requirements
        /// </summary>
        /// <param name="entity">Person entity to summarize</param>
        /// <param name="parameters">Search parameters for context</param>
        /// <returns>Text summary suitable for AI prompt inclusion</returns>
        string GenerateEntitySummary(Entity entity, TParams parameters);
    }
}
