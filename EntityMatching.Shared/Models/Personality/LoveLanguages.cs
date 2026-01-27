using Newtonsoft.Json;

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
    }
}
