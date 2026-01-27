using Newtonsoft.Json;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Search
{
    /// <summary>
    /// Represents a single entity match result with similarity score
    /// </summary>
    public class EntityMatch
    {
        [JsonProperty(PropertyName = "entityId")]
        public string EntityId { get; set; } = "";

        [JsonProperty(PropertyName = "similarityScore")]
        public float SimilarityScore { get; set; }

        /// <summary>
        /// Optional - full entity data (populated if includeEntities=true)
        /// DEPRECATED: Will be removed in future version for privacy protection
        /// Use entityId to fetch full entity separately
        /// </summary>
        [JsonProperty(PropertyName = "entity")]
        public Entity? Entity { get; set; }

        /// <summary>
        /// Attribute values that matched the search filters
        /// Provides transparency about why this entity matched
        /// Only includes fields visible to the requesting user
        /// Example: { "gender": "male", "naturePreferences.hasPets": true, "preferences.favoriteFoods": ["hamburgers"] }
        /// </summary>
        [JsonProperty(PropertyName = "matchedAttributes")]
        public Dictionary<string, object>? MatchedAttributes { get; set; }

        // Metadata for debugging and display
        [JsonProperty(PropertyName = "entityName")]
        public string EntityName { get; set; } = "";

        [JsonProperty(PropertyName = "entityLastModified")]
        public DateTime? EntityLastModified { get; set; }

        [JsonProperty(PropertyName = "embeddingDimensions")]
        public int? EmbeddingDimensions { get; set; }
    }

    /// <summary>
    /// Complete search result with matches and metadata
    /// </summary>
    public class SearchResult
    {
        [JsonProperty(PropertyName = "matches")]
        public List<EntityMatch> Matches { get; set; } = new();

        [JsonProperty(PropertyName = "totalMatches")]
        public int TotalMatches { get; set; }

        [JsonProperty(PropertyName = "metadata")]
        public SearchMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metadata about the search operation
    /// </summary>
    public class SearchMetadata
    {
        [JsonProperty(PropertyName = "searchedAt")]
        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "totalEmbeddingsSearched")]
        public int TotalEmbeddingsSearched { get; set; }

        [JsonProperty(PropertyName = "minSimilarity")]
        public float MinSimilarity { get; set; }

        [JsonProperty(PropertyName = "requestedLimit")]
        public int RequestedLimit { get; set; }

        [JsonProperty(PropertyName = "searchDurationMs")]
        public long SearchDurationMs { get; set; }
    }

    /// <summary>
    /// Request model for text-based entity search with attribute filtering
    /// Supports hybrid search: semantic similarity + structured attribute filters
    /// </summary>
    public class SearchRequest
    {
        /// <summary>
        /// Text query for semantic similarity search
        /// Example: "loves hiking and outdoor adventures"
        /// </summary>
        [JsonProperty(PropertyName = "query")]
        public string Query { get; set; } = "";

        /// <summary>
        /// Structured attribute filters to apply to search results
        /// Example: { "logicalOperator": "And", "filters": [{ "fieldPath": "gender", "operator": "Equals", "value": "male" }] }
        /// </summary>
        [JsonProperty(PropertyName = "attributeFilters")]
        public FilterGroup? AttributeFilters { get; set; }

        /// <summary>
        /// Metadata filters to apply to search results
        /// Entities must have matching metadata keys and values to be included
        /// Example: { "verification": { "email_verified": true }, "trust_score": 0.85 }
        /// </summary>
        [JsonProperty(PropertyName = "metadataFilters")]
        public Dictionary<string, object>? MetadataFilters { get; set; }

        /// <summary>
        /// Minimum overall reputation score (e.g., 4.0)
        /// Entities with lower scores will be excluded
        /// </summary>
        [JsonProperty(PropertyName = "minReputationScore")]
        public double? MinReputationScore { get; set; }

        /// <summary>
        /// Minimum number of ratings required
        /// Entities with fewer ratings will be excluded
        /// </summary>
        [JsonProperty(PropertyName = "minRatingCount")]
        public int? MinRatingCount { get; set; }

        /// <summary>
        /// Minimum confidence score (0.0-1.0)
        /// Filters out entities with low confidence (few ratings)
        /// </summary>
        [JsonProperty(PropertyName = "minConfidenceScore")]
        public double? MinConfidenceScore { get; set; }

        /// <summary>
        /// User ID of the requesting user (for privacy enforcement)
        /// If null, only Public fields are searchable (anonymous access)
        /// </summary>
        [JsonProperty(PropertyName = "requestingUserId")]
        public string? RequestingUserId { get; set; }

        /// <summary>
        /// Whether to enforce field-level privacy settings
        /// If true, only fields visible to requestingUserId are searchable
        /// Default: true (recommended for production)
        /// </summary>
        [JsonProperty(PropertyName = "enforcePrivacy")]
        public bool EnforcePrivacy { get; set; } = true;

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        [JsonProperty(PropertyName = "limit")]
        public int? Limit { get; set; }

        /// <summary>
        /// Minimum similarity score threshold (0.0 to 1.0)
        /// </summary>
        [JsonProperty(PropertyName = "minSimilarity")]
        public float? MinSimilarity { get; set; }

        /// <summary>
        /// Whether to populate full Entity objects in results
        /// DEPRECATED: Will be removed in future version for privacy protection
        /// Recommended: Use entityId to fetch full entities separately
        /// </summary>
        [JsonProperty(PropertyName = "includeEntities")]
        public bool? IncludeEntities { get; set; }
    }
}
