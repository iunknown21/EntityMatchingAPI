using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Shared.Models;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Azure Functions for profile/entity management (CRUD operations)
    /// All endpoints use /api/v1/entities prefix
    /// Now supports universal Entity model (Person, Job, Property, Career, Major, etc.)
    /// </summary>
    public class EntityFunctions : BaseApiFunction
    {
        private readonly IEntityService _profileService;
        private readonly IEntityService _entityService;

        public EntityFunctions(
            IEntityService profileService,
            IEntityService entityService,
            ILogger<EntityFunctions> logger) : base(logger)
        {
            _profileService = profileService;
            _entityService = entityService;
        }

        #region List Profiles

        // OPTIONS handler for /api/v1/entities
        [Function("GetEntitiesOptions")]
        public HttpResponseData GetEntitiesOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities")] HttpRequestData req)
        {
            _logger.LogInformation("OPTIONS preflight request received for /v1/entities");
            return CreateNoContentResponse(req);
        }

        // GET /api/v1/entities?userId={userId}
        [Function("GetEntities")]
        public async Task<HttpResponseData> GetEntities(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/entities")] HttpRequestData req)
        {
            try
            {
                // Extract userId from query string
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];

                if (string.IsNullOrEmpty(userId))
                {
                    return CreateBadRequestResponse(req, "userId query parameter is required");
                }

                _logger.LogInformation("Getting profiles for user {UserId}", userId);

                var profiles = await _profileService.GetAllEntitiesAsync(userId);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(profiles);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profiles");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Get PersonEntity by ID

        // OPTIONS handler for /api/v1/entities/{id}
        [Function("GetEntityByIdOptions")]
        public HttpResponseData GetEntityByIdOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("OPTIONS preflight request received for /v1/entities/{Id}", id);
            return CreateNoContentResponse(req);
        }

        // GET /api/v1/entities/{id}
        [Function("GetEntityById")]
        public async Task<HttpResponseData> GetEntityById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/entities/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                // Optional: Get userId from query string for ownership validation
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];

                _logger.LogInformation("Getting entity {EntityId}", id);

                Entity? entity;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Validate ownership
                    entity = await _entityService.GetEntityAsync(id, userId);
                }
                else
                {
                    // No ownership validation (use with caution - consider requiring userId in production)
                    entity = await _entityService.GetEntityAsync(id);
                }

                if (entity == null)
                {
                    return CreateNotFoundResponse(req, $"Entity {id} not found or access denied");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity {EntityId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Create PersonEntity

        // OPTIONS handler for /api/v1/entities (create)
        [Function("CreateEntityOptions")]
        public HttpResponseData CreateEntityOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities")] HttpRequestData req)
        {
            _logger.LogInformation("OPTIONS preflight request received for POST /v1/profiles");
            return CreateNoContentResponse(req);
        }

        // POST /api/v1/entities
        [Function("CreateEntity")]
        public async Task<HttpResponseData> CreateEntity(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/entities")] HttpRequestData req)
        {
            try
            {
                // Read request body
                string requestBody;
                using (var reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return CreateBadRequestResponse(req, "Request body is required");
                }

                // Try to deserialize as Entity (universal model)
                var entity = JsonHelper.DeserializeApi<Entity>(requestBody);

                if (entity == null)
                {
                    return CreateBadRequestResponse(req, "Invalid entity data");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(entity.OwnedByUserId))
                {
                    return CreateBadRequestResponse(req, "OwnedByUserId is required");
                }

                if (string.IsNullOrEmpty(entity.Name))
                {
                    return CreateBadRequestResponse(req, "Name is required");
                }

                _logger.LogInformation("Creating entity {EntityType} for user {UserId}",
                    entity.EntityType, entity.OwnedByUserId);

                await _entityService.AddEntityAsync(entity);

                var response = req.CreateResponse(HttpStatusCode.Created);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating entity");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Update PersonEntity

        // OPTIONS handler for /api/v1/entities/{id} (update)
        [Function("UpdateEntityOptions")]
        public HttpResponseData UpdateEntityOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("OPTIONS preflight request received for PUT /v1/profiles/{Id}", id);
            return CreateNoContentResponse(req);
        }

        // PUT /api/v1/entities/{id}
        [Function("UpdateEntity")]
        public async Task<HttpResponseData> UpdateEntity(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "v1/entities/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                // Read request body
                string requestBody;
                using (var reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return CreateBadRequestResponse(req, "Request body is required");
                }

                var profile = JsonHelper.DeserializeApi<PersonEntity>(requestBody);

                if (profile == null)
                {
                    return CreateBadRequestResponse(req, "Invalid profile data");
                }

                // Ensure ID matches route parameter
                if (profile.Id.ToString() != id)
                {
                    return CreateBadRequestResponse(req, "PersonEntity ID in body must match route parameter");
                }

                // Verify profile exists
                var existing = await _profileService.GetEntityAsync(id);
                if (existing == null)
                {
                    return CreateNotFoundResponse(req, $"PersonEntity {id} not found");
                }

                // Optional: Verify ownership if userId provided
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];

                if (!string.IsNullOrEmpty(userId) && existing.OwnedByUserId != userId)
                {
                    return CreateNotFoundResponse(req, "Access denied");
                }

                _logger.LogInformation("Updating profile {ProfileId}", id);

                await _profileService.UpdateEntityAsync(profile);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(profile);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile {ProfileId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Delete PersonEntity

        // OPTIONS handler for /api/v1/entities/{id} (delete)
        [Function("DeleteEntityOptions")]
        public HttpResponseData DeleteEntityOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("OPTIONS preflight request received for DELETE /v1/profiles/{Id}", id);
            return CreateNoContentResponse(req);
        }

        // DELETE /api/v1/entities/{id}
        [Function("DeleteEntity")]
        public async Task<HttpResponseData> DeleteEntity(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/entities/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                // Verify profile exists
                var existing = await _profileService.GetEntityAsync(id);
                if (existing == null)
                {
                    return CreateNotFoundResponse(req, $"PersonEntity {id} not found");
                }

                // Optional: Verify ownership if userId provided
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];

                if (!string.IsNullOrEmpty(userId) && existing.OwnedByUserId != userId)
                {
                    return CreateNotFoundResponse(req, "Access denied");
                }

                _logger.LogInformation("Deleting profile {ProfileId}", id);

                await _profileService.DeleteEntityAsync(id);

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                SetCorsHeaders(response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile {ProfileId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Metadata Operations

        // OPTIONS handler for /api/v1/entities/{id}/metadata
        [Function("EntityMetadataOptions")]
        public HttpResponseData EntityMetadataOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{id}/metadata")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("OPTIONS preflight request received for /v1/entities/{Id}/metadata", id);
            return CreateNoContentResponse(req);
        }

        // GET /api/v1/entities/{id}/metadata
        [Function("GetEntityMetadata")]
        public async Task<HttpResponseData> GetEntityMetadata(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/entities/{id}/metadata")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Getting metadata for profile {ProfileId}", id);

                var metadata = await _entityService.GetEntityMetadataAsync(id);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(metadata);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metadata for profile {ProfileId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        // PATCH /api/v1/entities/{id}/metadata
        [Function("UpdateEntityMetadata")]
        public async Task<HttpResponseData> UpdateEntityMetadata(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "v1/entities/{id}/metadata")] HttpRequestData req,
            string id)
        {
            try
            {
                // Read request body
                string requestBody;
                using (var reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return CreateBadRequestResponse(req, "Request body is required");
                }

                var metadata = JsonHelper.DeserializeApi<System.Collections.Generic.Dictionary<string, object>>(requestBody);

                if (metadata == null)
                {
                    return CreateBadRequestResponse(req, "Invalid metadata format");
                }

                _logger.LogInformation("Updating metadata for profile {ProfileId} with {Count} keys", id, metadata.Count);

                var updatedProfile = await _entityService.UpdateEntityMetadataAsync(id, metadata);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(updatedProfile);
                return response;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PersonEntity {ProfileId} not found", id);
                return CreateNotFoundResponse(req, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metadata for profile {ProfileId}", id);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion
    }
}
