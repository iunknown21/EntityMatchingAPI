using Newtonsoft.Json;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Search
{
    /// <summary>
    /// Represents a mutual match between two entities
    /// Both entities match each other above the similarity threshold
    /// Enables bidirectional discovery (Job finds Person, Person finds Job)
    /// </summary>
    public class MutualMatch
    {
        /// <summary>
        /// ID of the first entity (the one initiating the search)
        /// </summary>
        [JsonProperty(PropertyName = "entityAId")]
        public string EntityAId { get; set; } = "";

        /// <summary>
        /// ID of the second entity (the one being matched)
        /// </summary>
        [JsonProperty(PropertyName = "entityBId")]
        public string EntityBId { get; set; } = "";

        /// <summary>
        /// Type of entity A
        /// </summary>
        [JsonProperty(PropertyName = "entityAType")]
        public EntityType EntityAType { get; set; }

        /// <summary>
        /// Type of entity B
        /// </summary>
        [JsonProperty(PropertyName = "entityBType")]
        public EntityType EntityBType { get; set; }

        /// <summary>
        /// Name of entity A (for display purposes)
        /// </summary>
        [JsonProperty(PropertyName = "entityAName")]
        public string EntityAName { get; set; } = "";

        /// <summary>
        /// Name of entity B (for display purposes)
        /// </summary>
        [JsonProperty(PropertyName = "entityBName")]
        public string EntityBName { get; set; } = "";

        /// <summary>
        /// Similarity score from A's perspective (how well B matches A)
        /// Range: 0.0 to 1.0
        /// </summary>
        [JsonProperty(PropertyName = "aToB_Score")]
        public float AToB_Score { get; set; }

        /// <summary>
        /// Similarity score from B's perspective (how well A matches B)
        /// Range: 0.0 to 1.0
        /// </summary>
        [JsonProperty(PropertyName = "bToA_Score")]
        public float BToA_Score { get; set; }

        /// <summary>
        /// Overall mutual match score
        /// Calculated as: average of AToB_Score and BToA_Score
        /// Can be customized to use different formulas (min, weighted average, etc.)
        /// </summary>
        [JsonProperty(PropertyName = "mutualScore")]
        public float MutualScore { get; set; }

        /// <summary>
        /// Type of match
        /// "Mutual": both entities match each other
        /// "OneWay": only one entity matches the other (not used in mutual matching)
        /// </summary>
        [JsonProperty(PropertyName = "matchType")]
        public string MatchType { get; set; } = "Mutual";

        /// <summary>
        /// When this mutual match was detected
        /// </summary>
        [JsonProperty(PropertyName = "detectedAt")]
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional: attributes from entity B that matched entity A's filters
        /// Provides transparency about why the match occurred
        /// </summary>
        [JsonProperty(PropertyName = "matchedAttributes")]
        public Dictionary<string, object>? MatchedAttributes { get; set; }
    }

    /// <summary>
    /// Complete mutual match search result
    /// </summary>
    public class MutualMatchResult
    {
        [JsonProperty(PropertyName = "matches")]
        public List<MutualMatch> Matches { get; set; } = new();

        [JsonProperty(PropertyName = "totalMutualMatches")]
        public int TotalMutualMatches { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public MutualMatchMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metadata about the mutual match operation
    /// </summary>
    public class MutualMatchMetadata
    {
        [JsonProperty(PropertyName = "searchedAt")]
        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Entity type of the source entity (the one initiating the search)
        /// </summary>
        [JsonProperty(PropertyName = "sourceEntityType")]
        public EntityType SourceEntityType { get; set; }

        /// <summary>
        /// Optional filter for target entity type
        /// If specified, only matches entities of this type
        /// If null, matches all entity types
        /// </summary>
        [JsonProperty(PropertyName = "targetEntityType")]
        public EntityType? TargetEntityType { get; set; }

        /// <summary>
        /// Number of candidate entities evaluated (forward matches)
        /// </summary>
        [JsonProperty(PropertyName = "candidatesEvaluated")]
        public int CandidatesEvaluated { get; set; }

        /// <summary>
        /// Number of reverse lookups performed (checking if candidates also match source)
        /// </summary>
        [JsonProperty(PropertyName = "reverseLookups")]
        public int ReverseLookups { get; set; }

        /// <summary>
        /// Total time taken to find mutual matches (in milliseconds)
        /// </summary>
        [JsonProperty(PropertyName = "searchDurationMs")]
        public long SearchDurationMs { get; set; }

        /// <summary>
        /// Minimum similarity threshold used
        /// </summary>
        [JsonProperty(PropertyName = "minSimilarity")]
        public float MinSimilarity { get; set; }
    }
}
