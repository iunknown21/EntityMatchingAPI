using EntityMatching.Shared.Models;

namespace EntityMatching.Tests.Helpers
{
    /// <summary>
    /// Parametric profile generator for creating large-scale realistic test datasets
    /// Generates profiles with correlated, realistic attribute combinations
    /// </summary>
    public static class ProfileGenerator
    {
        private static readonly Random _random = new Random(42); // Seeded for reproducibility

        private static readonly string[] Cities = new[]
        {
            "Seattle, WA", "Portland, OR", "San Francisco, CA", "Los Angeles, CA",
            "New York, NY", "Austin, TX", "Denver, CO", "Miami, FL",
            "Chicago, IL", "Boston, MA"
        };

        private static readonly string[] FirstNames = new[]
        {
            "Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Avery", "Quinn",
            "Cameron", "Skyler", "Dakota", "Sage", "River", "Phoenix", "Robin", "Sam"
        };

        private static readonly string[] LastNames = new[]
        {
            "Smith", "Johnson", "Chen", "Patel", "Garcia", "Martinez", "Rodriguez",
            "Kim", "Lee", "Brown", "Davis", "Wilson", "Anderson", "Thomas", "Moore"
        };

        private static readonly string[] Allergens = new[]
        {
            "peanuts", "tree nuts", "shellfish", "dairy", "eggs", "soy", "wheat", "fish"
        };

        private static readonly string[] MovieGenres = new[]
        {
            "Action", "Comedy", "Drama", "Sci-Fi", "Thriller", "Horror", "Romance",
            "Documentary", "Adventure", "Fantasy", "Animation", "Mystery", "Independent"
        };

        private static readonly string[] MusicGenres = new[]
        {
            "Rock", "Pop", "Jazz", "Classical", "Hip-Hop", "Electronic", "Country",
            "Indie", "R&B", "Folk", "Metal", "Blues", "Reggae", "Alternative"
        };

        private static readonly string[] Hobbies = new[]
        {
            "Reading", "Gaming", "Cooking", "Photography", "Hiking", "Painting",
            "Gardening", "Traveling", "Dancing", "Writing", "Yoga", "Cycling",
            "Knitting", "Woodworking", "Running", "Swimming", "Rock climbing"
        };

        /// <summary>
        /// Generate a batch of realistic, diverse profiles
        /// </summary>
        public static List<PersonEntity> GenerateRealisticProfiles(int count, string? userId = null)
        {
            var profiles = new List<PersonEntity>();

            for (int i = 0; i < count; i++)
            {
                var profile = GenerateSingleProfile(i, userId);
                profiles.Add(profile);
            }

            return profiles;
        }

        private static PersonEntity GenerateSingleProfile(int seed, string? userId = null)
        {
            // Use seed to create variation while maintaining reproducibility
            var localRandom = new Random(42 + seed);

            var age = localRandom.Next(18, 75);
            var firstName = PickRandom(FirstNames, localRandom);
            var lastName = PickRandom(LastNames, localRandom);

            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = $"{firstName} {lastName}",
                OwnedByUserId = userId ?? $"user-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Description = GenerateBio(age, localRandom),
                ContactInformation = PickRandom(Cities, localRandom),
                Birthday = DateTime.UtcNow.AddYears(-age)
            };

            // Generate correlated preferences (realistic combinations)
            var riskTolerance = localRandom.Next(1, 11);
            var socialLevel = localRandom.Next(1, 11);
            var conscientiousness = localRandom.Next(1, 11);
            var extraversion = localRandom.Next(1, 11);

            // Adventure Preferences (correlated with extraversion and age)
            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = age > 50 ? Math.Min(riskTolerance, 6) : riskTolerance, // Older = lower risk
                NoveltyPreference = extraversion > 5 ? localRandom.Next(6, 11) : localRandom.Next(3, 8),
                EnjoysSpontaneity = extraversion > 6
            };

            // Social Preferences (correlated with personality)
            profile.SocialPreferences = new SocialPreferences
            {
                SocialBatteryLevel = socialLevel,
                EnjoysMeetingNewPeople = socialLevel > 6,
                PrefersDeepConversations = conscientiousness > 6
            };

            // Entertainment Preferences (correlated with age and personality)
            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = PickRandomMultiple(MovieGenres, localRandom.Next(2, 5), localRandom),
                FavoriteMusicGenres = PickRandomMultiple(MusicGenres, localRandom.Next(2, 4), localRandom),
                FavoriteBookGenres = PickRandomMultiple(new[] { "Fiction", "Non-fiction", "Biography", "Science Fiction", "Mystery", "Romance" }, localRandom.Next(1, 4), localRandom)
            };

            // Learning Preferences
            profile.LearningPreferences = new LearningPreferences
            {
                SubjectsOfInterest = PickRandomMultiple(new[] { "Technology", "History", "Art", "Science", "Philosophy", "Business", "Psychology" }, localRandom.Next(2, 5), localRandom),
                LearningStyles = PickRandomMultiple(new[] { "Visual", "Hands-on", "Reading", "Auditory", "Online courses" }, localRandom.Next(1, 3), localRandom)
            };

            // Dietary Restrictions (15% have allergies, 30% have dietary restrictions)
            var hasAllergies = localRandom.NextDouble() < 0.15;
            var hasDietaryRestrictions = localRandom.NextDouble() < 0.30;

            if (hasAllergies || hasDietaryRestrictions)
            {
                profile.DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = hasAllergies ? PickRandomMultiple(Allergens, localRandom.Next(1, 3), localRandom) : new List<string>(),
                    Restrictions = hasDietaryRestrictions ? PickRandomMultiple(new[] { "Vegetarian", "Vegan", "Gluten-Free", "Kosher", "Halal", "Low-carb" }, localRandom.Next(1, 3), localRandom) : new List<string>()
                };
            }

            // Accessibility Needs (10% have accessibility requirements)
            var needsAccessibility = localRandom.NextDouble() < 0.10;
            if (needsAccessibility)
            {
                profile.AccessibilityNeeds = new AccessibilityNeeds
                {
                    RequiresWheelchairAccess = localRandom.NextDouble() < 0.4,
                    HasLimitedMobility = localRandom.NextDouble() < 0.3,
                    RequiresHearingAssistance = localRandom.NextDouble() < 0.2,
                    RequiresSignLanguageInterpreter = localRandom.NextDouble() < 0.1,
                    RequiresLargeText = localRandom.NextDouble() < 0.2
                };
            }

            // Sensory Preferences (5% have sensory sensitivities)
            var hasSensorySensitivity = localRandom.NextDouble() < 0.05;
            if (hasSensorySensitivity)
            {
                profile.SensoryPreferences = new SensoryPreferences
                {
                    SensitiveToFlashingLights = localRandom.NextDouble() < 0.3,
                    NoiseToleranceLevel = localRandom.Next(1, 4), // Low tolerance (1-3)
                    Claustrophobic = localRandom.NextDouble() < 0.2,
                    CrowdSensitivity = localRandom.Next(1, 4), // Sensitive to crowds (1-3)
                    PrefersQuietEnvironments = localRandom.NextDouble() < 0.6
                };
            }

            // Activity Preferences
            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = age > 50 ? "Morning" : extraversion > 6 ? "Evening" : "Afternoon",
                EnergyLevelPreference = age > 60 ? localRandom.Next(3, 7) : localRandom.Next(5, 10),
                GroupSizePreference = socialLevel > 7 ? "Large groups" : "Small groups"
            };

            // Nature Preferences
            profile.NaturePreferences = new NaturePreferences
            {
                PreferredSeasons = PickRandomMultiple(new[] { "Spring", "Summer", "Fall", "Winter" }, localRandom.Next(1, 3), localRandom),
                FavoriteWeatherTypes = PickRandomMultiple(new[] { "Sunny", "Rainy", "Snowy", "Cloudy", "Windy" }, localRandom.Next(1, 3), localRandom),
                HasPets = localRandom.NextDouble() < 0.4,
                PetTypes = localRandom.NextDouble() < 0.4 ? PickRandomMultiple(new[] { "Dog", "Cat", "Bird", "Fish" }, localRandom.Next(1, 2), localRandom) : new List<string>(),
                EnjoysGardening = localRandom.NextDouble() < 0.3,
                EnjoysBirdWatching = localRandom.NextDouble() < 0.15
            };

            // Style Preferences
            profile.StylePreferences = new StylePreferences
            {
                FavoriteColors = PickRandomMultiple(new[] { "Blue", "Green", "Red", "Black", "White", "Purple", "Yellow", "Orange" }, localRandom.Next(2, 4), localRandom),
                FashionStyle = PickRandomMultiple(new[] { "Casual", "Formal", "Vintage", "Modern", "Bohemian", "Athletic" }, localRandom.Next(1, 3), localRandom),
                HomeDecorStyle = PickRandomMultiple(new[] { "Modern", "Traditional", "Minimalist", "Rustic", "Industrial", "Scandinavian" }, localRandom.Next(1, 2), localRandom),
                CasualVsFormal = conscientiousness > 7 ? localRandom.Next(3, 6) : localRandom.Next(6, 10),
                ColorPreferences = localRandom.Next(1, 11)
            };

            // Gift Preferences
            profile.GiftPreferences = new GiftPreferences
            {
                MeaningfulGiftTypes = PickRandomMultiple(new[] { "Books", "Experiences", "Handmade items", "Tech gadgets", "Jewelry", "Art" }, localRandom.Next(2, 4), localRandom),
                PreferredGiftStyle = conscientiousness > 6 ? "Thoughtful and personal" : "Practical",
                LikesSurprises = extraversion > 5,
                PrefersChoosing = conscientiousness > 7,
                CollectsOrHobbies = PickRandomMultiple(Hobbies, localRandom.Next(2, 5), localRandom)
            };

            // Personality Classifications
            profile.PersonalityClassifications = new PersonalityClassifications
            {
                MBTIType = GenerateMBTI(extraversion, conscientiousness, localRandom),
                EnneagramType = $"{localRandom.Next(1, 10)}w{localRandom.Next(1, 10)}",
                Openness = localRandom.Next(1, 11),
                Conscientiousness = conscientiousness,
                Extraversion = extraversion,
                Agreeableness = localRandom.Next(1, 11),
                Neuroticism = localRandom.Next(1, 11)
            };

            // Love Languages
            profile.LoveLanguages = new LoveLanguages
            {
                QualityTime = localRandom.Next(1, 11),
                WordsOfAffirmation = localRandom.Next(1, 11),
                PhysicalTouch = localRandom.Next(1, 11),
                ActsOfService = localRandom.Next(1, 11),
                ReceivingGifts = localRandom.Next(1, 11)
            };

            return profile;
        }

        private static string GenerateBio(int age, Random random)
        {
            var templates = new[]
            {
                "Passionate about {0} and {1}. Enjoys {2} in my free time.",
                "Love {0} and meeting new people. Always up for {1} and {2}.",
                "{0} enthusiast who enjoys {1}. Looking for opportunities to explore {2}.",
                "Creative soul who loves {0} and {1}. {2} is my favorite way to unwind.",
                "Adventurous spirit interested in {0}, {1}, and {2}."
            };

            var hobbies = PickRandomMultiple(Hobbies, 3, random);
            return string.Format(PickRandom(templates, random), hobbies[0].ToLower(), hobbies[1].ToLower(), hobbies[2].ToLower());
        }

        private static string GenerateMBTI(int extraversion, int conscientiousness, Random random)
        {
            var e_i = extraversion > 5 ? "E" : "I";
            var s_n = random.Next(2) == 0 ? "S" : "N";
            var t_f = random.Next(2) == 0 ? "T" : "F";
            var j_p = conscientiousness > 5 ? "J" : "P";
            return $"{e_i}{s_n}{t_f}{j_p}";
        }

        private static T PickRandom<T>(T[] array, Random? random = null)
        {
            var rng = random ?? _random;
            return array[rng.Next(array.Length)];
        }

        private static List<T> PickRandomMultiple<T>(T[] array, int count, Random? random = null)
        {
            var rng = random ?? _random;
            return array.OrderBy(x => rng.Next()).Take(Math.Min(count, array.Length)).ToList();
        }
    }
}
