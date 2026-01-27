using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class NaturePreferences
    {
        [JsonProperty(PropertyName = "favoriteAnimals")]
        public ICollection<string> FavoriteAnimals { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "hasPets")]
        public bool HasPets { get; set; }

        [JsonProperty(PropertyName = "petTypes")]
        public ICollection<string> PetTypes { get; set; } = new List<string>(); // Dogs, Cats, Birds, etc.

        [JsonProperty(PropertyName = "favoriteFlowers")]
        public ICollection<string> FavoriteFlowers { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "preferredSeasons")]
        public ICollection<string> PreferredSeasons { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteWeatherTypes")]
        public ICollection<string> FavoriteWeatherTypes { get; set; } = new List<string>(); // Sunny, Rainy, Snowy, etc.

        [JsonProperty(PropertyName = "enjoysBirdWatching")]
        public bool EnjoysBirdWatching { get; set; }

        [JsonProperty(PropertyName = "enjoysGardening")]
        public bool EnjoysGardening { get; set; }
    }
}
