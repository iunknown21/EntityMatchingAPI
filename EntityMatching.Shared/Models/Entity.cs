using Newtonsoft.Json;
using EntityMatching.Shared.Models.Privacy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Universal entity model for comprehensive matching across domains
    /// Supports multiple use cases: people, jobs, properties, products, events, etc.
    /// Designed for bidirectional matching where ANY entity can be both searcher and searchable
    /// </summary>
    public class Entity
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Type of entity (Person, Job, Property, Product, etc.)
        /// Enables entity-specific logic and cross-domain matching
        /// </summary>
        [JsonProperty(PropertyName = "entityType")]
        public EntityType EntityType { get; set; } = EntityType.Person;

        /// <summary>
        /// External system identifier (e.g., MLS ID, job board ID, product SKU)
        /// Used to link back to the source system and prevent duplicates
        /// </summary>
        [JsonProperty(PropertyName = "externalId")]
        public string? ExternalId { get; set; }

        /// <summary>
        /// Source system name (e.g., "MLS", "NTREIS", "Indeed", "Zillow")
        /// Combined with ExternalId creates a unique external reference
        /// </summary>
        [JsonProperty(PropertyName = "externalSource")]
        public string? ExternalSource { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// Free-form description of the entity
        /// For Person: bio, For Job: job description, For Property: property description
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// Flexible attribute storage for entity-specific data
        /// Examples:
        /// - Person: { "skills": ["Python", "AWS"], "yearsExperience": 5 }
        /// - Job: { "requiredSkills": ["Python"], "salaryRange": { "min": 100000, "max": 150000 } }
        /// - Property: { "bedrooms": 3, "bathrooms": 2, "petsAllowed": true }
        /// </summary>
        [JsonProperty(PropertyName = "attributes")]
        [System.Text.Json.Serialization.JsonConverter(typeof(EntityMatching.Shared.Utilities.ObjectDictionaryConverter))]
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Application-specific metadata for queryable custom data
        /// Examples: { "verification": { "email_verified": true }, "trust_score": 0.85 }
        /// </summary>
        [JsonProperty(PropertyName = "metadata")]
        [System.Text.Json.Serialization.JsonConverter(typeof(EntityMatching.Shared.Utilities.ObjectDictionaryConverter))]
        public Dictionary<string, object>? Metadata { get; set; }

        // === Privacy and Searchability Settings ===

        /// <summary>
        /// Field-level visibility settings for this entity
        /// Controls which fields are searchable and visible to whom
        /// </summary>
        [JsonProperty(PropertyName = "privacySettings")]
        public FieldVisibilitySettings PrivacySettings { get; set; } = new FieldVisibilitySettings();

        /// <summary>
        /// Whether this entity is searchable at all
        /// If false, entity will not appear in any search results
        /// Master switch for entity discoverability
        /// </summary>
        [JsonProperty(PropertyName = "isSearchable")]
        public bool IsSearchable { get; set; } = true;

        // === Ownership ===

        /// <summary>
        /// The user account that owns this entity
        /// For Person: the user themselves or someone managing their profile
        /// For Job/Property/Product: the hiring manager/landlord/seller
        /// </summary>
        [JsonProperty(PropertyName = "ownedByUserId")]
        public string? OwnedByUserId { get; set; }

        /// <summary>
        /// Whether this entity was created via an invite system
        /// </summary>
        [JsonProperty(PropertyName = "createdViaInvite")]
        public bool CreatedViaInvite { get; set; } = false;

        /// <summary>
        /// Secure token that allows the invitee to edit this entity later
        /// </summary>
        [JsonProperty(PropertyName = "editToken")]
        public string? EditToken { get; set; }

        /// <summary>
        /// Email address of the person who can edit this entity
        /// </summary>
        [JsonProperty(PropertyName = "inviteeEmail")]
        public string? InviteeEmail { get; set; }

        /// <summary>
        /// Whether the invitee can edit this entity
        /// </summary>
        [JsonProperty(PropertyName = "inviteeCanEdit")]
        public bool InviteeCanEdit { get; set; } = true;

        /// <summary>
        /// The relationship label assigned by the owner
        /// Domain-agnostic - can be used for any context
        /// Examples: "My Partner", "Candidate for Senior Engineer", "Rental Property in Seattle"
        /// </summary>
        [JsonProperty(PropertyName = "relationshipLabel")]
        public string? RelationshipLabel { get; set; }

        // === Timestamps ===

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when this entity was last modified
        /// Used for embedding generation and change detection
        /// </summary>
        [JsonProperty(PropertyName = "lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [JsonProperty(PropertyName = "inviteCreatedAt")]
        public DateTime? InviteCreatedAt { get; set; }

        // === Helper Methods ===

        /// <summary>
        /// Generate a secure edit token for invitee entity editing
        /// </summary>
        public static string GenerateEditToken()
        {
            var bytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }

        /// <summary>
        /// Check if this entity is owned by another user (created via invite)
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsOwnedEntity => !string.IsNullOrEmpty(OwnedByUserId);

        /// <summary>
        /// Get display name with relationship label if owned entity
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string DisplayNameWithRelationship
        {
            get
            {
                if (IsOwnedEntity && !string.IsNullOrEmpty(RelationshipLabel))
                {
                    return $"{Name} ({RelationshipLabel})";
                }
                return Name;
            }
        }

        /// <summary>
        /// Check if a specific field is visible to a requesting user
        /// Enforces field-level privacy based on visibility settings
        /// </summary>
        /// <param name="fieldPath">JSON path to the field (e.g., "name", "attributes.skills")</param>
        /// <param name="requestingUserId">User ID of the requester (null = anonymous)</param>
        /// <returns>True if field is visible to requesting user, false otherwise</returns>
        public bool IsFieldVisibleToUser(string fieldPath, string? requestingUserId)
        {
            // If entity is not searchable, no fields are visible
            if (!IsSearchable)
            {
                return false;
            }

            // Get visibility level for this field
            var visibility = PrivacySettings.GetFieldVisibility(fieldPath);

            switch (visibility)
            {
                case FieldVisibility.Public:
                    // Public fields visible to everyone
                    return true;

                case FieldVisibility.Private:
                    // Private fields only visible to owner
                    // Both must be non-null AND equal
                    return !string.IsNullOrEmpty(requestingUserId) &&
                           !string.IsNullOrEmpty(OwnedByUserId) &&
                           requestingUserId == OwnedByUserId;

                case FieldVisibility.FriendsOnly:
                    // FriendsOnly: currently owner-only (friendship system not implemented)
                    // TODO: Check friendship status when friend system is implemented
                    return !string.IsNullOrEmpty(requestingUserId) &&
                           !string.IsNullOrEmpty(OwnedByUserId) &&
                           requestingUserId == OwnedByUserId;

                default:
                    // Fail-closed: unknown visibility = not visible
                    return false;
            }
        }

        /// <summary>
        /// Check if any field in a list is visible to a requesting user
        /// </summary>
        public bool AnyFieldVisibleToUser(IEnumerable<string> fieldPaths, string? requestingUserId)
        {
            if (fieldPaths == null) return false;
            return fieldPaths.Any(fp => IsFieldVisibleToUser(fp, requestingUserId));
        }

        /// <summary>
        /// Get attribute value by key with type safety
        /// </summary>
        public T? GetAttribute<T>(string key) where T : class
        {
            if (Attributes != null && Attributes.TryGetValue(key, out var value))
            {
                return value as T;
            }
            return null;
        }

        /// <summary>
        /// Get attribute value by key for value types
        /// </summary>
        public T GetAttributeValue<T>(string key, T defaultValue = default) where T : struct
        {
            if (Attributes != null && Attributes.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set attribute value
        /// </summary>
        public void SetAttribute(string key, object value)
        {
            if (Attributes == null)
                Attributes = new Dictionary<string, object>();

            Attributes[key] = value;
            LastModified = DateTime.UtcNow;
        }
    }
}
