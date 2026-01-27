using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Search
{
    /// <summary>
    /// Importance level for safety and accommodation requirements
    /// Used for profile-based search to ensure results meet user's needs
    /// </summary>
    public enum SafetyImportance
    {
        /// <summary>
        /// Critical - Must be met or result is completely unsuitable (safety/health)
        /// Examples: Severe allergies, wheelchair accessibility, phobias
        /// </summary>
        Critical = 4,

        /// <summary>
        /// High - Strongly preferred, major penalty if not met
        /// Examples: Dietary restrictions for food events, quiet environment for sensory issues
        /// </summary>
        High = 3,

        /// <summary>
        /// Medium - Preferred but not dealbreaker
        /// Examples: Parking availability, pet-friendliness
        /// </summary>
        Medium = 2,

        /// <summary>
        /// Low - Nice to have but minimal impact if not met
        /// Examples: Aesthetic preferences, minor conveniences
        /// </summary>
        Low = 1
    }

    /// <summary>
    /// Represents a safety or accommodation requirement extracted from a profile
    /// Domain-agnostic - works for events, jobs, housing, travel, etc.
    /// </summary>
    public class SafetyRequirement
    {
        /// <summary>
        /// Unique identifier for this requirement (e.g., "no_peanuts", "wheelchair_accessible")
        /// </summary>
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; } = "";

        /// <summary>
        /// Human-readable description of why this requirement exists
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Importance level - determines scoring impact
        /// </summary>
        [JsonProperty(PropertyName = "importance")]
        public SafetyImportance Importance { get; set; }

        /// <summary>
        /// Additional context about this requirement
        /// </summary>
        [JsonProperty(PropertyName = "context")]
        public string? Context { get; set; }

        /// <summary>
        /// Which thing types this requirement applies to (e.g., "food_events", "restaurants", "all")
        /// Empty list means applies to all types
        /// </summary>
        [JsonProperty(PropertyName = "applicableThingTypes")]
        public List<string> ApplicableThingTypes { get; set; } = new List<string>();

        public SafetyRequirement()
        {
        }

        public SafetyRequirement(string key, string description, SafetyImportance importance, string? context = null)
        {
            Key = key;
            Description = description;
            Importance = importance;
            Context = context;
        }
    }
}
