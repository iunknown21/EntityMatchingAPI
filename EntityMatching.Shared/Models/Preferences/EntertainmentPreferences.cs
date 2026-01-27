using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Entertainment preferences including movies, music, books, and games
    /// Simplified version for MVP - extensible pattern can be added later
    /// </summary>
    public class EntertainmentPreferences
    {
        // Movies
        [JsonProperty(PropertyName = "favoriteMovieGenres")]
        public ICollection<string> FavoriteMovieGenres { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteActors")]
        public ICollection<string> FavoriteActors { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteDirectors")]
        public ICollection<string> FavoriteDirectors { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "enjoysForeignFilms")]
        public bool EnjoysForeignFilms { get; set; }

        [JsonProperty(PropertyName = "prefersCinemaOverStreaming")]
        public bool PrefersCinemaOverStreaming { get; set; }

        // Music
        [JsonProperty(PropertyName = "favoriteMusicGenres")]
        public ICollection<string> FavoriteMusicGenres { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteMusicians")]
        public ICollection<string> FavoriteMusicians { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteBands")]
        public ICollection<string> FavoriteBands { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "musicMoodPreferences")]
        public ICollection<string> MusicMoodPreferences { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "concertVenuePreferences")]
        public ICollection<string> ConcertVenuePreferences { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "liveMusicFrequency")]
        public string LiveMusicFrequency { get; set; } = "";

        [JsonProperty(PropertyName = "musicDiscoveryOpenness")]
        public int MusicDiscoveryOpenness { get; set; } = 5; // 1-10 scale

        // Books
        [JsonProperty(PropertyName = "favoriteBookGenres")]
        public ICollection<string> FavoriteBookGenres { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "favoriteAuthors")]
        public ICollection<string> FavoriteAuthors { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "prefersPhysicalBooks")]
        public bool PrefersPhysicalBooks { get; set; }

        // Games
        [JsonProperty(PropertyName = "favoriteGameTypes")]
        public ICollection<string> FavoriteGameTypes { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "enjoysPuzzles")]
        public bool EnjoysPuzzles { get; set; }

        [JsonProperty(PropertyName = "gameplayPreference")]
        public string GameplayPreference { get; set; } = ""; // e.g., "cooperative", "competitive", "solo"

        // General
        [JsonProperty(PropertyName = "entertainmentEnergyLevel")]
        public int EntertainmentEnergyLevel { get; set; } = 5; // 1-10 scale

        [JsonProperty(PropertyName = "culturalInterestLevel")]
        public int CulturalInterestLevel { get; set; } = 5; // 1-10 scale
    }
}
