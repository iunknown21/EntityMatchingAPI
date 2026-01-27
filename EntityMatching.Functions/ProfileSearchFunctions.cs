using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Search;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Azure Functions for profile-based "thing" discovery (events, gifts, jobs, etc.)
    /// This is distinct from profile-to-profile matching (SearchFunctions.cs)
    /// </summary>
    public class ProfileSearchFunctions : BaseApiFunction
    {
        private readonly IThingDiscoveryService<EventSearchParams, Event> _eventDiscoveryService;

        public ProfileSearchFunctions(
            IThingDiscoveryService<EventSearchParams, Event> eventDiscoveryService,
            ILogger<ProfileSearchFunctions> logger) : base(logger)
        {
            _eventDiscoveryService = eventDiscoveryService ?? throw new ArgumentNullException(nameof(eventDiscoveryService));
        }

        /// <summary>
        /// Search for events based on a user profile
        /// POST /api/v1/profile-search/events
        ///
        /// Request body:
        /// {
        ///   "profileId": "user123",
        ///   "location": "Seattle, WA",
        ///   "radiusMiles": 15,
        ///   "startDate": "2025-01-01",
        ///   "endDate": "2025-01-31",
        ///   "category": "music",
        ///   "maxResults": 20,
        ///   "searchMode": "hybrid"  // "web", "embeddings", or "hybrid"
        /// }
        ///
        /// Response:
        /// {
        ///   "matches": [
        ///     {
        ///       "id": "evt123",
        ///       "title": "Jazz Concert at Blue Note",
        ///       "description": "Live jazz performance...",
        ///       "location": "Seattle, WA",
        ///       "eventDate": "2025-01-15T20:00:00Z",
        ///       "category": "music",
        ///       "price": 35.00,
        ///       "externalUrl": "https://...",
        ///       "matchScore": 0.85,
        ///       "matchReasons": { "music": "Matches your interest in jazz" },
        ///       "scoringBreakdown": { "Safety": 1.0, "Social": 0.8, "Sensory": 0.7, "Interest": 0.9, "Practical": 0.8 }
        ///     }
        ///   ],
        ///   "totalMatches": 15,
        ///   "metadata": {
        ///     "totalResults": 15,
        ///     "searchMode": "hybrid",
        ///     "queriesGenerated": 6,
        ///     "searchDurationMs": 2500,
        ///     "profileId": "user123",
        ///     "embeddingResults": 0,
        ///     "webSearchResults": 15
        ///   }
        /// }
        /// </summary>
        [Function("SearchEventsForEntity")]
        public async Task<HttpResponseData> SearchEvents(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/profile-search/events")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing event search request");

                // Parse request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonHelper.DeserializeApi<EventSearchRequest>(requestBody);

                // Validate
                if (request == null)
                {
                    _logger.LogWarning("Invalid request body");
                    return CreateBadRequestResponse(req, "Invalid request body");
                }

                if (string.IsNullOrEmpty(request.ProfileId))
                {
                    _logger.LogWarning("Missing profileId in request");
                    return CreateBadRequestResponse(req, "profileId is required");
                }

                if (string.IsNullOrEmpty(request.Location))
                {
                    _logger.LogWarning("Missing location in request");
                    return CreateBadRequestResponse(req, "location is required");
                }

                // Convert request to parameters
                var parameters = request.ToParams();

                // Execute search based on mode
                ThingSearchResult<Event> result;
                var searchMode = request.SearchMode?.ToLower() ?? "hybrid";

                switch (searchMode)
                {
                    case "web":
                        _logger.LogInformation("Executing web search for profile {ProfileId}", request.ProfileId);
                        result = await _eventDiscoveryService.DiscoverViaWebSearchAsync(
                            request.ProfileId,
                            parameters,
                            request.MaxResults);
                        break;

                    case "embeddings":
                        _logger.LogInformation("Executing embedding search for profile {ProfileId}", request.ProfileId);
                        result = await _eventDiscoveryService.DiscoverViaEmbeddingsAsync(
                            request.ProfileId,
                            parameters,
                            request.MaxResults);
                        break;

                    case "hybrid":
                    default:
                        _logger.LogInformation("Executing hybrid search for profile {ProfileId}", request.ProfileId);
                        result = await _eventDiscoveryService.DiscoverHybridAsync(
                            request.ProfileId,
                            parameters,
                            request.MaxResults);
                        break;
                }

                _logger.LogInformation(
                    "Event search completed: {MatchCount} matches found for profile {ProfileId} in {DurationMs}ms",
                    result.TotalMatches,
                    request.ProfileId,
                    result.Metadata.SearchDurationMs);

                // Return results
                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event search request");
                return CreateErrorResponse(req, "An error occurred processing your request");
            }
        }
    }

    /// <summary>
    /// Request model for event search
    /// </summary>
    public class EventSearchRequest
    {
        public string ProfileId { get; set; } = "";
        public string Location { get; set; } = "";
        public int RadiusMiles { get; set; } = 15;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30);
        public string? Category { get; set; }
        public int MaxResults { get; set; } = 20;
        public string SearchMode { get; set; } = "hybrid";  // "web", "embeddings", or "hybrid"
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public EventSearchParams ToParams() => new EventSearchParams
        {
            Location = Location,
            RadiusMiles = RadiusMiles,
            StartDate = StartDate,
            EndDate = EndDate,
            Category = Category,
            MaxResults = MaxResults,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            SearchMode = SearchMode?.ToLower() switch
            {
                "web" => Core.Models.Search.SearchMode.WebSearch,
                "embeddings" => Core.Models.Search.SearchMode.Embeddings,
                _ => Core.Models.Search.SearchMode.Hybrid
            }
        };
    }
}
