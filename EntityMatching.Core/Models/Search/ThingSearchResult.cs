using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Search
{
    /// <summary>
    /// Generic search result container for "things" (events, gifts, jobs, etc.)
    /// </summary>
    /// <typeparam name="T">Type of thing being returned (Event, Gift, Job, etc.)</typeparam>
    public class ThingSearchResult<T>
    {
        /// <summary>
        /// List of matched items with scores
        /// </summary>
        [JsonProperty(PropertyName = "matches")]
        public List<T> Matches { get; set; } = new List<T>();

        /// <summary>
        /// Total number of matches found
        /// </summary>
        [JsonProperty(PropertyName = "totalMatches")]
        public int TotalMatches { get; set; }

        /// <summary>
        /// Search metadata (timing, mode, queries generated, etc.)
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        public ThingSearchMetadata Metadata { get; set; } = new ThingSearchMetadata();
    }

    /// <summary>
    /// Metadata about thing search execution (events, gifts, jobs, etc.)
    /// Distinct from ProfileMatch.SearchMetadata which is for profile-to-profile search
    /// </summary>
    public class ThingSearchMetadata
    {
        /// <summary>
        /// Total number of results found before filtering/limiting
        /// </summary>
        [JsonProperty(PropertyName = "totalResults")]
        public int TotalResults { get; set; }

        /// <summary>
        /// Search mode used: "web_search", "embeddings", or "hybrid"
        /// </summary>
        [JsonProperty(PropertyName = "searchMode")]
        public string SearchMode { get; set; } = "";

        /// <summary>
        /// Number of search queries generated for this search
        /// </summary>
        [JsonProperty(PropertyName = "queriesGenerated")]
        public int QueriesGenerated { get; set; }

        /// <summary>
        /// How long the search took in milliseconds
        /// </summary>
        [JsonProperty(PropertyName = "searchDurationMs")]
        public long SearchDurationMs { get; set; }

        /// <summary>
        /// When this search was performed
        /// </summary>
        [JsonProperty(PropertyName = "searchedAt")]
        public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Profile ID that was searched for
        /// </summary>
        [JsonProperty(PropertyName = "profileId")]
        public string? ProfileId { get; set; }

        /// <summary>
        /// Number of results from embeddings (for hybrid mode)
        /// </summary>
        [JsonProperty(PropertyName = "embeddingResults")]
        public int EmbeddingResults { get; set; }

        /// <summary>
        /// Number of results from web search (for hybrid mode)
        /// </summary>
        [JsonProperty(PropertyName = "webSearchResults")]
        public int WebSearchResults { get; set; }
    }
}
