using EntityMatching.Core.Models.Search;
using EntityMatching.SDK.Utils;

namespace EntityMatching.SDK.Endpoints;

/// <summary>
/// Search endpoint wrapper
/// Handles semantic search with attribute filtering
/// </summary>
public class SearchEndpoint
{
    private readonly HttpClientHelper _http;

    internal SearchEndpoint(HttpClientHelper http)
    {
        _http = http;
    }

    /// <summary>
    /// Search profiles using semantic similarity and attribute filters
    /// Privacy-first: Returns only profile IDs and similarity scores
    /// </summary>
    /// <param name="request">Search query with optional filters</param>
    /// <returns>Search results with profile IDs and similarity scores</returns>
    public async Task<SearchResult> SearchAsync(SearchRequest request)
    {
        return await _http.PostAsync<SearchResult>("/api/v1/entities/search", request);
    }
}
