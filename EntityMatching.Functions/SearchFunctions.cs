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
    /// Azure Functions for similarity search operations
    /// Endpoints: /api/v1/profiles/search, /api/v1/profiles/{id}/similar
    /// </summary>
    public class SearchFunctions : BaseApiFunction
    {
        private readonly ISimilaritySearchService _searchService;

        public SearchFunctions(
            ISimilaritySearchService searchService,
            ILogger<SearchFunctions> logger) : base(logger)
        {
            _searchService = searchService;
        }

        #region POST /api/v1/profiles/search

        // OPTIONS handler for CORS preflight
        [Function("SearchEntitiesOptions")]
        public HttpResponseData SearchEntitiesOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/search")]
            HttpRequestData req)
        {
            _logger.LogInformation("OPTIONS preflight request received for /v1/profiles/search");
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Search for profiles similar to a text query with optional attribute filtering
        /// POST /api/v1/profiles/search
        /// Body: {
        ///   "query": "loves hiking and outdoors",
        ///   "attributeFilters": { "logicalOperator": "And", "filters": [...] },
        ///   "requestingUserId": "user-123",
        ///   "enforcePrivacy": true,
        ///   "limit": 10,
        ///   "minSimilarity": 0.5,
        ///   "includeProfiles": false
        /// }
        /// </summary>
        [Function("SearchEntities")]
        public async Task<HttpResponseData> SearchEntities(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/entities/search")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Received profile search request");

                // Parse request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                // Check if body is empty before deserializing
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return CreateBadRequestResponse(req, "Request body is required");
                }

                var searchRequest = JsonHelper.DeserializeApi<SearchRequest>(requestBody);

                // Validate
                if (searchRequest == null)
                {
                    return CreateBadRequestResponse(req, "Invalid request body");
                }

                if (string.IsNullOrWhiteSpace(searchRequest.Query))
                {
                    return CreateBadRequestResponse(req, "Query is required");
                }

                // Execute search with attribute/metadata filters and privacy enforcement
                var result = await _searchService.SearchByQueryAsync(
                    searchRequest.Query,
                    searchRequest.Limit ?? 10,
                    searchRequest.MinSimilarity ?? 0.5f,
                    searchRequest.IncludeEntities ?? false,
                    searchRequest.AttributeFilters,
                    searchRequest.MetadataFilters,
                    searchRequest.RequestingUserId,
                    searchRequest.EnforcePrivacy);

                _logger.LogInformation(
                    "Search completed: found {MatchCount} matches for query '{Query}' (hasAttrFilters={HasAttrFilters}, hasMetadataFilters={HasMetadataFilters}, enforcePrivacy={EnforcePrivacy})",
                    result.TotalMatches, searchRequest.Query,
                    searchRequest.AttributeFilters?.HasFilters ?? false,
                    (searchRequest.MetadataFilters?.Count ?? 0) > 0,
                    searchRequest.EnforcePrivacy);

                // Return results
                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid search operation: {Message}", ex.Message);
                return CreateBadRequestResponse(req, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching profiles");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region GET /api/v1/profiles/{entityId}/similar

        // OPTIONS handler for CORS preflight
        [Function("GetSimilarEntitiesOptions")]
        public HttpResponseData GetSimilarEntitiesOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{entityId}/similar")]
            HttpRequestData req,
            string entityId)
        {
            _logger.LogInformation("OPTIONS preflight request received for /v1/profiles/{entityId}/similar", entityId);
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Find profiles similar to a given profile
        /// GET /api/v1/profiles/{entityId}/similar?limit=10&minSimilarity=0.5&includeProfiles=false
        /// </summary>
        [Function("GetSimilarEntities")]
        public async Task<HttpResponseData> GetSimilarEntities(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/entities/{entityId}/similar")]
            HttpRequestData req,
            string entityId)
        {
            try
            {
                _logger.LogInformation("Finding similar profiles for {entityId}", entityId);

                // Parse query parameters
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

                var limitStr = query["limit"];
                var limit = string.IsNullOrEmpty(limitStr) ? 10 : int.Parse(limitStr);

                var minSimStr = query["minSimilarity"];
                var minSimilarity = string.IsNullOrEmpty(minSimStr) ? 0.5f : float.Parse(minSimStr);

                var includeProfilesStr = query["includeProfiles"];
                var includeProfiles = !string.IsNullOrEmpty(includeProfilesStr) && bool.Parse(includeProfilesStr);

                // Execute similarity search
                var result = await _searchService.FindSimilarEntitiesAsync(
                    entityId,
                    limit,
                    minSimilarity,
                    includeProfiles);

                _logger.LogInformation("Found {MatchCount} similar profiles for {entityId}",
                    result.TotalMatches, entityId);

                // Return results
                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
                return CreateBadRequestResponse(req, ex.Message);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "Invalid query parameter format: {Message}", ex.Message);
                return CreateBadRequestResponse(req, "Invalid query parameter format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar profiles for {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion
    }
}
