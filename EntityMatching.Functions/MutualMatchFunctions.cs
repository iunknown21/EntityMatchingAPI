using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using EntityMatching.Shared.Models;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Azure Functions for mutual matching operations
    /// Enables bidirectional entity discovery where both entities match each other
    /// </summary>
    public class MutualMatchFunctions : BaseApiFunction
    {
        private readonly IMutualMatchService _mutualMatchService;

        public MutualMatchFunctions(
            IMutualMatchService mutualMatchService,
            ILogger<MutualMatchFunctions> logger) : base(logger)
        {
            _mutualMatchService = mutualMatchService;
        }

        #region POST /api/v1/entities/{id}/mutual-matches

        // OPTIONS handler for CORS preflight
        [Function("FindMutualMatchesOptions")]
        public HttpResponseData FindMutualMatchesOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{id}/mutual-matches")]
            HttpRequestData req,
            string id)
        {
            _logger.LogInformation("OPTIONS preflight request received for /v1/entities/{Id}/mutual-matches", id);
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Find mutual matches for an entity
        /// POST /api/v1/entities/{id}/mutual-matches
        ///
        /// Request body:
        /// {
        ///   "minSimilarity": 0.8,
        ///   "targetEntityType": 1,  // Optional: filter to specific entity type (0=Person, 1=Job, 2=Property, etc.)
        ///   "limit": 50
        /// }
        ///
        /// Response:
        /// {
        ///   "matches": [
        ///     {
        ///       "entityAId": "person-123",
        ///       "entityBId": "job-456",
        ///       "entityAType": 0,
        ///       "entityBType": 1,
        ///       "aToB_Score": 0.89,
        ///       "bToA_Score": 0.92,
        ///       "mutualScore": 0.905,
        ///       "matchType": "Mutual",
        ///       "detectedAt": "2025-01-10T12:00:00Z"
        ///     }
        ///   ],
        ///   "totalMutualMatches": 1,
        ///   "metadata": {
        ///     "candidatesEvaluated": 47,
        ///     "reverseLookups": 47,
        ///     "searchDurationMs": 3420,
        ///     "minSimilarity": 0.8
        ///   }
        /// }
        /// </summary>
        [Function("FindMutualMatches")]
        public async Task<HttpResponseData> FindMutualMatches(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/entities/{id}/mutual-matches")]
            HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Received mutual match request for entity {EntityId}", id);

                // Parse request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = string.IsNullOrWhiteSpace(requestBody)
                    ? new MutualMatchRequest()
                    : JsonHelper.DeserializeApi<MutualMatchRequest>(requestBody);

                // Validate
                if (string.IsNullOrWhiteSpace(id))
                {
                    return CreateBadRequestResponse(req, "Entity ID is required");
                }

                // Execute mutual matching
                var result = await _mutualMatchService.FindMutualMatchesAsync(
                    id,
                    request.MinSimilarity ?? 0.8f,
                    request.TargetEntityType,
                    request.Limit ?? 50);

                _logger.LogInformation(
                    "Mutual match search completed: found {MatchCount} mutual matches for entity {EntityId} " +
                    "(candidates evaluated: {CandidatesEvaluated}, duration: {DurationMs}ms)",
                    result.TotalMutualMatches, id,
                    result.Metadata.CandidatesEvaluated,
                    result.Metadata.SearchDurationMs);

                // Return results
                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation finding mutual matches for entity {EntityId}", id);
                return CreateBadRequestResponse(req, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding mutual matches for entity {EntityId}", id);
                return CreateErrorResponse(req, "An error occurred processing your request");
            }
        }

        #endregion
    }

    /// <summary>
    /// Request model for mutual matching
    /// </summary>
    public class MutualMatchRequest
    {
        /// <summary>
        /// Minimum similarity score threshold (0-1)
        /// Both directions must exceed this threshold for a mutual match
        /// Default: 0.8
        /// </summary>
        public float? MinSimilarity { get; set; }

        /// <summary>
        /// Optional filter for target entity type
        /// If specified, only matches entities of this type
        /// Values: 0=Person, 1=Job, 2=Property, 3=Product, 4=Service, 5=Event
        /// Default: null (matches all types)
        /// </summary>
        public EntityType? TargetEntityType { get; set; }

        /// <summary>
        /// Maximum number of mutual matches to return
        /// Default: 50
        /// </summary>
        public int? Limit { get; set; }
    }
}
