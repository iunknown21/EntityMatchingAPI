using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Matching;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Azure Functions for match request management
    /// Endpoints: /api/v1/matches/*, /api/v1/profiles/{id}/matches/*
    /// </summary>
    public class MatchFunctions : BaseApiFunction
    {
        private readonly IMatchService _matchService;

        public MatchFunctions(
            IMatchService matchService,
            ILogger<MatchFunctions> logger) : base(logger)
        {
            _matchService = matchService;
        }

        #region Match Requests CRUD

        // POST /api/v1/matches
        [Function("CreateMatchRequest")]
        public async Task<HttpResponseData> CreateMatchRequest(
            [HttpTrigger(AuthorizationLevel.Function, "post", "options", Route = "v1/matches")] HttpRequestData req)
        {
            if (req.Method == "OPTIONS")
            {
                return CreateNoContentResponse(req);
            }

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var matchRequest = JsonHelper.DeserializeApi<MatchRequest>(requestBody);

                if (matchRequest == null)
                {
                    return CreateBadRequestResponse(req, "Invalid match request data");
                }

                if (string.IsNullOrEmpty(matchRequest.TargetId) || string.IsNullOrEmpty(matchRequest.RequesterId))
                {
                    return CreateBadRequestResponse(req, "TargetId and RequesterId are required");
                }

                _logger.LogInformation("Creating match request from {RequesterId} to {TargetId}",
                    matchRequest.RequesterId, matchRequest.TargetId);

                var saved = await _matchService.CreateMatchRequestAsync(matchRequest);

                var response = req.CreateResponse(HttpStatusCode.Created);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(saved);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating match request");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // GET /api/v1/matches/{id}
        [Function("GetMatchRequest")]
        public async Task<HttpResponseData> GetMatchRequest(
            [HttpTrigger(AuthorizationLevel.Function, "get", "options", Route = "v1/matches/{id}")] HttpRequestData req,
            string id)
        {
            if (req.Method == "OPTIONS")
            {
                return CreateNoContentResponse(req);
            }

            try
            {
                _logger.LogInformation("Getting match request {RequestId}", id);

                var matchRequest = await _matchService.GetMatchRequestAsync(id);
                if (matchRequest == null)
                {
                    return CreateNotFoundResponse(req, $"Match request {id} not found");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(matchRequest);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting match request {RequestId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // PATCH /api/v1/matches/{id}/status
        [Function("UpdateMatchStatus")]
        public async Task<HttpResponseData> UpdateMatchStatus(
            [HttpTrigger(AuthorizationLevel.Function, "patch", "options", Route = "v1/matches/{id}/status")] HttpRequestData req,
            string id)
        {
            if (req.Method == "OPTIONS")
            {
                return CreateNoContentResponse(req);
            }

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateRequest = JsonHelper.DeserializeApi<StatusUpdateRequest>(requestBody);

                if (updateRequest == null)
                {
                    return CreateBadRequestResponse(req, "Invalid status update request");
                }

                _logger.LogInformation("Updating match request {RequestId} status to {NewStatus}",
                    id, updateRequest.NewStatus);

                var updated = await _matchService.UpdateMatchStatusAsync(
                    id,
                    updateRequest.NewStatus,
                    updateRequest.ResponseMessage);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(updated);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid status transition for match request {RequestId}", id);
                return CreateBadRequestResponse(req, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating match request {RequestId} status", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Profile Match Lists

        // GET /api/v1/profiles/{entityId}/matches/incoming?includeResolved=false
        [Function("GetIncomingMatches")]
        public async Task<HttpResponseData> GetIncomingMatches(
            [HttpTrigger(AuthorizationLevel.Function, "get", "options", Route = "v1/entities/{entityId}/matches/incoming")] HttpRequestData req,
            string entityId)
        {
            if (req.Method == "OPTIONS")
            {
                return CreateNoContentResponse(req);
            }

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var includeResolvedStr = query["includeResolved"];
                var includeResolved = !string.IsNullOrEmpty(includeResolvedStr) && bool.Parse(includeResolvedStr);

                _logger.LogInformation("Getting incoming match requests for profile {entityId} (includeResolved={IncludeResolved})",
                    entityId, includeResolved);

                var matches = await _matchService.GetIncomingMatchRequestsAsync(entityId, includeResolved);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(matches);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incoming matches for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // GET /api/v1/profiles/{entityId}/matches/outgoing?includeResolved=false
        [Function("GetOutgoingMatches")]
        public async Task<HttpResponseData> GetOutgoingMatches(
            [HttpTrigger(AuthorizationLevel.Function, "get", "options", Route = "v1/entities/{entityId}/matches/outgoing")] HttpRequestData req,
            string entityId)
        {
            if (req.Method == "OPTIONS")
            {
                return CreateNoContentResponse(req);
            }

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var includeResolvedStr = query["includeResolved"];
                var includeResolved = !string.IsNullOrEmpty(includeResolvedStr) && bool.Parse(includeResolvedStr);

                _logger.LogInformation("Getting outgoing match requests for profile {entityId} (includeResolved={IncludeResolved})",
                    entityId, includeResolved);

                var matches = await _matchService.GetOutgoingMatchRequestsAsync(entityId, includeResolved);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(matches);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outgoing matches for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion
    }

    public class StatusUpdateRequest
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "newStatus")]
        public MatchStatus NewStatus { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "responseMessage")]
        public string? ResponseMessage { get; set; }
    }
}
