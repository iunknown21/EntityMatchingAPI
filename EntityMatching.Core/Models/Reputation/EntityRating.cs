using Newtonsoft.Json;
using System;

namespace EntityMatching.Core.Models.Reputation
{
    /// <summary>
    /// Represents a rating given by one entity to another
    /// Domain-agnostic: can be used for any context (dating, jobs, services, etc.)
    /// </summary>
    public class EntityRating
    {
        /// <summary>
        /// Unique identifier for this rating
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// ID of the entity being rated (the target)
        /// Cosmos DB partition key - JSON property name kept as "profileId" for database compatibility
        /// </summary>
        [JsonProperty(PropertyName = "profileId")]
        public string EntityId { get; set; } = "";

        /// <summary>
        /// ID of the entity giving the rating (the rater)
        /// JSON property name kept as "ratedByProfileId" for database compatibility
        /// </summary>
        [JsonProperty(PropertyName = "ratedByProfileId")]
        public string RatedByEntityId { get; set; } = "";

        /// <summary>
        /// Overall numeric rating (e.g., 1-5 stars, 1-10 score)
        /// Application defines the scale
        /// </summary>
        [JsonProperty(PropertyName = "overallRating")]
        public double OverallRating { get; set; }

        /// <summary>
        /// Optional category/attribute ratings
        /// Examples:
        /// - Dating: { "communication": 4.5, "trustworthiness": 5.0, "compatibility": 4.0 }
        /// - Jobs: { "professionalism": 5.0, "technical_skills": 4.5, "teamwork": 4.0 }
        /// - Services: { "quality": 4.5, "timeliness": 5.0, "communication": 4.0 }
        /// </summary>
        [JsonProperty(PropertyName = "categoryRatings")]
        public System.Collections.Generic.Dictionary<string, double>? CategoryRatings { get; set; }

        /// <summary>
        /// Optional textual review/feedback
        /// </summary>
        [JsonProperty(PropertyName = "review")]
        public string? Review { get; set; }

        /// <summary>
        /// Whether this rating is verified (e.g., both entities matched/connected)
        /// Verified ratings may have higher weight in reputation calculation
        /// </summary>
        [JsonProperty(PropertyName = "isVerified")]
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Whether this rating is public (visible to others) or private (for internal calculations only)
        /// </summary>
        [JsonProperty(PropertyName = "isPublic")]
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// When this rating was created
        /// </summary>
        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this rating was last modified
        /// </summary>
        [JsonProperty(PropertyName = "lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Application-specific metadata
        /// Example: { "interaction_date": "2024-01-15", "context": "first_date", "verified_via": "mutual_match" }
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public System.Collections.Generic.Dictionary<string, object>? Metadata { get; set; }
    }
}
