using EntityMatching.Core.Models.Search;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Generic service for discovering "things" (events, gifts, jobs, etc.) based on user profiles
    /// Supports multiple discovery modes: Web search, stored embeddings, or hybrid
    /// </summary>
    /// <typeparam name="TParams">Type of search parameters (e.g., EventSearchParams, GiftSearchParams)</typeparam>
    /// <typeparam name="TResult">Type of result being discovered (e.g., Event, Gift, Job)</typeparam>
    public interface IThingDiscoveryService<TParams, TResult>
    {
        /// <summary>
        /// Discover things using real-time web search (Groq API)
        /// Always fresh results, no storage required
        /// </summary>
        /// <param name="profileId">User profile ID to match against</param>
        /// <param name="parameters">Search parameters (location, date, category, etc.)</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>Search results with match scores</returns>
        Task<ThingSearchResult<TResult>> DiscoverViaWebSearchAsync(
            string profileId,
            TParams parameters,
            int limit = 20);

        /// <summary>
        /// Discover things using stored embeddings (semantic similarity)
        /// Fast, cached results from pre-indexed items
        /// </summary>
        /// <param name="profileId">User profile ID to match against</param>
        /// <param name="parameters">Search parameters (location, date, category, etc.)</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>Search results with match scores</returns>
        Task<ThingSearchResult<TResult>> DiscoverViaEmbeddingsAsync(
            string profileId,
            TParams parameters,
            int limit = 20);

        /// <summary>
        /// Hybrid approach: Try embeddings first, supplement with web search
        /// Best of both worlds - fast cached results + fresh real-time discoveries
        /// </summary>
        /// <param name="profileId">User profile ID to match against</param>
        /// <param name="parameters">Search parameters (location, date, category, etc.)</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>Search results with match scores</returns>
        Task<ThingSearchResult<TResult>> DiscoverHybridAsync(
            string profileId,
            TParams parameters,
            int limit = 20);
    }
}
