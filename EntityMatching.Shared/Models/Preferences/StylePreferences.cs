using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class StylePreferences
    {
        [JsonProperty(PropertyName = "favoriteColors")]
        public ICollection<string> FavoriteColors { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "fashionStyle")]
        public ICollection<string> FashionStyle { get; set; } = new List<string>(); // Casual, Formal, Boho, Minimalist, etc.

        [JsonProperty(PropertyName = "homeDecorStyle")]
        public ICollection<string> HomeDecorStyle { get; set; } = new List<string>(); // Modern, Rustic, Scandinavian, etc.

        [JsonProperty(PropertyName = "aestheticPreferences")]
        public ICollection<string> AestheticPreferences { get; set; } = new List<string>(); // Vintage, Industrial, Cozy, etc.

        [JsonProperty(PropertyName = "casualVsFormal")]
        public int CasualVsFormal { get; set; } = 0; // 0 = very casual, 10 = very formal

        [JsonProperty(PropertyName = "colorPreferences")]
        public int ColorPreferences { get; set; } = 0; // 0 = neutral/calm, 10 = bright/vibrant
    }
}
