using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class AdventurePreferences
    {
        [JsonProperty(PropertyName = "riskTolerance")]
        public int RiskTolerance { get; set; } // 1-10 (1 = very risk-averse, 10 = thrill-seeker)

        [JsonProperty(PropertyName = "noveltyPreference")]
        public int NoveltyPreference { get; set; } // 1-10 (1 = routine-loving, 10 = always wants new experiences)

        [JsonProperty(PropertyName = "enjoysSpontaneity")]
        public bool EnjoysSpontaneity { get; set; }

        [JsonProperty(PropertyName = "likesPlanning")]
        public bool LikesPlanning { get; set; }

        [JsonProperty(PropertyName = "adventureTypes")]
        public ICollection<string> AdventureTypes { get; set; } = new List<string>(); // Hiking, Skydiving, Travel, etc.

        [JsonProperty(PropertyName = "comfortActivities")]
        public ICollection<string> ComfortActivities { get; set; } = new List<string>(); // Reading, Cooking, Netflix, etc.

        [JsonProperty(PropertyName = "energyLevelPreference")]
        public int EnergyLevelPreference { get; set; } // 1-10 (1 = very low energy, 10 = high energy activities)

        [JsonProperty(PropertyName = "enjoysSpontaneousActivities")]
        public bool EnjoysSpontaneousActivities { get; set; }

        [JsonProperty(PropertyName = "prefersWellPlannedActivities")]
        public bool PrefersWellPlannedActivities { get; set; }

        [JsonProperty(PropertyName = "likesSurprises")]
        public bool LikesSurprises { get; set; }

        [JsonProperty(PropertyName = "enjoysLearningNewSkills")]
        public bool EnjoysLearningNewSkills { get; set; }
    }
}
