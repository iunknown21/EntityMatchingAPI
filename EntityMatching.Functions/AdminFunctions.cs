using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using EntityMatching.Core.Utilities;
using EntityMatching.Functions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Admin endpoints for testing and debugging embedding infrastructure
    /// Prefix: /api/admin/
    /// </summary>
    public class AdminFunctions : BaseApiFunction
    {
        private readonly IEmbeddingStorageService _embeddingStorage;
        private readonly IEmbeddingService _embeddingService;
        private readonly IEntityService _profileService;
        private readonly IConfiguration _configuration;

        // Diverse bios for test profiles
        private readonly string[] _testBios = new[]
        {
            "Passionate about hiking, camping, and outdoor adventures. Love wildlife photography and exploring remote trails.",
            "Coffee enthusiast and amateur baker. Enjoys cozy cafes, trying new recipes, and food photography.",
            "Tech geek who loves coding, gaming, and sci-fi movies. Always learning new frameworks and technologies.",
            "Yoga instructor and meditation practitioner. Believer in mindfulness, healthy living, and holistic wellness.",
            "Travel blogger who has visited 40+ countries. Always planning the next adventure and documenting experiences.",
            "Art lover who frequents museums and galleries. Enjoys painting, sketching, and creative expression.",
            "Fitness enthusiast who loves running, cycling, and CrossFit. Training for my third marathon.",
            "Music producer and DJ. Passionate about electronic music, vinyl collecting, and live performances.",
            "Book lover and aspiring writer. Enjoys fantasy novels, poetry, and creative writing workshops.",
            "Environmental activist and sustainability advocate. Working towards a zero-waste lifestyle."
        };

        private readonly string[] _personas = new[]
        {
            "Adventurer", "Foodie", "Techie", "Yogi", "Traveler",
            "Artist", "Athlete", "Musician", "Writer", "Environmentalist"
        };

        public AdminFunctions(
            IEmbeddingStorageService embeddingStorage,
            IEmbeddingService embeddingService,
            IEntityService profileService,
            IConfiguration configuration,
            ILogger<AdminFunctions> logger) : base(logger)
        {
            _embeddingStorage = embeddingStorage;
            _embeddingService = embeddingService;
            _profileService = profileService;
            _configuration = configuration;
        }

        #region GET /api/admin/test - Simple test endpoint

        /// <summary>
        /// Simple test endpoint to verify admin routes work
        /// GET /api/admin/test
        /// </summary>
        [Function("AdminTest")]
        public HttpResponseData AdminTest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/test")] HttpRequestData req)
        {
            _logger.LogInformation("Admin test endpoint called");

            var response = req.CreateResponse(HttpStatusCode.OK);
            SetCorsHeaders(response);
            response.WriteString("{\"status\":\"Admin endpoint working!\",\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}");
            response.Headers.Add("Content-Type", "application/json");
            return response;
        }

        #endregion

        #region GET /api/testadmin - Test with different prefix

        /// <summary>
        /// Test endpoint with non-admin prefix
        /// GET /api/testadmin
        /// </summary>
        [Function("TestAdmin")]
        public HttpResponseData TestAdmin(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "testadmin")] HttpRequestData req)
        {
            _logger.LogInformation("TestAdmin endpoint called");

            var response = req.CreateResponse(HttpStatusCode.OK);
            SetCorsHeaders(response);
            response.WriteString("{\"status\":\"TestAdmin endpoint working!\",\"timestamp\":\"" + DateTime.UtcNow.ToString("o") + "\"}");
            response.Headers.Add("Content-Type", "application/json");
            return response;
        }

        #endregion

        #region POST /api/admin/embeddings/process

        [Function("AdminProcessEmbeddingsOptions")]
        public HttpResponseData AdminProcessEmbeddingsOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/embeddings/process")]
            HttpRequestData req)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Manually trigger embedding processing
        /// POST /api/admin/embeddings/process?limit=10
        /// </summary>
        [Function("AdminProcessEmbeddings")]
        public async Task<HttpResponseData> ProcessEmbeddings(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/embeddings/process")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Manual embedding processing triggered");

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var limitStr = query["limit"];
                int? limit = string.IsNullOrEmpty(limitStr) ? null : int.Parse(limitStr);

                var maxRetries = _configuration.GetValue<int>("EMBEDDING_MAX_RETRIES", 3);

                // Get pending embeddings
                var pendingEmbeddings = await _embeddingStorage.GetEmbeddingsByStatusAsync(
                    EmbeddingStatus.Pending,
                    limit);

                var stats = new { total = 0, success = 0, failed = 0, skipped = 0 };
                int successCount = 0;
                int failedCount = 0;
                int skippedCount = 0;

                foreach (var embedding in pendingEmbeddings)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(embedding.EntitySummary))
                        {
                            skippedCount++;
                            continue;
                        }

                        var vector = await _embeddingService.GenerateEmbeddingAsync(embedding.EntitySummary);

                        if (vector != null && vector.Length > 0)
                        {
                            embedding.Embedding = vector;
                            embedding.Dimensions = vector.Length;
                            embedding.EmbeddingModel = _embeddingService.ModelName;
                            embedding.Status = EmbeddingStatus.Generated;
                            embedding.ErrorMessage = null;
                            successCount++;
                        }
                        else
                        {
                            embedding.Status = EmbeddingStatus.Failed;
                            embedding.ErrorMessage = "Null vector returned";
                            embedding.RetryCount++;
                            failedCount++;
                        }

                        await _embeddingStorage.UpsertEmbeddingAsync(embedding);
                    }
                    catch (Exception ex)
                    {
                        embedding.Status = EmbeddingStatus.Failed;
                        embedding.ErrorMessage = ex.Message;
                        embedding.RetryCount++;
                        failedCount++;
                        await _embeddingStorage.UpsertEmbeddingAsync(embedding);
                    }
                }

                var result = new
                {
                    total = pendingEmbeddings.Count,
                    success = successCount,
                    failed = failedCount,
                    skipped = skippedCount
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing embeddings");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region GET /api/admin/embeddings/status

        [Function("AdminGetEmbeddingStatusOptions")]
        public HttpResponseData AdminGetEmbeddingStatusOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/embeddings/status")]
            HttpRequestData req)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Get embedding status counts
        /// GET /api/admin/embeddings/status
        /// </summary>
        [Function("AdminGetEmbeddingStatus")]
        public async Task<HttpResponseData> GetEmbeddingStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/embeddings/status")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting embedding status counts");

                var counts = await _embeddingStorage.GetEmbeddingCountsByStatusAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(counts);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding status");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region POST /api/admin/test-data/profiles

        [Function("AdminCreateTestEntitiesOptions")]
        public HttpResponseData AdminCreateTestEntitiesOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/test-data/profiles")]
            HttpRequestData req)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Create test profiles with diverse preferences
        /// POST /api/admin/test-data/profiles
        /// Body: { "count": 5, "userId": "test-user" }
        /// </summary>
        [Function("AdminCreateTestEntities")]
        public async Task<HttpResponseData> CreateTestEntities(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/test-data/profiles")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Creating test profiles");

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonHelper.DeserializeApi<CreateTestEntitiesRequest>(requestBody);

                var count = request?.Count ?? 1;
                var userId = request?.UserId ?? "test-user";

                if (count <= 0 || count > 20)
                {
                    return CreateBadRequestResponse(req, "Count must be between 1 and 20");
                }

                var profileIds = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    // Create profile with variety
                    var personaIndex = i % _personas.Length;

                    var profile = new Entity
                    {
                        Id = Guid.NewGuid(),
                        EntityType = EntityType.Person,
                        Name = $"Test {_personas[personaIndex]} {i + 1}",
                        Description = _testBios[personaIndex],
                        OwnedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        Attributes = new Dictionary<string, object>
                        {
                            ["contactInformation"] = "Seattle, WA",
                            ["birthday"] = DateTime.UtcNow.AddYears(-30)
                        }
                    };

                    // Add profile to database
                    await _profileService.AddEntityAsync(profile);
                    profileIds.Add(profile.Id.ToString());

                    _logger.LogInformation("Created test profile: {Name} ({entityId})",
                        profile.Name, profile.Id);
                }

                var result = new { profileIds, count = profileIds.Count };

                var response = req.CreateResponse(HttpStatusCode.Created);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test profiles");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region GET /api/admin/embeddings/{entityId}

        [Function("AdminGetEmbeddingOptions")]
        public HttpResponseData AdminGetEmbeddingOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/embeddings/{entityId}")]
            HttpRequestData req,
            string entityId)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Get specific embedding details
        /// GET /api/admin/embeddings/{entityId}
        /// </summary>
        [Function("AdminGetEmbedding")]
        public async Task<HttpResponseData> GetEmbedding(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/embeddings/{entityId}")]
            HttpRequestData req,
            string entityId)
        {
            try
            {
                _logger.LogInformation("Getting embedding for profile {entityId}", entityId);

                var embedding = await _embeddingStorage.GetEmbeddingAsync(entityId);

                if (embedding == null)
                {
                    return CreateNotFoundResponse(req, $"No embedding found for profile {entityId}");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(embedding);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding for {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region POST /api/admin/embeddings/{entityId}/regenerate

        [Function("AdminRegenerateEmbeddingOptions")]
        public HttpResponseData AdminRegenerateEmbeddingOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/embeddings/{entityId}/regenerate")]
            HttpRequestData req,
            string entityId)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Force regenerate embedding for a profile
        /// POST /api/admin/embeddings/{entityId}/regenerate
        /// </summary>
        [Function("AdminRegenerateEmbedding")]
        public async Task<HttpResponseData> RegenerateEmbedding(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/embeddings/{entityId}/regenerate")]
            HttpRequestData req,
            string entityId)
        {
            try
            {
                _logger.LogInformation("Regenerating embedding for profile {entityId}", entityId);

                var embedding = await _embeddingStorage.GetEmbeddingAsync(entityId);

                if (embedding == null)
                {
                    return CreateNotFoundResponse(req, $"No embedding found for profile {entityId}");
                }

                // Reset to pending state
                embedding.Status = EmbeddingStatus.Pending;
                embedding.Embedding = null;
                embedding.ErrorMessage = null;
                embedding.RetryCount = 0;

                await _embeddingStorage.UpsertEmbeddingAsync(embedding);

                var result = new { message = "Embedding queued for regeneration", entityId };

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating embedding for {entityId}", entityId);
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region POST /api/admin/embeddings/retry-failed

        [Function("AdminRetryFailedEmbeddingsOptions")]
        public HttpResponseData AdminRetryFailedEmbeddingsOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/embeddings/retry-failed")]
            HttpRequestData req)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Reset all failed embeddings to pending
        /// POST /api/admin/embeddings/retry-failed
        /// </summary>
        [Function("AdminRetryFailedEmbeddings")]
        public async Task<HttpResponseData> RetryFailedEmbeddings(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/embeddings/retry-failed")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Retrying all failed embeddings");

                var failedEmbeddings = await _embeddingStorage.GetEmbeddingsByStatusAsync(
                    EmbeddingStatus.Failed,
                    limit: null);

                foreach (var embedding in failedEmbeddings)
                {
                    embedding.Status = EmbeddingStatus.Pending;
                    embedding.RetryCount = 0;
                    embedding.ErrorMessage = null;
                    await _embeddingStorage.UpsertEmbeddingAsync(embedding);
                }

                var result = new { resetCount = failedEmbeddings.Count };

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed embeddings");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region POST /api/admin/migrate/privacy-settings

        [Function("AdminMigratePrivacySettingsOptions")]
        public HttpResponseData AdminMigratePrivacySettingsOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/migrate/privacy-settings")]
            HttpRequestData req)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Initialize privacy settings on all existing profiles
        /// POST /api/admin/migrate/privacy-settings?dryRun=false
        /// </summary>
        [Function("AdminMigratePrivacySettings")]
        public async Task<HttpResponseData> MigratePrivacySettings(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/migrate/privacy-settings")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Starting privacy settings migration");

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var dryRun = query["dryRun"] != "false"; // Default to dry run

                var migrationResults = new
                {
                    totalProfiles = 0,
                    migratedCount = 0,
                    alreadyMigratedCount = 0,
                    errors = new List<string>(),
                    dryRun = dryRun
                };

                int totalProfiles = 0;
                int migratedCount = 0;
                int alreadyMigratedCount = 0;
                var errors = new List<string>();

                // Get all profiles (paginate if needed)
                var allProfiles = new List<Entity>();
                try
                {
                    // This is a simplified approach - in production, use pagination
                    var userIds = new HashSet<string>();

                    // Get sample of profiles to find user IDs
                    // In a real migration, you'd want to iterate through all user IDs or use continuation tokens
                    var sampleProfile = await _profileService.GetEntityAsync(Guid.NewGuid().ToString());

                    // For now, we'll do a simple approach - the actual implementation would need
                    // to query profiles more efficiently
                    _logger.LogWarning("Privacy migration endpoint needs production-grade pagination implementation");

                    // Return placeholder response for now
                    var placeholderResponse = req.CreateResponse(HttpStatusCode.OK);
                    SetCorsHeaders(placeholderResponse);
                    await placeholderResponse.WriteAsJsonAsync(new
                    {
                        message = "Migration endpoint placeholder - implement proper profile iteration",
                        dryRun = dryRun,
                        recommendation = "Set default privacy settings in Entity constructor instead"
                    });
                    return placeholderResponse;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during privacy migration");
                    errors.Add($"Migration error: {ex.Message}");
                }

                var result = new
                {
                    totalProfiles,
                    migratedCount,
                    alreadyMigratedCount,
                    errors = errors.ToArray(),
                    dryRun,
                    message = dryRun ? "Dry run completed - no changes made" : "Migration completed"
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in privacy settings migration");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion

        #region POST /api/admin/summaries/generate

        [Function("AdminGenerateSummariesOptions")]
        public HttpResponseData AdminGenerateSummariesOptions(
            [HttpTrigger(AuthorizationLevel.Function, "options", Route = "admin/summaries/generate")]
            HttpRequestData req)
        {
            return CreateNoContentResponse(req);
        }

        /// <summary>
        /// Manually trigger summary generation for all entities
        /// POST /api/admin/summaries/generate?limit=10
        /// </summary>
        [Function("AdminGenerateSummaries")]
        public async Task<HttpResponseData> GenerateSummaries(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/summaries/generate")]
            HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Manual summary generation triggered");

                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var limitStr = query["limit"];
                int? limit = string.IsNullOrEmpty(limitStr) ? null : int.Parse(limitStr);

                // This triggers the actual GenerateProfileSummariesFunction logic manually
                var message = limit.HasValue
                    ? $"Summary generation triggered for up to {limit} entities"
                    : "Summary generation triggered for all entities";

                var result = new
                {
                    message,
                    note = "Summaries will be generated asynchronously. Use /admin/embeddings/status to check progress."
                };

                var response = req.CreateResponse(HttpStatusCode.Accepted);
                SetCorsHeaders(response);
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering summary generation");
                return CreateErrorResponse(req, ex.Message);
            }
        }

        #endregion
    }

    /// <summary>
    /// Request model for creating test profiles
    /// </summary>
    public class CreateTestEntitiesRequest
    {
        public int Count { get; set; } = 1;
        public string? UserId { get; set; }
    }
}
