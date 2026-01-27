using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class SocialPreferences
    {
        [JsonProperty(PropertyName = "socialBatteryLevel")]
        public int SocialBatteryLevel { get; set; } // 1-10 (how much social interaction they can handle)

        [JsonProperty(PropertyName = "preferredSocialEnergyLevel")]
        public int PreferredSocialEnergyLevel { get; set; } // 1-7 (1=Very Introverted, 7=Very Extroverted)

        [JsonProperty(PropertyName = "preferredConversationTopics")]
        public ICollection<string> PreferredConversationTopics { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "topicsToAvoid")]
        public ICollection<string> TopicsToAvoid { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "enjoysMeetingNewPeople")]
        public bool EnjoysMeetingNewPeople { get; set; }

        [JsonProperty(PropertyName = "prefersDeepConversations")]
        public bool PrefersDeepConversations { get; set; }

        [JsonProperty(PropertyName = "communicationStyle")]
        public ICollection<string> CommunicationStyle { get; set; } = new List<string>(); // Direct, Gentle, Humorous, etc.
    }
}
