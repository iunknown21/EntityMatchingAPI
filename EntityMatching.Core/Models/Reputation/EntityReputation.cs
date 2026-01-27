using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Reputation
{
    /// <summary>
    /// Aggregated reputation scores for an entity
    /// Calculated from EntityRating records
    /// Domain-agnostic: works for any application context
    /// </summary>
    public class EntityReputation
    {
        /// <summary>
        /// Unique identifier for this reputation record
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// ID of the entity this reputation belongs to
        /// Cosmos DB partition key - JSON property name kept as "profileId" for database compatibility
        /// </summary>
        [JsonProperty(PropertyName = "profileId")]
        public string EntityId { get; set; } = "";

        /// <summary>
        /// Overall average rating across all ratings
        /// </summary>
        [JsonProperty(PropertyName = "overallScore")]
        public double OverallScore { get; set; }

        /// <summary>
        /// Total number of ratings received
        /// </summary>
        [JsonProperty(PropertyName = "totalRatings")]
        public int TotalRatings { get; set; }

        /// <summary>
        /// Number of verified ratings (higher trust)
        /// </summary>
        [JsonProperty(PropertyName = "verifiedRatings")]
        public int VerifiedRatings { get; set; }

        /// <summary>
        /// Average rating from verified raters only
        /// May differ from overallScore if verification matters
        /// </summary>
        [JsonProperty(PropertyName = "verifiedScore")]
        public double? VerifiedScore { get; set; }

        /// <summary>
        /// Breakdown of reputation by category/attribute
        /// Example:
        /// - Dating: [{ "category": "communication", "score": 4.5, "count": 10 }]
        /// - Jobs: [{ "category": "technical_skills", "score": 4.8, "count": 15 }]
        /// </summary>
        [JsonProperty(PropertyName = "categoryScores")]
        public List<CategoryReputation> CategoryScores { get; set; } = new();

        /// <summary>
        /// Confidence score (0.0-1.0) based on number of ratings
        /// More ratings = higher confidence
        /// Formula: min(totalRatings / TARGET_RATINGS, 1.0)
        /// </summary>
        [JsonProperty(PropertyName = "confidenceScore")]
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// When this reputation was last calculated
        /// </summary>
        [JsonProperty(PropertyName = "lastCalculated")]
        public DateTime LastCalculated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Application-specific metadata
        /// Example: { "calculation_method": "weighted_average", "min_ratings_threshold": 3 }
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Reputation score for a specific category/attribute
    /// </summary>
    public class CategoryReputation
    {
        /// <summary>
        /// Category name (e.g., "communication", "technical_skills", "quality")
        /// </summary>
        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; } = "";

        /// <summary>
        /// Average score for this category
        /// </summary>
        [JsonProperty(PropertyName = "score")]
        public double Score { get; set; }

        /// <summary>
        /// Number of ratings for this category
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
    }
}
