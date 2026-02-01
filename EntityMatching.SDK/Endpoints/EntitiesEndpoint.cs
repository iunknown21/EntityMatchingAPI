using EntityMatching.SDK.Utils;
using EntityMatching.Shared.Models;

namespace EntityMatching.SDK.Endpoints;

/// <summary>
/// Profiles endpoint wrapper
/// Handles all profile CRUD operations
/// </summary>
public class EntitiesEndpoint
{
    private readonly HttpClientHelper _http;

    internal EntitiesEndpoint(HttpClientHelper http)
    {
        _http = http;
    }

    /// <summary>
    /// Get all profiles for a user
    /// </summary>
    public async Task<List<Entity>> ListAsync(string userId)
    {
        return await _http.GetAsync<List<Entity>>("/api/v1/entities",
            new Dictionary<string, string> { { "userId", userId } });
    }

    /// <summary>
    /// Get a single profile by ID
    /// </summary>
    public async Task<Entity> GetAsync(Guid entityId)
    {
        return await _http.GetAsync<Entity>($"/api/v1/entities/{entityId}");
    }

    /// <summary>
    /// Create a new profile
    /// </summary>
    public async Task<Entity> CreateAsync(Entity profile)
    {
        return await _http.PostAsync<Entity>("/api/v1/entities", profile);
    }

    /// <summary>
    /// Create multiple entities in bulk
    /// </summary>
    public async Task<List<Entity>> CreateBulkAsync(List<Entity> entities)
    {
        return await _http.PostAsync<List<Entity>>("/api/v1/entities/bulk", entities);
    }

    /// <summary>
    /// Update an existing profile
    /// </summary>
    public async Task<Entity> UpdateAsync(Guid entityId, Entity profile)
    {
        return await _http.PutAsync<Entity>($"/api/v1/entities/{entityId}", profile);
    }

    /// <summary>
    /// Delete a profile
    /// </summary>
    public async Task DeleteAsync(Guid entityId)
    {
        await _http.DeleteAsync($"/api/v1/entities/{entityId}");
    }
}
