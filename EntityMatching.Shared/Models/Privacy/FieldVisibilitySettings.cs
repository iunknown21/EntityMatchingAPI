using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models.Privacy
{
    /// <summary>
    /// Maps profile field paths to their visibility settings
    /// Stored as part of Profile document in Cosmos DB
    /// </summary>
    public class FieldVisibilitySettings
    {
        /// <summary>
        /// Map of JSON field paths to visibility levels
        /// Examples:
        ///   - "name" → Public
        ///   - "bio" → Public
        ///   - "birthday" → Private
        ///   - "contactInformation" → Private
        ///   - "naturePreferences.hasPets" → Public
        ///   - "naturePreferences.petTypes" → Public
        ///   - "personalityClassifications.mbtiType" → FriendsOnly
        ///   - "preferences.favoriteCuisines" → Public
        /// </summary>
        [JsonProperty(PropertyName = "fieldVisibility")]
        public Dictionary<string, FieldVisibility> FieldVisibilityMap { get; set; } = new Dictionary<string, FieldVisibility>();

        /// <summary>
        /// Default visibility for fields not explicitly configured
        /// Recommended: Private (fail-closed security)
        /// </summary>
        [JsonProperty(PropertyName = "defaultVisibility")]
        public FieldVisibility DefaultVisibility { get; set; } = FieldVisibility.Private;

        /// <summary>
        /// Helper to get visibility for a specific field path
        /// Returns DefaultVisibility if field not in map
        /// </summary>
        /// <param name="fieldPath">JSON path to field (e.g., "name", "naturePreferences.hasPets")</param>
        /// <returns>Visibility level for the field</returns>
        public FieldVisibility GetFieldVisibility(string fieldPath)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
            {
                return DefaultVisibility;
            }

            return FieldVisibilityMap.TryGetValue(fieldPath, out var visibility)
                ? visibility
                : DefaultVisibility;
        }

        /// <summary>
        /// Helper to set visibility for a field path
        /// </summary>
        /// <param name="fieldPath">JSON path to field</param>
        /// <param name="visibility">Visibility level</param>
        public void SetFieldVisibility(string fieldPath, FieldVisibility visibility)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
            {
                return;
            }

            FieldVisibilityMap[fieldPath] = visibility;
        }

        /// <summary>
        /// Bulk set visibility for multiple fields
        /// </summary>
        /// <param name="visibilityMap">Map of field paths to visibility levels</param>
        public void SetBulkVisibility(Dictionary<string, FieldVisibility> visibilityMap)
        {
            if (visibilityMap == null) return;

            foreach (var kvp in visibilityMap)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    FieldVisibilityMap[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Remove visibility setting for a field (will revert to DefaultVisibility)
        /// </summary>
        /// <param name="fieldPath">JSON path to field</param>
        public void RemoveFieldVisibility(string fieldPath)
        {
            if (!string.IsNullOrWhiteSpace(fieldPath))
            {
                FieldVisibilityMap.Remove(fieldPath);
            }
        }

        /// <summary>
        /// Check if a field has an explicit visibility setting
        /// </summary>
        /// <param name="fieldPath">JSON path to field</param>
        /// <returns>True if field has explicit setting, false if using default</returns>
        public bool HasExplicitVisibility(string fieldPath)
        {
            return !string.IsNullOrWhiteSpace(fieldPath) && FieldVisibilityMap.ContainsKey(fieldPath);
        }

        /// <summary>
        /// Get all field paths that are set to Public visibility
        /// </summary>
        /// <returns>List of public field paths</returns>
        public List<string> GetPublicFields()
        {
            var publicFields = new List<string>();

            foreach (var kvp in FieldVisibilityMap)
            {
                if (kvp.Value == FieldVisibility.Public)
                {
                    publicFields.Add(kvp.Key);
                }
            }

            return publicFields;
        }

        /// <summary>
        /// Get all field paths that are set to Private visibility
        /// </summary>
        /// <returns>List of private field paths</returns>
        public List<string> GetPrivateFields()
        {
            var privateFields = new List<string>();

            foreach (var kvp in FieldVisibilityMap)
            {
                if (kvp.Value == FieldVisibility.Private)
                {
                    privateFields.Add(kvp.Key);
                }
            }

            return privateFields;
        }
    }
}
