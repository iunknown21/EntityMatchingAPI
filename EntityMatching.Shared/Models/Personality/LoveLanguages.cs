using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace EntityMatching.Shared.Models
{
    public class LoveLanguages
    {
        [JsonProperty(PropertyName = "wordsOfAffirmation")]
        public int WordsOfAffirmation { get; set; }

        [JsonProperty(PropertyName = "actsOfService")]
        public int ActsOfService { get; set; }

        [JsonProperty(PropertyName = "receivingGifts")]
        public int ReceivingGifts { get; set; }

        [JsonProperty(PropertyName = "qualityTime")]
        public int QualityTime { get; set; }

        [JsonProperty(PropertyName = "physicalTouch")]
        public int PhysicalTouch { get; set; }

        [JsonProperty(PropertyName = "notes")]
        public string Notes { get; set; } = "";

        [JsonProperty(PropertyName = "primary")]
        public string Primary { get; set; } = "";

        [JsonProperty(PropertyName = "secondary")]
        public string Secondary { get; set; } = "";

        public override string ToString()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(Primary))
                parts.Add($"Primary: {Primary}");

            if (!string.IsNullOrEmpty(Secondary))
                parts.Add($"Secondary: {Secondary}");

            // Score breakdown
            var scores = new List<string>();
            if (WordsOfAffirmation > 0) scores.Add($"Words of Affirmation: {WordsOfAffirmation}/10");
            if (ActsOfService > 0) scores.Add($"Acts of Service: {ActsOfService}/10");
            if (ReceivingGifts > 0) scores.Add($"Receiving Gifts: {ReceivingGifts}/10");
            if (QualityTime > 0) scores.Add($"Quality Time: {QualityTime}/10");
            if (PhysicalTouch > 0) scores.Add($"Physical Touch: {PhysicalTouch}/10");

            if (scores.Any())
                parts.Add(string.Join(", ", scores));

            if (!string.IsNullOrEmpty(Notes))
                parts.Add($"Notes: {Notes}");

            return parts.Any() ? string.Join(" | ", parts) : "Love languages data available";
        }
    }
}
