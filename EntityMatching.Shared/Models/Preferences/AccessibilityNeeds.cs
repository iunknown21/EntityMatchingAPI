using Newtonsoft.Json;

namespace EntityMatching.Shared.Models
{
    public class AccessibilityNeeds
    {
        [JsonProperty(PropertyName = "requiresWheelchairAccess")]
        public bool RequiresWheelchairAccess { get; set; }

        [JsonProperty(PropertyName = "requiresHearingAssistance")]
        public bool RequiresHearingAssistance { get; set; }

        [JsonProperty(PropertyName = "hasLimitedMobility")]
        public bool HasLimitedMobility { get; set; }

        [JsonProperty(PropertyName = "requiresSignLanguageInterpreter")]
        public bool RequiresSignLanguageInterpreter { get; set; }

        [JsonProperty(PropertyName = "requiresLargeText")]
        public bool RequiresLargeText { get; set; }

        [JsonProperty(PropertyName = "specialConsiderations")]
        public string SpecialConsiderations { get; set; } = "";
    }
}
