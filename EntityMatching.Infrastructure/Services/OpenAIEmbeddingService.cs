using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using EntityMatching.Core.Interfaces;
using System;
using System.ClientModel;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// OpenAI embedding service using text-embedding-3-small model
    /// Generates 1536-dimensional vector embeddings for semantic similarity
    /// </summary>
    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly ILogger<OpenAIEmbeddingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _modelName;
        private readonly int _dimensions;
        private readonly int _maxRetries;
        private readonly string _apiKey;
        private readonly string? _endpoint;
        private EmbeddingClient? _embeddingClient;

        public string ModelName => _modelName;
        public int? Dimensions => _dimensions;

        public OpenAIEmbeddingService(
            IConfiguration configuration,
            ILogger<OpenAIEmbeddingService> logger)
        {
            _logger = logger;
            _configuration = configuration;

            // Get configuration values but don't initialize client yet
            _apiKey = configuration["OpenAI:ApiKey"]
                ?? configuration["OpenAI__ApiKey"]
                ?? throw new InvalidOperationException("OpenAI:ApiKey configuration is required");

            _modelName = configuration["OpenAI:EmbeddingModel"]
                ?? configuration["OpenAI__EmbeddingModel"]
                ?? "text-embedding-3-small";

            _dimensions = int.Parse(configuration["OpenAI:EmbeddingDimensions"]
                ?? configuration["OpenAI__EmbeddingDimensions"]
                ?? "1536");

            _maxRetries = int.Parse(configuration["OpenAI:MaxRetries"]
                ?? configuration["OpenAI__MaxRetries"]
                ?? "3");

            _endpoint = configuration["OpenAI:Endpoint"] ?? configuration["OpenAI__Endpoint"];

            _logger.LogInformation(
                "OpenAI Embedding Service configured with model {Model} ({Dimensions} dimensions)",
                _modelName, _dimensions);
        }

        private EmbeddingClient GetEmbeddingClient()
        {
            if (_embeddingClient == null)
            {
                if (!string.IsNullOrEmpty(_endpoint))
                {
                    // Azure OpenAI
                    _logger.LogInformation("Creating Azure OpenAI Embedding Client with endpoint {Endpoint}", _endpoint);
                    var azureClient = new AzureOpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
                    _embeddingClient = azureClient.GetEmbeddingClient(_modelName);
                }
                else
                {
                    // OpenAI directly
                    _logger.LogInformation("Creating OpenAI Embedding Client");
                    var openAIClient = new OpenAI.OpenAIClient(_apiKey);
                    _embeddingClient = openAIClient.GetEmbeddingClient(_modelName);
                }
            }
            return _embeddingClient;
        }

        public async Task<float[]?> GenerateEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Attempted to generate embedding for null or empty text");
                return null;
            }

            var attempt = 0;
            Exception? lastException = null;

            while (attempt < _maxRetries)
            {
                attempt++;
                try
                {
                    _logger.LogDebug(
                        "Generating embedding (attempt {Attempt}/{MaxRetries}) for text of length {Length}",
                        attempt, _maxRetries, text.Length);

                    var client = GetEmbeddingClient();
                    var response = await client.GenerateEmbeddingAsync(text);
                    var embedding = response.Value;

                    var vector = embedding.ToFloats().ToArray();

                    _logger.LogDebug(
                        "Successfully generated {Dimensions}-dimensional embedding",
                        vector.Length);

                    return vector;
                }
                catch (ClientResultException ex) when (ex.Status == 429)
                {
                    // Rate limit - wait and retry
                    lastException = ex;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    _logger.LogWarning(
                        "Rate limit hit (attempt {Attempt}/{MaxRetries}), waiting {Delay}s before retry",
                        attempt, _maxRetries, delay.TotalSeconds);

                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(
                        ex,
                        "Error generating embedding (attempt {Attempt}/{MaxRetries}): {Message}",
                        attempt, _maxRetries, ex.Message);

                    if (attempt < _maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                        await Task.Delay(delay);
                    }
                }
            }

            _logger.LogError(
                lastException,
                "Failed to generate embedding after {MaxRetries} attempts",
                _maxRetries);

            return null;
        }
    }
}
