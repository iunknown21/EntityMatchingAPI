using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Search;
using System.Collections.Generic;

namespace EntityMatching.Core.Interfaces
{
    /// <summary>
    /// Service for evaluating structured attribute filters on entities
    /// Supports privacy enforcement and complex filter logic (AND/OR/nested groups)
    /// </summary>
    public interface IAttributeFilterService
    {
        /// <summary>
        /// Evaluate all filters in a filter group against an entity
        /// Respects field-level privacy settings
        /// </summary>
        /// <param name="entity">Entity to evaluate filters against</param>
        /// <param name="filterGroup">Filter group with conditions to check</param>
        /// <param name="requestingUserId">User ID of the requester (null = anonymous)</param>
        /// <param name="enforcePrivacy">Whether to enforce field visibility rules (recommended: true)</param>
        /// <returns>
        /// True if entity matches all filters (respecting logical operators)
        /// False if entity doesn't match OR if privacy restrictions prevent evaluation
        /// </returns>
        /// <remarks>
        /// Privacy enforcement:
        /// - If enforcePrivacy=true and field is not visible to requestingUserId, that filter is SKIPPED (fail-closed)
        /// - This means private fields cannot be used to filter results for unauthorized users
        /// - Example: If "birthday" is Private and user is anonymous, age filter is ignored
        /// </remarks>
        bool EvaluateFilters(
            Entity entity,
            FilterGroup filterGroup,
            string? requestingUserId,
            bool enforcePrivacy);

        /// <summary>
        /// Get the actual attribute values that matched the filters
        /// Used to populate EntityMatch.MatchedAttributes for transparency
        /// </summary>
        /// <param name="entity">Entity that matched the filters</param>
        /// <param name="filterGroup">Filter group that was evaluated</param>
        /// <param name="requestingUserId">User ID of the requester (for privacy)</param>
        /// <param name="enforcePrivacy">Whether to enforce field visibility rules</param>
        /// <returns>
        /// Dictionary mapping field paths to their values
        /// Example: { "gender": "male", "naturePreferences.hasPets": true, "preferences.favoriteFoods": ["hamburgers", "pizza"] }
        /// Only includes fields visible to requestingUserId
        /// </returns>
        Dictionary<string, object> GetMatchedAttributes(
            Entity entity,
            FilterGroup filterGroup,
            string? requestingUserId,
            bool enforcePrivacy);

        /// <summary>
        /// Build a Cosmos DB SQL query string from filter group
        /// Allows pushing simple filters to database layer for performance
        /// </summary>
        /// <param name="filterGroup">Filter group to convert to SQL</param>
        /// <returns>Cosmos DB SQL WHERE clause (without "WHERE" keyword)</returns>
        /// <remarks>
        /// Example output: "c.gender = 'male' AND c.naturePreferences.hasPets = true"
        /// Note: Privacy enforcement must still happen in application layer
        /// This is an optimization for reducing data transfer
        /// </remarks>
        string BuildCosmosQuery(FilterGroup filterGroup);

        /// <summary>
        /// Check if a filter group can be fully evaluated in Cosmos DB
        /// Some complex filters (regex, custom logic) require application-level evaluation
        /// </summary>
        /// <param name="filterGroup">Filter group to check</param>
        /// <returns>True if filter can be pushed to database, false if app-level required</returns>
        /// <remarks>
        /// Returns false for:
        /// - Contains operator on string fields (requires case-insensitive search)
        /// - Nested groups with complex logic
        /// - Filters on computed properties (Age, Location)
        /// Returns true for:
        /// - Simple equality checks (Equals, NotEquals)
        /// - Boolean checks (IsTrue, IsFalse)
        /// - Numeric comparisons (GreaterThan, LessThan, etc.)
        /// </remarks>
        bool CanEvaluateInCosmosDb(FilterGroup filterGroup);
    }
}
