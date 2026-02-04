using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.SDK.Utils;

namespace EntityMatching.SDK.Endpoints;

/// <summary>
/// Conversations endpoint wrapper
/// Handles conversational profiling with AI
/// </summary>
public class ConversationsEndpoint
{
    private readonly HttpClientHelper _http;

    internal ConversationsEndpoint(HttpClientHelper http)
    {
        _http = http;
    }

    /// <summary>
    /// Send a message to the conversational profiling AI
    /// </summary>
    /// <param name="entityId">Entity ID to build conversation for</param>
    /// <param name="userId">User ID sending the message</param>
    /// <param name="message">Message containing profile information</param>
    /// <param name="systemPrompt">System prompt defining the conversation behavior. REQUIRED for first message in a new conversation, optional for subsequent messages (uses stored prompt).</param>
    /// <returns>AI response with extracted insights</returns>
    public async Task<ConversationResponse> SendMessageAsync(Guid entityId, string userId, string message, string? systemPrompt = null)
    {
        var request = new { Message = message, UserId = userId, SystemPrompt = systemPrompt };
        return await _http.PostAsync<ConversationResponse>(
            $"/api/v1/entities/{entityId}/conversation",
            request);
    }

    /// <summary>
    /// Get conversation history for a profile
    /// </summary>
    /// <param name="entityId">Entity ID</param>
    /// <returns>Conversation history with all extracted insights</returns>
    public async Task<ConversationContext> GetHistoryAsync(Guid entityId)
    {
        return await _http.GetAsync<ConversationContext>(
            $"/api/v1/entities/{entityId}/conversation");
    }

    /// <summary>
    /// Delete conversation history for a profile
    /// </summary>
    /// <param name="entityId">Entity ID</param>
    public async Task DeleteAsync(Guid entityId)
    {
        await _http.DeleteAsync($"/api/v1/entities/{entityId}/conversation");
    }
}
