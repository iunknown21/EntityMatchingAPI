using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class ExperiencePreferences
    {
        [JsonProperty(PropertyName = "comfortZoneActivities")]
        public ICollection<string> ComfortZoneActivities { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "bucketListActivities")]
        public ICollection<string> BucketListActivities { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "energyLevelPreference")]
        public int EnergyLevelPreference { get; set; }

        [JsonProperty(PropertyName = "preferredTimeOfDay")]
        public string PreferredTimeOfDay { get; set; } = "";

        [JsonProperty(PropertyName = "indoorVsOutdoorPreference")]
        public int IndoorVsOutdoorPreference { get; set; }

        [JsonProperty(PropertyName = "crowdTolerance")]
        public int CrowdTolerance { get; set; }

        [JsonProperty(PropertyName = "budgetPreference")]
        public string BudgetPreference { get; set; } = "";

        [JsonProperty(PropertyName = "adventureLevel")]
        public int AdventureLevel { get; set; } = 0; // 0-10 scale, 0 = prefer familiar, 10 = love new adventures

        [JsonProperty(PropertyName = "planningStyle")]
        public int PlanningStyle { get; set; } = 0; // 0-10 scale, 0 = spontaneous, 10 = well-planned
    }
}
