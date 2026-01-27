using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Matching
{
    /// <summary>
    /// Status of a match request throughout its lifecycle
    /// </summary>
    public enum MatchStatus
    {
        /// <summary>
        /// Match request sent, awaiting response
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Target profile viewed the request
        /// </summary>
        Viewed = 1,

        /// <summary>
        /// Target profile indicated interest
        /// </summary>
        Interested = 2,

        /// <summary>
        /// Target profile declined the request
        /// </summary>
        Declined = 3,

        /// <summary>
        /// Both profiles mutually agreed (match established)
        /// </summary>
        Connected = 4,

        /// <summary>
        /// Request expired due to timeout
        /// </summary>
        Expired = 5,

        /// <summary>
        /// Request was withdrawn by the requester
        /// </summary>
        Withdrawn = 6
    }

    /// <summary>
    /// Represents a match/connection request from one profile to another
    /// Domain-agnostic: works for dating, networking, hiring, etc.
    /// </summary>
    public class MatchRequest
    {
        /// <summary>
        /// Unique identifier for this match request
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// ID of the profile that will receive the match request
        /// Cosmos DB partition key - allows efficient queries for incoming requests
        /// </summary>
        [JsonProperty(PropertyName = "targetId")]
        public string TargetId { get; set; } = "";

        /// <summary>
        /// ID of the profile sending the match request
        /// </summary>
        [JsonProperty(PropertyName = "requesterId")]
        public string RequesterId { get; set; } = "";

        /// <summary>
        /// Current status of the match request
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public MatchStatus Status { get; set; } = MatchStatus.Pending;

        /// <summary>
        /// Optional message from requester to target
        /// Example: "Hi! I think we'd be a great match based on our shared interests"
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string? Message { get; set; }

        /// <summary>
        /// Optional response message from target to requester
        /// </summary>
        [JsonProperty(PropertyName = "responseMessage")]
        public string? ResponseMessage { get; set; }

        /// <summary>
        /// When this match request was created
        /// </summary>
        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the target profile first viewed this request
        /// Null until status transitions to Viewed or later
        /// </summary>
        [JsonProperty(PropertyName = "viewedAt")]
        public DateTime? ViewedAt { get; set; }

        /// <summary>
        /// When the status last changed
        /// </summary>
        [JsonProperty(PropertyName = "lastStatusChangeAt")]
        public DateTime LastStatusChangeAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this request expires (if not responded to)
        /// Null = no expiration
        /// Applications can set custom expiration policies (e.g., 30 days)
        /// </summary>
        [JsonProperty(PropertyName = "expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Application-specific metadata
        /// Example: { "source": "search_results", "similarity_score": 0.85, "context": "shared_interests" }
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Check if this request is active (not in a terminal state)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsActive => Status == MatchStatus.Pending ||
                                 Status == MatchStatus.Viewed ||
                                 Status == MatchStatus.Interested;

        /// <summary>
        /// Check if this request has been resolved (terminal state)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsResolved => Status == MatchStatus.Connected ||
                                   Status == MatchStatus.Declined ||
                                   Status == MatchStatus.Expired ||
                                   Status == MatchStatus.Withdrawn;

        /// <summary>
        /// Check if this request has resulted in a successful match
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsSuccessful => Status == MatchStatus.Connected;

        /// <summary>
        /// Check if this request is expired based on ExpiresAt timestamp
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsExpiredByTime => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value && Status != MatchStatus.Expired;
    }
}
