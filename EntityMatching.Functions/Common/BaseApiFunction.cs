using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace EntityMatching.Functions.Common
{
    /// <summary>
    /// Base class for all API functions with CORS handling
    /// CRITICAL: All responses must call SetCorsHeaders() to ensure CORS works
    /// </summary>
    public abstract class BaseApiFunction
    {
        protected readonly ILogger _logger;

        protected BaseApiFunction(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Set CORS headers on the response
        /// MUST be called on EVERY response (success AND error)
        /// </summary>
        protected void SetCorsHeaders(HttpResponseData response)
        {
            response.Headers.Add("Access-Control-Allow-Origin",
                "http://localhost:5001,https://localhost:5001,https://datenightplanner.com,https://api.bystorm.com");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "*");
            response.Headers.Add("Access-Control-Allow-Credentials", "true");
        }

        /// <summary>
        /// Create a 204 No Content response for OPTIONS preflight requests
        /// </summary>
        protected HttpResponseData CreateNoContentResponse(HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            SetCorsHeaders(response);
            return response;
        }

        /// <summary>
        /// Create a 400 Bad Request response
        /// </summary>
        protected HttpResponseData CreateBadRequestResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            SetCorsHeaders(response);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString($"{{\"error\":\"{message}\"}}");
            return response;
        }

        /// <summary>
        /// Create a 404 Not Found response
        /// </summary>
        protected HttpResponseData CreateNotFoundResponse(HttpRequestData req, string message = "Resource not found")
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            SetCorsHeaders(response);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString($"{{\"error\":\"{message}\"}}");
            return response;
        }

        /// <summary>
        /// Create a 500 Internal Server Error response
        /// </summary>
        protected HttpResponseData CreateErrorResponse(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            SetCorsHeaders(response);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString($"{{\"error\":\"{message}\"}}");
            return response;
        }
    }
}
