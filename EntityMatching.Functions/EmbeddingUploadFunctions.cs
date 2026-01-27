using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Azure Functions for privacy-first embedding upload
    /// Allows clients to upload pre-computed embeddings without sending documents or PII
    /// </summary>
    public class EmbeddingUploadFunctions : BaseApiFunction
    {
        private readonly IEmbeddingStorageService _embeddingStorage;
        private readonly IEntityService _profileService;

        public EmbeddingUploadFunctions(
            IEmbeddingStorageService embeddingStorage,
            IEntityService profileService,
            ILogger<EmbeddingUploadFunctions> logger) : base(logger)
        {
            _embeddingStorage = embeddingStorage;
            _profileService = profileService;
        }

        #region Upload Embedding

        /// <summary>
        /// OPTIONS handler for CORS preflight requests
        /// </summary>
        [Function("UploadEmbeddingOptions")]
        public HttpResponseData UploadEmbeddingOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options",
                Route = "v1/entities/{entityId}/embeddings/upload")]
            HttpRequestData req,
            string entityId)
        {
            _logger.LogInformation("OPTIONS preflight for upload embedding {entityId}", entityId);
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// POST /api/v1/profiles/{entityId}/embeddings/upload
        /// Upload a pre-computed embedding vector for a profile
        /// Privacy-first: Client generates embedding locally, uploads only the vector
        /// </summary>
        [Function("UploadEmbedding")]
        public async Task<HttpResponseData> UploadEmbedding(
            [HttpTrigger(AuthorizationLevel.Function, "post",
                Route = "v1/entities/{entityId}/embeddings/upload")]
            HttpRequestData req,
            string entityId)
        {
            try
            {
                _logger.LogInformation("Upload embedding request for profile {entityId}", entityId);

                // 1. Parse request body
                string requestBody;
                using (var reader = new StreamReader(req.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return CreateBadRequestResponse(req, "Request body is required");
                }

                var uploadRequest = JsonHelper.DeserializeApi<UploadEmbeddingRequest>(requestBody);
                if (uploadRequest == null)
                {
                    return CreateBadRequestResponse(req, "Invalid request format");
                }

                // 2. Validate embedding
                var validationError = ValidateEmbedding(uploadRequest);
                if (validationError != null)
                {
                    return CreateBadRequestResponse(req, validationError);
                }

                // 3. Verify profile exists and check ownership
                var profile = await _profileService.GetEntityAsync(entityId);
                if (profile == null)
                {
                    return CreateNotFoundResponse(req, $"Profile {entityId} not found");
                }

                // Optional ownership validation
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var userId = query["userId"];
                if (!string.IsNullOrEmpty(userId) && profile.OwnedByUserId != userId)
                {
                    return CreateNotFoundResponse(req, "Access denied");
                }

                // 4. Create EntityEmbedding document
                var embedding = CreateEmbeddingFromUpload(entityId, uploadRequest, profile.LastModified);

                // 5. Upsert to Cosmos DB
                var saved = await _embeddingStorage.UpsertEmbeddingAsync(embedding);

                _logger.LogInformation(
                    "Successfully uploaded embedding for profile {entityId} ({Dimensions} dimensions)",
                    entityId, saved.Dimensions);

                // 6. Return success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(new
                {
                    ProfileId = saved.EntityId,
                    Status = saved.Status.ToString(),
                    Dimensions = saved.Dimensions,
                    EmbeddingModel = saved.EmbeddingModel,
                    GeneratedAt = saved.GeneratedAt,
                    Message = "Embedding uploaded successfully"
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading embedding for profile {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region Validation & Helper Methods

        /// <summary>
        /// Validate embedding request
        /// </summary>
        /// <returns>Error message if invalid, null if valid</returns>
        private string? ValidateEmbedding(UploadEmbeddingRequest request)
        {
            // Check embedding exists
            if (request.Embedding == null || request.Embedding.Length == 0)
            {
                return "Embedding vector is required and cannot be empty";
            }

            // Validate dimensions (only support 1536 for v1)
            if (request.Embedding.Length != 1536)
            {
                return $"Invalid embedding dimensions. Expected 1536, got {request.Embedding.Length}";
            }

            // Validate float values
            for (int i = 0; i < request.Embedding.Length; i++)
            {
                if (float.IsNaN(request.Embedding[i]) || float.IsInfinity(request.Embedding[i]))
                {
                    return $"Invalid embedding value at index {i}: {request.Embedding[i]}";
                }
            }

            // Validate model name if provided
            if (!string.IsNullOrEmpty(request.EmbeddingModel))
            {
                var supportedModels = new[] { "text-embedding-3-small", "text-embedding-3-large" };
                if (Array.IndexOf(supportedModels, request.EmbeddingModel) == -1)
                {
                    return $"Unsupported embedding model: {request.EmbeddingModel}. " +
                           $"Supported models: {string.Join(", ", supportedModels)}";
                }
            }

            return null; // Valid
        }

        /// <summary>
        /// Create EntityEmbedding from upload request
        /// Privacy-first: Uses placeholder summary instead of actual text
        /// </summary>
        private EntityEmbedding CreateEmbeddingFromUpload(
            string entityId,
            UploadEmbeddingRequest request,
            DateTime profileLastModified)
        {
            var now = DateTime.UtcNow;
            var placeholderSummary = "[CLIENT_UPLOADED]";

            return new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(entityId),
                EntityId = entityId,

                // Privacy-first: No actual text summary
                EntitySummary = placeholderSummary,
                SummaryHash = EntityEmbedding.ComputeHash(placeholderSummary),

                // Embedding data
                Embedding = request.Embedding,
                EmbeddingModel = request.EmbeddingModel ?? "text-embedding-3-small",
                Dimensions = request.Embedding.Length,

                // Status - immediately Generated (no pending state)
                Status = EmbeddingStatus.Generated,
                GeneratedAt = request.Metadata?.GeneratedAt ?? now,
                EntityLastModified = profileLastModified,

                // Error tracking (none for uploads)
                RetryCount = 0,
                ErrorMessage = null,

                // Metadata to track source
                SummaryMetadata = new SummaryMetadata
                {
                    HasConversationData = false,
                    ConversationChunksCount = 0,
                    ExtractedInsightsCount = 0,
                    PreferenceCategories = new System.Collections.Generic.List<string>(),
                    HasPersonalityData = false,
                    SummaryWordCount = 0
                }
            };
        }

        #endregion
    }
}
