using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Strongly-typed entity for person profiles
    /// Includes all person-specific preferences and personality data
    /// Can represent: job seekers, dating profiles, customers, etc.
    /// </summary>
    public class PersonEntity : Entity
    {
        public PersonEntity()
        {
            EntityType = EntityType.Person;
        }

        // === Person-Specific Fields ===

        [JsonProperty(PropertyName = "contactInformation")]
        public string ContactInformation { get; set; } = "";

        [JsonProperty(PropertyName = "birthday")]
        public DateTime? Birthday { get; set; }

        [JsonProperty(PropertyName = "profileImages")]
        public ICollection<ProfileImage> ProfileImages { get; set; } = new List<ProfileImage>();

        [JsonProperty(PropertyName = "importantDates")]
        public ICollection<ImportantDate> ImportantDates { get; set; } = new List<ImportantDate>();

        [JsonProperty(PropertyName = "loveLanguages")]
        public LoveLanguages LoveLanguages { get; set; } = new LoveLanguages();

        [JsonProperty(PropertyName = "personalityClassifications")]
        public PersonalityClassifications PersonalityClassifications { get; set; } = new PersonalityClassifications();

        [JsonProperty(PropertyName = "preferences")]
        public PreferencesAndInterests Preferences { get; set; } = new PreferencesAndInterests();

        [JsonProperty(PropertyName = "experiencePreferences")]
        public ExperiencePreferences ExperiencePreferences { get; set; } = new ExperiencePreferences();

        // === 8+ Dimensional Preference Categories ===

        [JsonProperty(PropertyName = "entertainmentPreferences")]
        public EntertainmentPreferences EntertainmentPreferences { get; set; } = new EntertainmentPreferences();

        [JsonProperty(PropertyName = "stylePreferences")]
        public StylePreferences StylePreferences { get; set; } = new StylePreferences();

        [JsonProperty(PropertyName = "naturePreferences")]
        public NaturePreferences NaturePreferences { get; set; } = new NaturePreferences();

        [JsonProperty(PropertyName = "socialPreferences")]
        public SocialPreferences SocialPreferences { get; set; } = new SocialPreferences();

        [JsonProperty(PropertyName = "sensoryPreferences")]
        public SensoryPreferences SensoryPreferences { get; set; } = new SensoryPreferences();

        [JsonProperty(PropertyName = "adventurePreferences")]
        public AdventurePreferences AdventurePreferences { get; set; } = new AdventurePreferences();

        [JsonProperty(PropertyName = "learningPreferences")]
        public LearningPreferences LearningPreferences { get; set; } = new LearningPreferences();

        [JsonProperty(PropertyName = "giftPreferences")]
        public GiftPreferences GiftPreferences { get; set; } = new GiftPreferences();

        [JsonProperty(PropertyName = "accessibilityNeeds")]
        public AccessibilityNeeds AccessibilityNeeds { get; set; } = new AccessibilityNeeds();

        [JsonProperty(PropertyName = "dietaryRestrictions")]
        public DietaryRestrictions DietaryRestrictions { get; set; } = new DietaryRestrictions();

        [JsonProperty(PropertyName = "activityPreferences")]
        public ActivityPreferences ActivityPreferences { get; set; } = new ActivityPreferences();

        /// <summary>
        /// User's consent to provide sensitive health and accessibility information
        /// Required for GDPR/CCPA compliance when collecting health data
        /// </summary>
        [JsonProperty(PropertyName = "sensitiveDataConsent")]
        public bool SensitiveDataConsent { get; set; } = false;

        // === Computed Properties ===

        /// <summary>
        /// Gets the default profile image URL or null if none exists
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string? DefaultProfileImageUrl => ProfileImages?.FirstOrDefault(img => img.IsDefault)?.ImageUrl;

        /// <summary>
        /// Gets all profile image URLs
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public IEnumerable<string> ProfileImageUrls => ProfileImages?.Select(img => img.ImageUrl) ?? Enumerable.Empty<string>();

        /// <summary>
        /// Gets the calculated age based on birthday
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int? Age
        {
            get
            {
                if (Birthday == null) return null;
                var today = DateTime.Today;
                var age = today.Year - Birthday.Value.Year;
                if (Birthday.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        /// <summary>
        /// Gets the location from contact information or returns a default
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string Location
        {
            get
            {
                if (!string.IsNullOrEmpty(ContactInformation))
                {
                    // Try to extract location from contact information
                    return ContactInformation.Split('\n', ',').FirstOrDefault()?.Trim() ?? "Unknown";
                }
                return "Unknown";
            }
        }

        /// <summary>
        /// Sync strongly-typed properties to the base Attributes dictionary
        /// Call this before saving to ensure search filters can access person-specific fields
        /// </summary>
        public void SyncToAttributes()
        {
            if (Birthday.HasValue)
                SetAttribute("birthday", Birthday.Value);

            if (Age.HasValue)
                SetAttribute("age", Age.Value);

            SetAttribute("location", Location);

            if (LoveLanguages != null)
                SetAttribute("loveLanguages", LoveLanguages);

            if (PersonalityClassifications != null)
                SetAttribute("personalityClassifications", PersonalityClassifications);

            if (EntertainmentPreferences != null)
                SetAttribute("entertainmentPreferences", EntertainmentPreferences);

            if (StylePreferences != null)
                SetAttribute("stylePreferences", StylePreferences);

            if (NaturePreferences != null)
                SetAttribute("naturePreferences", NaturePreferences);

            if (SocialPreferences != null)
                SetAttribute("socialPreferences", SocialPreferences);

            if (SensoryPreferences != null)
                SetAttribute("sensoryPreferences", SensoryPreferences);

            if (AdventurePreferences != null)
                SetAttribute("adventurePreferences", AdventurePreferences);

            if (LearningPreferences != null)
                SetAttribute("learningPreferences", LearningPreferences);

            if (GiftPreferences != null)
                SetAttribute("giftPreferences", GiftPreferences);

            if (AccessibilityNeeds != null)
                SetAttribute("accessibilityNeeds", AccessibilityNeeds);

            if (DietaryRestrictions != null)
                SetAttribute("dietaryRestrictions", DietaryRestrictions);

            if (ActivityPreferences != null)
                SetAttribute("activityPreferences", ActivityPreferences);
        }
    }
}
