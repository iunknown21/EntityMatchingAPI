using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Represents sensory preferences and sensitivities for nuanced matching
    /// Simplified version for MVP - complex scoring methods removed
    /// </summary>
    public class SensoryPreferences
    {
        // Sound preferences
        [JsonProperty(PropertyName = "noiseToleranceLevel")]
        public int NoiseToleranceLevel { get; set; } = 5; // 1-10 (1=very sensitive, 10=high tolerance)

        [JsonProperty(PropertyName = "favoriteSounds")]
        public ICollection<string> FavoriteSounds { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "soundsToAvoid")]
        public ICollection<string> SoundsToAvoid { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "prefersQuietEnvironments")]
        public bool PrefersQuietEnvironments { get; set; }

        [JsonProperty(PropertyName = "enjoysLiveMusic")]
        public bool EnjoysLiveMusic { get; set; } = true;

        [JsonProperty(PropertyName = "musicVolumePreference")]
        public string MusicVolumePreference { get; set; } = ""; // "Soft", "Moderate", "Loud"

        // Smell preferences
        [JsonProperty(PropertyName = "favoriteScents")]
        public ICollection<string> FavoriteScents { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "scentsToAvoid")]
        public ICollection<string> ScentsToAvoid { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "scentSensitivityLevel")]
        public int ScentSensitivityLevel { get; set; } = 5; // 1-10 (1=very sensitive, 10=not sensitive)

        [JsonProperty(PropertyName = "enjoysCandles")]
        public bool EnjoysCandles { get; set; }

        [JsonProperty(PropertyName = "enjoysIncense")]
        public bool EnjoysIncense { get; set; }

        // Texture preferences
        [JsonProperty(PropertyName = "comfortTextures")]
        public ICollection<string> ComfortTextures { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "textureDislikes")]
        public ICollection<string> TextureDislikes { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "textureSensitivityLevel")]
        public int TextureSensitivityLevel { get; set; } = 5; // 1-10 (1=very sensitive, 10=not sensitive)

        [JsonProperty(PropertyName = "preferredFabrics")]
        public ICollection<string> PreferredFabrics { get; set; } = new List<string>();

        // Light sensitivity
        [JsonProperty(PropertyName = "lightSensitivityLevel")]
        public int LightSensitivityLevel { get; set; } = 5; // 1-10 (1=very sensitive, 10=not sensitive)

        [JsonProperty(PropertyName = "prefersDimLighting")]
        public bool PrefersDimLighting { get; set; }

        [JsonProperty(PropertyName = "sensitiveToFlashingLights")]
        public bool SensitiveToFlashingLights { get; set; }

        [JsonProperty(PropertyName = "prefersNaturalLight")]
        public bool PrefersNaturalLight { get; set; }

        [JsonProperty(PropertyName = "preferredLightingTypes")]
        public ICollection<string> PreferredLightingTypes { get; set; } = new List<string>();

        // Temperature preferences
        [JsonProperty(PropertyName = "temperatureSensitivity")]
        public int TemperatureSensitivity { get; set; } = 5; // 1-10 (1=very sensitive, 10=not sensitive)

        [JsonProperty(PropertyName = "prefersWarmEnvironments")]
        public bool PrefersWarmEnvironments { get; set; }

        [JsonProperty(PropertyName = "prefersCoolEnvironments")]
        public bool PrefersCoolEnvironments { get; set; }

        [JsonProperty(PropertyName = "idealTemperatureRange")]
        public string IdealTemperatureRange { get; set; } = "";

        // Crowd and space preferences
        [JsonProperty(PropertyName = "crowdSensitivity")]
        public int CrowdSensitivity { get; set; } = 5; // 1-10 (1=very sensitive, 10=thrives in crowds)

        [JsonProperty(PropertyName = "needsPersonalSpace")]
        public bool NeedsPersonalSpace { get; set; }

        [JsonProperty(PropertyName = "claustrophobic")]
        public bool Claustrophobic { get; set; }

        [JsonProperty(PropertyName = "prefersOpenSpaces")]
        public bool PrefersOpenSpaces { get; set; }

        // General sensory processing
        [JsonProperty(PropertyName = "overallSensoryProcessing")]
        public string OverallSensoryProcessing { get; set; } = ""; // "Highly Sensitive", "Average", "Low Sensitivity"

        [JsonProperty(PropertyName = "sensoryOverloadTriggers")]
        public ICollection<string> SensoryOverloadTriggers { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "calmingActivities")]
        public ICollection<string> CalmingActivities { get; set; } = new List<string>();
    }
}
