using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using EntityMatching.Functions.Common;
using System;
using System.Net;
using System.Reflection;

namespace EntityMatching.Functions
{
    /// <summary>
    /// Public version and health check endpoint
    /// No authentication required - used for deployment verification
    /// </summary>
    public class VersionFunctions : BaseApiFunction
    {
        public VersionFunctions(ILogger<VersionFunctions> logger) : base(logger)
        {
        }

        /// <summary>
        /// GET /api/version - Public endpoint for version info and health check
        /// </summary>
        [Function("GetVersion")]
        public HttpResponseData GetVersion(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "version")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Version endpoint called");

                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "unknown";
                var buildDate = GetBuildDate(assembly);

                var versionInfo = new
                {
                    service = "EntityMatching API",
                    version = version,
                    buildDate = buildDate.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    status = "healthy",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                SetCorsHeaders(response);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString(System.Text.Json.JsonSerializer.Serialize(versionInfo));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVersion");
                return CreateErrorResponse(req, "Failed to retrieve version information");
            }
        }

        /// <summary>
        /// OPTIONS handler for CORS preflight
        /// </summary>
        [Function("GetVersionOptions")]
        public HttpResponseData GetVersionOptions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "version")] HttpRequestData req)
        {
            _logger.LogInformation("OPTIONS preflight request received for /version");
            return CreateNoContentResponse(req);
        }

        private static DateTime GetBuildDate(Assembly assembly)
        {
            // Use the last write time of the assembly file as build date
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location))
                return DateTime.UtcNow;

            try
            {
                return System.IO.File.GetLastWriteTimeUtc(location);
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }
    }
}
