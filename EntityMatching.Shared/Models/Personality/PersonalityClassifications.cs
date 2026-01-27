using Newtonsoft.Json;

namespace EntityMatching.Shared.Models
{
    public class PersonalityClassifications
    {
        [JsonProperty(PropertyName = "mbtiType")]
        public string MBTIType { get; set; } = "";

        [JsonProperty(PropertyName = "openness")]
        public int Openness { get; set; }

        [JsonProperty(PropertyName = "conscientiousness")]
        public int Conscientiousness { get; set; }

        [JsonProperty(PropertyName = "extraversion")]
        public int Extraversion { get; set; }

        [JsonProperty(PropertyName = "agreeableness")]
        public int Agreeableness { get; set; }

        [JsonProperty(PropertyName = "neuroticism")]
        public int Neuroticism { get; set; }

        [JsonProperty(PropertyName = "enneagramType")]
        public string EnneagramType { get; set; } = "";
    }
}
