using Azure.AI.OpenAI;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.SDK.Endpoints;
using EntityMatching.SDK.Utils;

namespace EntityMatching.SDK;

/// <summary>
/// Options for configuring the EntityMatchingClient
/// </summary>
public class EntityMatchingClientOptions
{
    /// <summary>
    /// API key for EntityMatchingAPI (Ocp-Apim-Subscription-Key)
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Base URL for the API (defaults to https://api.bystorm.com)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.bystorm.com";

    /// <summary>
    /// OpenAI API key for client-side embedding generation
    /// Required for privacy-first features like UploadResumeAsync()
    /// </summary>
    public string? OpenAIKey { get; set; }
}

/// <summary>
/// EntityMatchingAPI Client SDK
///
/// Privacy-first client for semantic profile matching with zero PII storage.
/// Supports client-side embedding generation to ensure sensitive data never leaves the user's device.
/// </summary>
/// <example>
/// <code>
/// var client = new EntityMatchingClient(new EntityMatchingClientOptions
/// {
///     ApiKey = "your-api-key",
///     OpenAIKey = "your-openai-key" // Optional, for client-side embedding
/// });
///
/// // Privacy-first resume upload
/// await client.UploadResumeAsync(entityId, resumeText);
/// </code>
/// </example>
public class EntityMatchingClient
{
    private readonly HttpClientHelper _http;
    private readonly OpenAIClient? _openAI;

    /// <summary>
    /// Entities endpoint - CRUD operations for entities
    /// </summary>
    public EntitiesEndpoint Entities { get; }

    /// <summary>
    /// Embeddings endpoint - Privacy-first vector upload
    /// </summary>
    public EmbeddingsEndpoint Embeddings { get; }

    /// <summary>
    /// Conversations endpoint - Conversational profiling with AI
    /// </summary>
    public ConversationsEndpoint Conversations { get; }

    /// <summary>
    /// Search endpoint - Semantic search with attribute filtering
    /// </summary>
    public SearchEndpoint Search { get; }

    /// <summary>
    /// Initialize a new EntityMatchingClient
    /// </summary>
    /// <param name="options">Client configuration options</param>
    public EntityMatchingClient(EntityMatchingClientOptions options)
    {
        var baseUrl = options.BaseUrl.TrimEnd('/');
        _http = new HttpClientHelper(baseUrl, options.ApiKey);

        // Initialize OpenAI client if API key provided
        if (!string.IsNullOrEmpty(options.OpenAIKey))
        {
            _openAI = new OpenAIClient(options.OpenAIKey);
        }

        // Initialize endpoint wrappers
        Entities = new EntitiesEndpoint(_http);
        Embeddings = new EmbeddingsEndpoint(_http);
        Conversations = new ConversationsEndpoint(_http);
        Search = new SearchEndpoint(_http);
    }

    /// <summary>
    /// Upload resume with privacy-first approach
    ///
    /// Generates embedding locally using OpenAI API, then uploads ONLY the vector.
    /// The original resume text never leaves the client device.
    ///
    /// <para><b>Privacy Benefits:</b></para>
    /// <list type="bullet">
    /// <item>Server never sees resume text</item>
    /// <item>Only 1536-dimensional vector is stored</item>
    /// <item>Even if database is breached, attackers get meaningless numbers</item>
    /// <item>GDPR compliant - no PII means no data protection requirements</item>
    /// </list>
    /// </summary>
    /// <param name="entityId">Entity ID to associate the resume with</param>
    /// <param name="resumeText">Resume text (stays on client, never sent to server)</param>
    /// <returns>Task representing the async operation</returns>
    /// <exception cref="InvalidOperationException">Thrown if OpenAI API key was not provided in constructor</exception>
    /// <example>
    /// <code>
    /// var resumeText = @"
    ///   Senior Software Engineer with 10 years experience in Python and AWS.
    ///   Built machine learning pipelines processing 100M+ events/day.
    /// ";
    ///
    /// await client.UploadResumeAsync(entityId, resumeText);
    /// // Resume text stays local, only vector uploaded!
    /// </code>
    /// </example>
    public async Task UploadResumeAsync(Guid entityId, string resumeText)
    {
        if (_openAI == null)
        {
            throw new InvalidOperationException(
                "OpenAI API key required for client-side embedding generation. " +
                "Provide OpenAIKey in EntityMatchingClientOptions.");
        }

        // Step 1: Generate embedding locally (privacy-first!)
        var embeddingOptions = new EmbeddingsOptions("text-embedding-3-small", new[] { resumeText });
        var embeddingResponse = await _openAI.GetEmbeddingsAsync(embeddingOptions);

        var vector = embeddingResponse.Value.Data[0].Embedding.ToArray();

        // Step 2: Upload ONLY the vector (never the text)
        await Embeddings.UploadAsync(entityId, new UploadEmbeddingRequest
        {
            Embedding = vector,
            EmbeddingModel = "text-embedding-3-small",
            Metadata = new ClientEmbeddingMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                ClientVersion = "1.0.0"
            }
        });

        // Original resume text stays on client, never sent to server
        // This is the core privacy guarantee of EntityMatchingAPI
    }

    /// <summary>
    /// Generate embedding for any text (not just resumes)
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <returns>1536-dimensional embedding vector</returns>
    /// <exception cref="InvalidOperationException">Thrown if OpenAI API key was not provided</exception>
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (_openAI == null)
        {
            throw new InvalidOperationException(
                "OpenAI API key required for embedding generation. " +
                "Provide OpenAIKey in EntityMatchingClientOptions.");
        }

        var embeddingOptions = new EmbeddingsOptions("text-embedding-3-small", new[] { text });
        var response = await _openAI.GetEmbeddingsAsync(embeddingOptions);

        return response.Value.Data[0].Embedding.ToArray();
    }
}
