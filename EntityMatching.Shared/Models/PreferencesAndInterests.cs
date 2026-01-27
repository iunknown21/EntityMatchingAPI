using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class PreferencesAndInterests
    {
        [JsonProperty(PropertyName = "dietaryRestrictions")]
        public ICollection<string> DietaryRestrictions { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "allergies")]
        public ICollection<string> Allergies { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteCuisines")]
        public ICollection<string> FavoriteCuisines { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteMusic")]
        public ICollection<string> FavoriteMusic { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteEntertainment")]
        public ICollection<string> FavoriteEntertainment { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "hobbies")]
        public ICollection<string> Hobbies { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "categoryPreferences")]
        public Dictionary<string, List<string>> CategoryPreferences { get; set; } = new Dictionary<string, List<string>>();

        [JsonProperty(PropertyName = "socialEnergy")]
        public int SocialEnergy { get; set; } = 0; // 0-10 scale, 0 = prefer intimate settings, 10 = love large gatherings
    }
}
