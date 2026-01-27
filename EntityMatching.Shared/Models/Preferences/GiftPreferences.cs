using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class GiftPreferences
    {
        [JsonProperty(PropertyName = "meaningfulGiftTypes")]
        public ICollection<string> MeaningfulGiftTypes { get; set; } = new List<string>(); // Handmade, Experiences, Practical, etc.

        [JsonProperty(PropertyName = "likesSurprises")]
        public bool LikesSurprises { get; set; }

        [JsonProperty(PropertyName = "prefersChoosing")]
        public bool PrefersChoosing { get; set; }

        [JsonProperty(PropertyName = "collectsOrHobbies")]
        public ICollection<string> CollectsOrHobbies { get; set; } = new List<string>(); // Books, Vinyl, Succulents, etc.

        [JsonProperty(PropertyName = "preferredGiftStyle")]
        public string PreferredGiftStyle { get; set; } = ""; // Small/thoughtful, Medium, Large/extravagant
    }
}
