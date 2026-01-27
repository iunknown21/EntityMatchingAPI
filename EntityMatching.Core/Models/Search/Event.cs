using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Search
{
    /// <summary>
    /// Represents an event discovered via search
    /// Can come from web search or stored embeddings
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Unique identifier for this event
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Event title/name
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; } = "";

        /// <summary>
        /// Detailed description of the event
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Venue name and address
        /// </summary>
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; } = "";

        /// <summary>
        /// When the event takes place
        /// </summary>
        [JsonProperty(PropertyName = "eventDate")]
        public DateTime? EventDate { get; set; }

        /// <summary>
        /// Event category (e.g., "music", "food", "outdoor", "culture")
        /// </summary>
        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; } = "";

        /// <summary>
        /// Ticket/admission price (null if free or price not available)
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// URL to event details or ticket purchase
        /// </summary>
        [JsonProperty(PropertyName = "externalUrl")]
        public string? ExternalUrl { get; set; }

        /// <summary>
        /// Source of this event (e.g., "web_search", "embedding_match", "eventbrite")
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; } = "web_search";

        // Matching metadata (populated during scoring)

        /// <summary>
        /// Match score from 0.0 to 1.0 indicating how well this matches the profile
        /// </summary>
        [JsonProperty(PropertyName = "matchScore")]
        public double MatchScore { get; set; }

        /// <summary>
        /// Human-readable reasons why this event matches the profile
        /// Key = dimension (e.g., "safety", "social"), Value = reason
        /// </summary>
        [JsonProperty(PropertyName = "matchReasons")]
        public Dictionary<string, string> MatchReasons { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Detailed scoring breakdown by dimension
        /// Key = dimension (e.g., "Safety", "Social"), Value = score 0.0-1.0
        /// </summary>
        [JsonProperty(PropertyName = "scoringBreakdown")]
        public Dictionary<string, double> ScoringBreakdown { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Tags/keywords associated with this event
        /// </summary>
        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }
}
