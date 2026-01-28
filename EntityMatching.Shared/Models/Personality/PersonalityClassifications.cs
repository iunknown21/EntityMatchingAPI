using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

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

        public override string ToString()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(MBTIType))
                parts.Add($"MBTI: {MBTIType}");

            if (!string.IsNullOrEmpty(EnneagramType))
                parts.Add($"Enneagram: {EnneagramType}");

            // Big Five traits
            var traits = new List<string>();
            if (Openness > 0) traits.Add($"Openness: {Openness}/10");
            if (Conscientiousness > 0) traits.Add($"Conscientiousness: {Conscientiousness}/10");
            if (Extraversion > 0) traits.Add($"Extraversion: {Extraversion}/10");
            if (Agreeableness > 0) traits.Add($"Agreeableness: {Agreeableness}/10");
            if (Neuroticism > 0) traits.Add($"Neuroticism: {Neuroticism}/10");

            if (traits.Any())
                parts.Add(string.Join(", ", traits));

            return parts.Any() ? string.Join(" | ", parts) : "Personality data available";
        }
    }
}
