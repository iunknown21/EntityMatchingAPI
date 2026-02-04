using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Shared.Models;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Azure Functions for conversational profiling
    /// Endpoints for managing profile-building conversations with AI
    /// </summary>
    public class ConversationFunctions : BaseApiFunction
    {
        private readonly IConversationService _conversationService;
        private readonly IEntityService _profileService;

        public ConversationFunctions(
            IConversationService conversationService,
            IEntityService profileService,
            ILogger<ConversationFunctions> logger) : base(logger)
        {
            _conversationService = conversationService;
            _profileService = profileService;
        }

        #region Send Message

        // OPTIONS handler for POST /api/v1/entities/{entityId}/conversation
        [Function("SendConversationMessageOptions")]
        public HttpResponseData SendConversationMessageOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{entityId}/conversation")] HttpRequestData req,
            string entityId)
        {
            _logger.LogInformation("OPTIONS preflight request received for POST /v1/entities/{entityId}/conversation", entityId);
            return CreateNoContentResponse(req);
        }

        // POST /api/v1/entities/{entityId}/conversation
        [Function("SendConversationMessage")]
        public async Task<HttpResponseData> SendConversationMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/entities/{entityId}/conversation")] HttpRequestData req,
            string entityId)
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

                var request = JsonHelper.DeserializeApi<ConversationMessageRequest>(requestBody);

                if (request == null || string.IsNullOrEmpty(request.Message))
                {
                    return CreateBadRequestResponse(req, "Message is required");
                }

                // Verify profile exists
                var profile = await _profileService.GetEntityAsync(entityId);
                if (profile == null)
                {
                    return CreateNotFoundResponse(req, $"Profile {entityId} not found");
                }

                // Optional: Verify ownership if userId provided
                if (!string.IsNullOrEmpty(request.UserId) && profile.OwnedByUserId != request.UserId)
                {
                    return CreateNotFoundResponse(req, "Access denied");
                }

                _logger.LogInformation("Processing conversation message for profile {entityId}", entityId);

                // Send message to conversation service
                var result = await _conversationService.ProcessUserMessageAsync(entityId, request.UserId ?? "", request.Message, request.SystemPrompt);

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing conversation message for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Get Conversation History

        // OPTIONS handler for GET /api/v1/entities/{entityId}/conversation
        [Function("GetConversationHistoryOptions")]
        public HttpResponseData GetConversationHistoryOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{entityId}/conversation")] HttpRequestData req,
            string entityId)
        {
            _logger.LogInformation("OPTIONS preflight request received for GET /v1/entities/{entityId}/conversation", entityId);
            return CreateNoContentResponse(req);
        }

        // GET /api/v1/entities/{entityId}/conversation
        [Function("GetConversationHistory")]
        public async Task<HttpResponseData> GetConversationHistory(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/entities/{entityId}/conversation")] HttpRequestData req,
            string entityId)
        {
            try
            {
                // Verify profile exists
                var profile = await _profileService.GetEntityAsync(entityId);
                if (profile == null)
                {
                    return CreateNotFoundResponse(req, $"Profile {entityId} not found");
                }

                // Optional: Verify ownership if userId provided
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];

                if (!string.IsNullOrEmpty(userId) && profile.OwnedByUserId != userId)
                {
                    return CreateNotFoundResponse(req, "Access denied");
                }

                _logger.LogInformation("Getting conversation history for profile {entityId}", entityId);

                var conversation = await _conversationService.GetConversationHistoryAsync(entityId);

                if (conversation == null)
                {
                    // No conversation exists yet - return empty context
                    conversation = new ConversationContext
                    {
                        Id = Guid.NewGuid().ToString(),
                        EntityId = entityId
                    };
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(conversation);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Delete Conversation

        // OPTIONS handler for DELETE /api/v1/entities/{entityId}/conversation
        [Function("DeleteConversationOptions")]
        public HttpResponseData DeleteConversationOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "v1/entities/{entityId}/conversation")] HttpRequestData req,
            string entityId)
        {
            _logger.LogInformation("OPTIONS preflight request received for DELETE /v1/entities/{entityId}/conversation", entityId);
            return CreateNoContentResponse(req);
        }

        // DELETE /api/v1/entities/{entityId}/conversation
        [Function("DeleteConversation")]
        public async Task<HttpResponseData> DeleteConversation(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/entities/{entityId}/conversation")] HttpRequestData req,
            string entityId)
        {
            try
            {
                // Verify profile exists
                var profile = await _profileService.GetEntityAsync(entityId);
                if (profile == null)
                {
                    return CreateNotFoundResponse(req, $"Profile {entityId} not found");
                }

                // Optional: Verify ownership if userId provided
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];

                if (!string.IsNullOrEmpty(userId) && profile.OwnedByUserId != userId)
                {
                    return CreateNotFoundResponse(req, "Access denied");
                }

                _logger.LogInformation("Deleting conversation for profile {entityId}", entityId);

                await _conversationService.ClearConversationHistoryAsync(entityId);

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                SetCorsHeaders(response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        /// <summary>
        /// Request model for sending a conversation message
        /// </summary>
        public class ConversationMessageRequest
        {
            public string Message { get; set; } = "";
            public string? UserId { get; set; }
            public string? SystemPrompt { get; set; }
        }
    }
}
