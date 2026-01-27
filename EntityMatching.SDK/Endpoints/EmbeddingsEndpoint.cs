using EntityMatching.Core.Models.Embedding;
using EntityMatching.SDK.Utils;

namespace EntityMatching.SDK.Endpoints;

/// <summary>
/// Embeddings endpoint wrapper
/// Handles privacy-first vector upload operations
/// </summary>
public class EmbeddingsEndpoint
{
    private readonly HttpClientHelper _http;

    internal EmbeddingsEndpoint(HttpClientHelper http)
    {
        _http = http;
    }

    /// <summary>
    /// Upload a pre-computed embedding vector
    /// Privacy-first: Client generates embeddings locally, uploads only vectors
    /// </summary>
    /// <param name="entityId">Entity ID to associate the embedding with</param>
    /// <param name="request">Embedding data (1536-dimensional vector)</param>
    /// <returns>Upload confirmation with status</returns>
    public async Task<EmbeddingUploadResponse> UploadAsync(Guid entityId, UploadEmbeddingRequest request)
    {
        return await _http.PostAsync<EmbeddingUploadResponse>(
            $"/api/v1/entities/{entityId}/embeddings/upload",
            request);
    }
}

/// <summary>
/// Response from embedding upload operation
/// </summary>
public class EmbeddingUploadResponse
{
    public string ProfileId { get; set; } = "";
    public string Status { get; set; } = "";
    public int Dimensions { get; set; }
    public string EmbeddingModel { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public string Message { get; set; } = "";
}
