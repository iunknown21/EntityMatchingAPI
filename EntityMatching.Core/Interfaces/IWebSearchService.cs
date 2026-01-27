using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for performing web searches using AI with web search capabilities (e.g., Groq)
    /// Generic service that can search for any type of thing (events, gifts, jobs, etc.)
    /// </summary>
    public interface IWebSearchService
    {
        /// <summary>
        /// Search for things using natural language query with AI-powered web search
        /// </summary>
        /// <typeparam name="TResult">The type of result to return (e.g., Event, Gift, Job)</typeparam>
        /// <param name="query">Natural language search query</param>
        /// <param name="context">Search context including thing type, max results, and metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of results matching the query</returns>
        Task<IEnumerable<TResult>> SearchAsync<TResult>(
            string query,
            SearchContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Context for a web search request
    /// </summary>
    public class SearchContext
    {
        /// <summary>
        /// Type of thing being searched for (e.g., "events", "gifts", "jobs")
        /// Used to customize the AI prompt and result parsing
        /// </summary>
        public string ThingType { get; set; } = "";

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxResults { get; set; } = 20;

        /// <summary>
        /// Optional additional metadata for the search
        /// Can include location, date range, price range, etc.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Optional system prompt override for customizing AI behavior
        /// If not provided, uses default system prompt for the thing type
        /// </summary>
        public string? SystemPromptOverride { get; set; }
    }

    /// <summary>
    /// Configuration for web search service
    /// </summary>
    public class WebSearchConfig
    {
        /// <summary>
        /// Maximum number of retry attempts for failed API calls
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Base delay in milliseconds before retrying after rate limit
        /// Uses exponential backoff: delay * 2^(attempt-1)
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Maximum rate limit per minute for API calls
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 10;

        /// <summary>
        /// Groq model to use for web search
        /// </summary>
        public string Model { get; set; } = "groq/compound";

        /// <summary>
        /// Temperature for AI responses (0.0 to 1.0)
        /// Lower = more deterministic, Higher = more creative
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Maximum tokens for AI response
        /// </summary>
        public int MaxTokens { get; set; } = 6000;
    }
}
