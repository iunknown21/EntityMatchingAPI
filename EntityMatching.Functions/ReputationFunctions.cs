using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Reputation;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Azure Functions for reputation and ratings management
    /// Endpoints: /api/v1/ratings/*, /api/v1/entities/{id}/reputation
    /// </summary>
    public class ReputationFunctions : BaseApiFunction
    {
        private readonly IReputationService _reputationService;

        public ReputationFunctions(
            IReputationService reputationService,
            ILogger<ReputationFunctions> logger) : base(logger)
        {
            _reputationService = reputationService;
        }

        #region Ratings CRUD

        // OPTIONS /api/v1/ratings
        [Function("CreateRatingOptions")]
        public HttpResponseData CreateRatingOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/ratings")] HttpRequestData req)
        {
            _logger.LogInformation("OPTIONS preflight for POST /v1/ratings");
            return CreateNoContentResponse(req);
        }

        // POST /api/v1/ratings
        [Function("CreateRating")]
        public async Task<HttpResponseData> CreateRating(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/ratings")] HttpRequestData req)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var rating = JsonHelper.DeserializeApi<EntityRating>(requestBody);

                if (rating == null)
                {
                    return CreateBadRequestResponse(req, "Invalid rating data");
                }

                if (string.IsNullOrEmpty(rating.EntityId) || string.IsNullOrEmpty(rating.RatedByEntityId))
                {
                    return CreateBadRequestResponse(req, "ProfileId and RatedByProfileId are required");
                }

                _logger.LogInformation("Creating rating for profile {entityId} by {RatedBy}",
                    rating.EntityId, rating.RatedByEntityId);

                var saved = await _reputationService.AddOrUpdateRatingAsync(rating);

                var response = req.CreateResponse(HttpStatusCode.Created);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(saved);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rating");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // OPTIONS /api/v1/ratings/{id}
        [Function("GetRatingOptions")]
        public HttpResponseData GetRatingOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/ratings/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("OPTIONS preflight for /v1/ratings/{Id}", id);
            return CreateNoContentResponse(req);
        }

        // GET /api/v1/ratings/{id}
        [Function("GetRating")]
        public async Task<HttpResponseData> GetRating(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/ratings/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Getting rating {RatingId}", id);

                var rating = await _reputationService.GetRatingAsync(id);
                if (rating == null)
                {
                    return CreateNotFoundResponse(req, $"Rating {id} not found");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(rating);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rating {RatingId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // DELETE /api/v1/ratings/{id}
        [Function("DeleteRating")]
        public async Task<HttpResponseData> DeleteRating(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/ratings/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Deleting rating {RatingId}", id);

                await _reputationService.DeleteRatingAsync(id);

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                SetCorsHeaders(response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rating {RatingId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Profile Ratings and Reputation

        // OPTIONS /api/v1/entities/{entityId}/ratings
        [Function("GetEntityRatingsOptions")]
        public HttpResponseData GetEntityRatingsOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{entityId}/ratings")] HttpRequestData req,
            string entityId)
        {
            _logger.LogInformation("OPTIONS preflight for /v1/entities/{entityId}/ratings", entityId);
            return CreateNoContentResponse(req);
        }

        // GET /api/v1/entities/{entityId}/ratings?includePrivate=false
        [Function("GetEntityRatings")]
        public async Task<HttpResponseData> GetEntityRatings(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/entities/{entityId}/ratings")] HttpRequestData req,
            string entityId)
        {
            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var includePrivateStr = query["includePrivate"];
                var includePrivate = !string.IsNullOrEmpty(includePrivateStr) && bool.Parse(includePrivateStr);

                _logger.LogInformation("Getting ratings for profile {entityId} (includePrivate={IncludePrivate})",
                    entityId, includePrivate);

                var ratings = await _reputationService.GetRatingsForEntityAsync(entityId, includePrivate);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(ratings);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ratings for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // OPTIONS /api/v1/entities/{entityId}/reputation
        [Function("GetEntityReputationOptions")]
        public HttpResponseData GetEntityReputationOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{entityId}/reputation")] HttpRequestData req,
            string entityId)
        {
            _logger.LogInformation("OPTIONS preflight for /v1/entities/{entityId}/reputation", entityId);
            return CreateNoContentResponse(req);
        }

        // GET /api/v1/entities/{entityId}/reputation?forceRecalculate=false
        [Function("GetEntityReputation")]
        public async Task<HttpResponseData> GetEntityReputation(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/entities/{entityId}/reputation")] HttpRequestData req,
            string entityId)
        {
            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var forceRecalcStr = query["forceRecalculate"];
                var forceRecalc = !string.IsNullOrEmpty(forceRecalcStr) && bool.Parse(forceRecalcStr);

                _logger.LogInformation("Getting reputation for profile {entityId} (forceRecalculate={ForceRecalc})",
                    entityId, forceRecalc);

                var reputation = await _reputationService.GetReputationAsync(entityId, forceRecalc);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(reputation);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reputation for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // POST /api/v1/entities/{entityId}/reputation/recalculate
        [Function("RecalculateReputation")]
        public async Task<HttpResponseData> RecalculateReputation(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/entities/{entityId}/reputation/recalculate")] HttpRequestData req,
            string entityId)
        {
            try
            {
                _logger.LogInformation("Force recalculating reputation for profile {entityId}", entityId);

                var reputation = await _reputationService.RecalculateReputationAsync(entityId);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(reputation);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating reputation for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion
    }
}
