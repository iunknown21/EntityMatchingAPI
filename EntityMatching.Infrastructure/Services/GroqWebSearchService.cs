using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Web search service using Groq API with web search capabilities
    /// Generic implementation that can search for any type of thing (events, gifts, jobs, etc.)
    /// </summary>
    public class GroqWebSearchService : IWebSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _groqApiKey;
        private readonly ILogger<GroqWebSearchService> _logger;
        private readonly WebSearchConfig _config;
        private const string GroqChatEndpoint = "https://api.groq.com/openai/v1/chat/completions";

        public GroqWebSearchService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GroqWebSearchService> logger,
            WebSearchConfig? config = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _groqApiKey = configuration["Groq:ApiKey"] ?? configuration["ApiKeys:Groq"] ?? configuration["ApiKeys__Groq"];
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? new WebSearchConfig();

            if (string.IsNullOrEmpty(_groqApiKey))
            {
                _logger.LogWarning("Groq API key not configured - web search will not work");
            }
        }

        public async Task<IEnumerable<TResult>> SearchAsync<TResult>(
            string query,
            SearchContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_groqApiKey))
            {
                _logger.LogError("Groq API key not configured");
                return new List<TResult>();
            }

            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("Search query is empty");
                return new List<TResult>();
            }

            try
            {
                _logger.LogInformation("🔍 Searching for {ThingType} with query: {Query}",
                    context.ThingType, query);

                // Build system prompt based on thing type
                var systemPrompt = context.SystemPromptOverride ?? BuildSystemPrompt(context.ThingType, context.MaxResults);

                // Build Groq API request
                var request = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = query }
                    },
                    temperature = _config.Temperature,
                    max_tokens = _config.MaxTokens
                };

                var requestJson = JsonSerializer.Serialize(request);
                var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Execute with retry logic
                var (response, responseContent) = await CallGroqApiWithRetryAsync(requestContent, "SearchAsync", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Groq API returned error status: {StatusCode}", response.StatusCode);
                    return new List<TResult>();
                }

                // Parse response
                var results = ParseGroqResponse<TResult>(responseContent);
                _logger.LogInformation("✅ Found {Count} {ThingType} from web search",
                    results.Count(), context.ThingType);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during web search for {ThingType}", context.ThingType);
                return new List<TResult>();
            }
        }

        /// <summary>
        /// Call Groq API with retry logic and exponential backoff
        /// </summary>
        private async Task<(HttpResponseMessage response, string content)> CallGroqApiWithRetryAsync(
            StringContent requestContent,
            string operation,
            CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < _config.MaxRetries; attempt++)
            {
                try
                {
                    // Apply exponential backoff delay if this is a retry
                    if (attempt > 0)
                    {
                        var delay = _config.RetryDelayMs * (int)Math.Pow(2, attempt - 1);
                        _logger.LogInformation("⏱️ {Operation}: Waiting {DelayMs}ms before retry {Attempt}/{MaxRetries}",
                            operation, delay, attempt + 1, _config.MaxRetries);
                        await Task.Delay(delay, cancellationToken);
                    }

                    // Set authorization header
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

                    _logger.LogInformation("🔍 {Operation}: Calling Groq API (attempt {Attempt}/{MaxRetries})",
                        operation, attempt + 1, _config.MaxRetries);

                    var response = await _httpClient.PostAsync(GroqChatEndpoint, requestContent, cancellationToken);
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("✅ {Operation}: Groq API succeeded on attempt {Attempt}",
                            operation, attempt + 1);
                        return (response, content);
                    }

                    // Handle rate limiting specifically
                    if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < _config.MaxRetries - 1)
                    {
                        _logger.LogWarning("⚠️ {Operation}: Rate limited (429), will retry", operation);
                        continue;
                    }

                    _logger.LogWarning("❌ {Operation}: Groq API failed ({StatusCode}) on attempt {Attempt}: {Error}",
                        operation, response.StatusCode, attempt + 1, content);

                    // If this was the last attempt, return the failed response
                    if (attempt == _config.MaxRetries - 1)
                    {
                        return (response, content);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Operation cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ {Operation}: Groq API error on attempt {Attempt}",
                        operation, attempt + 1);

                    if (attempt == _config.MaxRetries - 1)
                    {
                        // Last attempt failed, return error response
                        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                        return (errorResponse, "[]");
                    }
                }
            }

            // Should not reach here, but just in case
            _logger.LogError("❌ {Operation}: All Groq API retries exhausted", operation);
            var finalResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return (finalResponse, "[]");
        }

        /// <summary>
        /// Parse Groq API response and extract results
        /// Handles both direct JSON arrays and JSON within markdown code blocks
        /// </summary>
        private IEnumerable<TResult> ParseGroqResponse<TResult>(string responseContent)
        {
            try
            {
                // Parse the Groq API response structure
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                // Extract the message content from Groq's response format
                if (root.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        var messageContent = content.GetString();
                        if (!string.IsNullOrEmpty(messageContent))
                        {
                            // Extract JSON from markdown code blocks if present
                            var jsonContent = ExtractJsonFromMarkdown(messageContent);

                            // Try to deserialize as array of results
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                AllowTrailingCommas = true
                            };

                            var results = JsonSerializer.Deserialize<List<TResult>>(jsonContent, options);
                            return results ?? new List<TResult>();
                        }
                    }
                }

                _logger.LogWarning("Could not extract results from Groq response");
                return new List<TResult>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing Groq response JSON");
                return new List<TResult>();
            }
        }

        /// <summary>
        /// Extract JSON content from markdown code blocks
        /// Handles various markdown formatting patterns
        /// </summary>
        private string ExtractJsonFromMarkdown(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "[]";

            _logger.LogDebug("ExtractJsonFromMarkdown input length: {Length}", content.Length);

            // Look for JSON in markdown code blocks with different variations
            var patterns = new[]
            {
                "```json",
                "```JSON",
                "``` json",
                "``` JSON",
                "```\njson",
                "```\nJSON"
            };

            foreach (var pattern in patterns)
            {
                var jsonStart = content.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (jsonStart >= 0)
                {
                    jsonStart += pattern.Length;

                    // Skip any whitespace/newlines after the marker
                    while (jsonStart < content.Length && char.IsWhiteSpace(content[jsonStart]))
                        jsonStart++;

                    var jsonEnd = content.IndexOf("```", jsonStart);
                    if (jsonEnd > jsonStart)
                    {
                        var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart).Trim();
                        _logger.LogDebug("Found JSON with pattern '{Pattern}', length: {Length}",
                            pattern, jsonContent.Length);
                        return jsonContent;
                    }
                }
            }

            // Look for any code blocks and check if they contain JSON
            var codeBlockStart = 0;
            while ((codeBlockStart = content.IndexOf("```", codeBlockStart)) >= 0)
            {
                codeBlockStart += 3;

                // Skip the language identifier line if present
                var lineEnd = content.IndexOf('\n', codeBlockStart);
                if (lineEnd > codeBlockStart)
                {
                    codeBlockStart = lineEnd + 1;
                }

                var codeBlockEnd = content.IndexOf("```", codeBlockStart);
                if (codeBlockEnd > codeBlockStart)
                {
                    var blockContent = content.Substring(codeBlockStart, codeBlockEnd - codeBlockStart).Trim();

                    // Check if it looks like JSON (starts with [ or {)
                    if (blockContent.StartsWith("[") || blockContent.StartsWith("{"))
                    {
                        _logger.LogDebug("Found potential JSON in generic code block, length: {Length}",
                            blockContent.Length);
                        return blockContent;
                    }

                    codeBlockStart = codeBlockEnd + 3;
                }
                else
                {
                    break;
                }
            }

            // If no code blocks found, check if the content itself is JSON
            var trimmedContent = content.Trim();
            if (trimmedContent.StartsWith("[") || trimmedContent.StartsWith("{"))
            {
                _logger.LogDebug("Content appears to be direct JSON");
                return trimmedContent;
            }

            _logger.LogWarning("No JSON found in content");
            return "[]";
        }

        /// <summary>
        /// Build system prompt for a specific thing type
        /// </summary>
        private string BuildSystemPrompt(string thingType, int maxResults)
        {
            return thingType.ToLowerInvariant() switch
            {
                "event" or "events" => BuildEventSearchPrompt(maxResults),
                "gift" or "gifts" => BuildGiftSearchPrompt(maxResults),
                "job" or "jobs" => BuildJobSearchPrompt(maxResults),
                _ => BuildGenericSearchPrompt(thingType, maxResults)
            };
        }

        private string BuildEventSearchPrompt(int maxResults)
        {
            return $@"You are a helpful assistant that searches the web for real events and activities.

When given a search query, use your web search capabilities to find up to {maxResults} real, current events that match the criteria.

Return results as a JSON array with this exact structure:
```json
[
  {{
    ""title"": ""Event name"",
    ""description"": ""Detailed description"",
    ""location"": ""Venue name and address"",
    ""eventDate"": ""2025-01-15T19:00:00"",
    ""category"": ""Category (e.g., music, food, outdoor, culture)"",
    ""price"": 25.00,
    ""externalUrl"": ""https://..."",
    ""source"": ""Where you found this information""
  }}
]
```

IMPORTANT:
- Only return REAL events you found via web search
- Include specific dates, times, and locations
- Provide actual URLs where users can get tickets/info
- If no events found, return an empty array: []
- Return ONLY the JSON array, no additional text";
        }

        private string BuildGiftSearchPrompt(int maxResults)
        {
            return $@"You are a helpful assistant that searches the web for gift ideas and products.

When given a search query, use your web search capabilities to find up to {maxResults} relevant gift ideas or products.

Return results as a JSON array with this exact structure:
```json
[
  {{
    ""title"": ""Product/gift name"",
    ""description"": ""Description and why it's a good gift"",
    ""price"": 49.99,
    ""category"": ""Category (e.g., books, tech, home, experience)"",
    ""externalUrl"": ""https://..."",
    ""source"": ""Where you found this information""
  }}
]
```

IMPORTANT:
- Find real products available for purchase
- Include accurate pricing where available
- Provide direct links to products
- If no gifts found, return an empty array: []
- Return ONLY the JSON array, no additional text";
        }

        private string BuildJobSearchPrompt(int maxResults)
        {
            return $@"You are a helpful assistant that searches the web for job listings.

When given a search query, use your web search capabilities to find up to {maxResults} relevant job postings.

Return results as a JSON array with this exact structure:
```json
[
  {{
    ""title"": ""Job title"",
    ""description"": ""Job description and requirements"",
    ""company"": ""Company name"",
    ""location"": ""Job location or 'Remote'"",
    ""salary"": ""Salary range if available"",
    ""category"": ""Job category (e.g., engineering, marketing, sales)"",
    ""externalUrl"": ""https://..."",
    ""source"": ""Job board or company website""
  }}
]
```

IMPORTANT:
- Only return REAL job postings you found via web search
- Include specific company names and locations
- Provide actual URLs to job applications
- If no jobs found, return an empty array: []
- Return ONLY the JSON array, no additional text";
        }

        private string BuildGenericSearchPrompt(string thingType, int maxResults)
        {
            return $@"You are a helpful assistant that searches the web for {thingType}.

When given a search query, use your web search capabilities to find up to {maxResults} relevant results.

Return results as a JSON array where each item contains relevant information about the {thingType}.

IMPORTANT:
- Only return REAL information you found via web search
- Include specific details and sources
- Provide URLs where applicable
- If no results found, return an empty array: []
- Return ONLY a JSON array, no additional text";
        }
    }
}
