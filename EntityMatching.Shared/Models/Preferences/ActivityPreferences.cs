using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class ActivityPreferences
    {
        [JsonProperty(PropertyName = "favoriteOutdoorActivities")]
        public ICollection<string> FavoriteOutdoorActivities { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteIndoorActivities")]
        public ICollection<string> FavoriteIndoorActivities { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "preferredTimeOfDay")]
        public string PreferredTimeOfDay { get; set; } = "";

        [JsonProperty(PropertyName = "energyLevelPreference")]
        public int EnergyLevelPreference { get; set; } // 1-10

        [JsonProperty(PropertyName = "groupSizePreference")]
        public string GroupSizePreference { get; set; } = "";

        [JsonProperty(PropertyName = "seasonalPreferences")]
        public ICollection<string> SeasonalPreferences { get; set; } = new List<string>();
    }
}
