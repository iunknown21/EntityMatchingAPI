using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// Helper class for making HTTP requests to Azure Functions API
    /// Supports both local development and deployed Azure environments
    /// </summary>
    public class ApiTestHelper : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _functionKey;
        private readonly string _baseUrl;

        public ApiTestHelper(IConfiguration configuration)
        {
            // Get base URL from config or environment variable
            // Default to local development URL
            _baseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
                ?? configuration["Api:BaseUrl"]
                ?? "http://localhost:7071";

            // Get function key if needed (for deployed environments)
            _functionKey = Environment.GetEnvironmentVariable("API_FUNCTION_KEY")
                ?? configuration["Api:FunctionKey"]
                ?? "";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Make a GET request to the API
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync(string endpoint, string? queryString = null)
        {
            var url = BuildUrl(endpoint, queryString);
            return await _httpClient.GetAsync(url);
        }

        /// <summary>
        /// Make a POST request to the API
        /// </summary>
        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T body, string? queryString = null)
        {
            var url = BuildUrl(endpoint, queryString);
            return await _httpClient.PostAsJsonAsync(url, body);
        }

        /// <summary>
        /// Make a PUT request to the API
        /// </summary>
        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T body, string? queryString = null)
        {
            var url = BuildUrl(endpoint, queryString);
            return await _httpClient.PutAsJsonAsync(url, body);
        }

        /// <summary>
        /// Make a DELETE request to the API
        /// </summary>
        public async Task<HttpResponseMessage> DeleteAsync(string endpoint, string? queryString = null)
        {
            var url = BuildUrl(endpoint, queryString);
            return await _httpClient.DeleteAsync(url);
        }

        /// <summary>
        /// Make a POST request with no body
        /// </summary>
        public async Task<HttpResponseMessage> PostAsync(string endpoint, string? queryString = null)
        {
            var url = BuildUrl(endpoint, queryString);
            return await _httpClient.PostAsync(url, null);
        }

        /// <summary>
        /// Build URL with function key if needed
        /// </summary>
        private string BuildUrl(string endpoint, string? queryString)
        {
            // Remove leading slash if present
            endpoint = endpoint.TrimStart('/');

            var url = endpoint;

            // Add function key if configured (for deployed Azure Functions)
            if (!string.IsNullOrEmpty(_functionKey))
            {
                var separator = string.IsNullOrEmpty(queryString) ? "?" : "&";
                queryString = $"{queryString}{separator}code={_functionKey}";
            }

            // Add query string if present
            if (!string.IsNullOrEmpty(queryString))
            {
                queryString = queryString.TrimStart('?');
                url = $"{endpoint}?{queryString}";
            }

            return url;
        }

        /// <summary>
        /// Check if the Functions API is running and reachable
        /// </summary>
        public async Task<bool> IsApiAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the base URL being used for tests
        /// </summary>
        public string GetBaseUrl() => _baseUrl;

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
