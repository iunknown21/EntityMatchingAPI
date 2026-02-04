using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EntityMatching.SDK.Utils;

/// <summary>
/// HTTP client helper for ProfileMatchingAPI
/// Handles authentication, request/response serialization, and error handling
/// </summary>
internal class HttpClientHelper
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public HttpClientHelper(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/'))
        };
        _apiKey = apiKey;
        ConfigureHeaders();
    }

    private void ConfigureHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("x-functions-key", _apiKey); // For Azure Functions auth
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null)
    {
        var url = BuildUrl(endpoint, queryParams);
        var response = await _httpClient.GetAsync(url);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<T> PostAsync<T>(string endpoint, object? body = null)
    {
        var content = body != null
            ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            : null;

        var response = await _httpClient.PostAsync(endpoint, content);
        return await HandleResponseAsync<T>(response);
    }

    public async Task<T> PutAsync<T>(string endpoint, object? body = null)
    {
        var content = body != null
            ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            : null;

        var response = await _httpClient.PutAsync(endpoint, content);
        return await HandleResponseAsync<T>(response);
    }

    public async Task DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    private string BuildUrl(string endpoint, Dictionary<string, string>? queryParams)
    {
        if (queryParams == null || queryParams.Count == 0)
            return endpoint;

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{endpoint}?{queryString}";
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        // Handle empty responses
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return default!;
        }

        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }
}
