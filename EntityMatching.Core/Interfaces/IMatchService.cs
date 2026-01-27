using EntityMatching.Core.Models.Matching;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for managing match requests and connections between entities
    /// Handles request lifecycle, status transitions, and expiration
    /// </summary>
    public interface IMatchService
    {
        /// <summary>
        /// Create a new match request from one entity to another
        /// </summary>
        /// <param name="request">The match request to create</param>
        /// <returns>The created match request</returns>
        Task<MatchRequest> CreateMatchRequestAsync(MatchRequest request);

        /// <summary>
        /// Update the status of a match request with validation
        /// Enforces valid status transitions (e.g., can't go from Declined to Pending)
        /// </summary>
        /// <param name="requestId">Match request ID</param>
        /// <param name="newStatus">New status to transition to</param>
        /// <param name="responseMessage">Optional response message</param>
        /// <returns>Updated match request</returns>
        Task<MatchRequest> UpdateMatchStatusAsync(string requestId, MatchStatus newStatus, string? responseMessage = null);

        /// <summary>
        /// Get a specific match request by ID
        /// </summary>
        /// <param name="requestId">Match request ID</param>
        /// <returns>The match request or null if not found</returns>
        Task<MatchRequest?> GetMatchRequestAsync(string requestId);

        /// <summary>
        /// Get all match requests received by an entity (incoming)
        /// </summary>
        /// <param name="targetId">ID of the target entity</param>
        /// <param name="includeResolved">Whether to include resolved requests (default: false)</param>
        /// <returns>List of incoming match requests</returns>
        Task<IEnumerable<MatchRequest>> GetIncomingMatchRequestsAsync(string targetId, bool includeResolved = false);

        /// <summary>
        /// Get all match requests sent by an entity (outgoing)
        /// </summary>
        /// <param name="requesterId">ID of the requester entity</param>
        /// <param name="includeResolved">Whether to include resolved requests (default: false)</param>
        /// <returns>List of outgoing match requests</returns>
        Task<IEnumerable<MatchRequest>> GetOutgoingMatchRequestsAsync(string requesterId, bool includeResolved = false);

        /// <summary>
        /// Expire old match requests that have passed their ExpiresAt timestamp
        /// Called by background job
        /// </summary>
        /// <returns>Number of requests expired</returns>
        Task<int> ExpireOldMatchRequestsAsync();

        /// <summary>
        /// Initialize the storage container if needed
        /// </summary>
        Task InitializeAsync();
    }
}
