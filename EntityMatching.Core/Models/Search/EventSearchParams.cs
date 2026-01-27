using Newtonsoft.Json;
using System;

namespace EntityMatching.Core.Models.Search
{
    /// <summary>
    /// Parameters for event search requests
    /// </summary>
    public class EventSearchParams
    {
        /// <summary>
        /// Location for event search (city, neighborhood, venue)
        /// </summary>
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; } = "";

        /// <summary>
        /// Radius in miles to search around location
        /// </summary>
        [JsonProperty(PropertyName = "radiusMiles")]
        public int RadiusMiles { get; set; } = 15;

        /// <summary>
        /// Start date for event search
        /// </summary>
        [JsonProperty(PropertyName = "startDate")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// End date for event search
        /// </summary>
        [JsonProperty(PropertyName = "endDate")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30);

        /// <summary>
        /// Optional category filter (e.g., "music", "food", "outdoor", "culture")
        /// </summary>
        [JsonProperty(PropertyName = "category")]
        public string? Category { get; set; }

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        [JsonProperty(PropertyName = "maxResults")]
        public int MaxResults { get; set; } = 20;

        /// <summary>
        /// Search mode: WebSearch, Embeddings, or Hybrid
        /// </summary>
        [JsonProperty(PropertyName = "searchMode")]
        public SearchMode SearchMode { get; set; } = SearchMode.Hybrid;

        /// <summary>
        /// Optional price range filter (minimum price)
        /// </summary>
        [JsonProperty(PropertyName = "minPrice")]
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Optional price range filter (maximum price)
        /// </summary>
        [JsonProperty(PropertyName = "maxPrice")]
        public decimal? MaxPrice { get; set; }
    }

    /// <summary>
    /// Search mode for thing discovery
    /// </summary>
    public enum SearchMode
    {
        /// <summary>
        /// Real-time web search only using Groq
        /// </summary>
        WebSearch,

        /// <summary>
        /// Stored embeddings only (semantic similarity)
        /// </summary>
        Embeddings,

        /// <summary>
        /// Hybrid: Try embeddings first, supplement with web search
        /// </summary>
        Hybrid
    }
}
