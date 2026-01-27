using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;

namespace EntityMatching.Tests.Helpers
{
    /// <summary>
    /// Factory class for creating test data objects
    /// Makes it easy to create consistent test data across all tests
    /// </summary>
    public static class TestDataFactory
    {
        #region Profiles

        public static PersonEntity CreateMinimalProfile(string? userId = null, string? name = null)
        {
            return new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = name ?? "Test PersonEntity",
                OwnedByUserId = userId ?? $"user-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
        }

        public static PersonEntity CreateCompleteProfile(string? userId = null)
        {
            var profile = CreateMinimalProfile(userId, "Comprehensive Test PersonEntity");

            profile.Description = "A comprehensive test profile with all fields populated";
            profile.ContactInformation = "Seattle, WA";
            profile.Birthday = DateTime.UtcNow.AddYears(-30);

            profile.EntertainmentPreferences = CreateEntertainmentPreferences();
            profile.AdventurePreferences = CreateAdventurePreferences();
            profile.LearningPreferences = CreateLearningPreferences();
            profile.SensoryPreferences = CreateSensoryPreferences();
            profile.SocialPreferences = CreateSocialPreferences();
            profile.StylePreferences = CreateStylePreferences();
            profile.NaturePreferences = CreateNaturePreferences();
            profile.GiftPreferences = CreateGiftPreferences();
            profile.ActivityPreferences = CreateActivityPreferences();
            profile.AccessibilityNeeds = CreateAccessibilityNeeds();
            profile.DietaryRestrictions = CreateDietaryRestrictions();
            profile.LoveLanguages = CreateLoveLanguages();
            profile.PersonalityClassifications = CreatePersonalityClassifications();

            return profile;
        }

        public static PersonEntity CreateOutdoorAdventureProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? "Outdoor Adventure Enthusiast");
            profile.Description = "Loves hiking, camping, and outdoor adventures. Enjoys nature and physical activities.";
            profile.ContactInformation = "Portland, OR";
            profile.Birthday = DateTime.UtcNow.AddYears(-28);

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Adventure", "Documentary", "Action" },
                FavoriteMusicGenres = new List<string> { "Folk", "Rock", "Indie" },
                FavoriteBookGenres = new List<string> { "Adventure", "Travel", "Survival" }
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 9,
                NoveltyPreference = 8,
                EnjoysSpontaneity = true
            };

            profile.LearningPreferences = new LearningPreferences
            {
                SubjectsOfInterest = new List<string> { "Geography", "Wildlife", "Outdoor Skills" },
                LearningStyles = new List<string> { "Hands-on", "Visual" }
            };

            profile.NaturePreferences = new NaturePreferences
            {
                PreferredSeasons = new List<string> { "Summer", "Fall" },
                FavoriteWeatherTypes = new List<string> { "Sunny", "Clear skies" },
                HasPets = true,
                PetTypes = new List<string> { "Dog" },
                EnjoysGardening = false,
                EnjoysBirdWatching = true
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Morning",
                EnergyLevelPreference = 9,
                GroupSizePreference = "Small groups"
            };

            profile.PersonalityClassifications = new PersonalityClassifications
            {
                MBTIType = "ESTP",
                EnneagramType = "7w8",
                Openness = 9,
                Conscientiousness = 6,
                Extraversion = 8,
                Agreeableness = 7,
                Neuroticism = 3
            };

            return profile;
        }

        public static PersonEntity CreateArtisticIntrovertProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? "Creative Introvert");
            profile.Description = "Passionate about art, music, and creative expression. Enjoys quiet time for deep work and reflection.";
            profile.ContactInformation = "Brooklyn, NY";
            profile.Birthday = DateTime.UtcNow.AddYears(-26);

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Drama", "Independent", "Foreign" },
                FavoriteMusicGenres = new List<string> { "Classical", "Jazz", "Ambient" },
                FavoriteBookGenres = new List<string> { "Literature", "Poetry", "Art History" }
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 3,
                NoveltyPreference = 6,
                EnjoysSpontaneity = false
            };

            profile.LearningPreferences = new LearningPreferences
            {
                SubjectsOfInterest = new List<string> { "Art", "Philosophy", "Music Theory" },
                LearningStyles = new List<string> { "Visual", "Reading" }
            };

            profile.StylePreferences = new StylePreferences
            {
                FavoriteColors = new List<string> { "Black", "White", "Burgundy" },
                FashionStyle = new List<string> { "Vintage", "Minimalist" },
                HomeDecorStyle = new List<string> { "Bohemian", "Eclectic" },
                AestheticPreferences = new List<string> { "Artistic", "Textured surfaces" },
                CasualVsFormal = 4,
                ColorPreferences = 8
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Evening",
                EnergyLevelPreference = 5,
                GroupSizePreference = "One-on-one"
            };

            profile.PersonalityClassifications = new PersonalityClassifications
            {
                MBTIType = "INFP",
                EnneagramType = "4w5",
                Openness = 9,
                Conscientiousness = 5,
                Extraversion = 2,
                Agreeableness = 8,
                Neuroticism = 6
            };

            return profile;
        }

        public static PersonEntity CreateTechEnthusiastProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? "Tech Enthusiast");
            profile.Description = "Software engineer and tech enthusiast. Loves coding, gaming, and staying up-to-date with latest technology.";
            profile.ContactInformation = "San Francisco, CA";
            profile.Birthday = DateTime.UtcNow.AddYears(-29);

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Sci-Fi", "Thriller", "Documentary" },
                FavoriteMusicGenres = new List<string> { "Electronic", "Synthwave", "Lo-fi" },
                FavoriteBookGenres = new List<string> { "Science Fiction", "Technology", "Business" }
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 6,
                NoveltyPreference = 8,
                EnjoysSpontaneity = true
            };

            profile.LearningPreferences = new LearningPreferences
            {
                SubjectsOfInterest = new List<string> { "Technology", "Programming", "AI", "Robotics" },
                LearningStyles = new List<string> { "Hands-on", "Online courses" }
            };

            profile.StylePreferences = new StylePreferences
            {
                FavoriteColors = new List<string> { "Blue", "Black", "Gray" },
                FashionStyle = new List<string> { "Casual", "Tech wear" },
                HomeDecorStyle = new List<string> { "Modern", "Minimalist" },
                AestheticPreferences = new List<string> { "Clean lines", "High-tech" },
                CasualVsFormal = 8,
                ColorPreferences = 5
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Night",
                EnergyLevelPreference = 7,
                GroupSizePreference = "Small groups"
            };

            profile.PersonalityClassifications = new PersonalityClassifications
            {
                MBTIType = "INTP",
                EnneagramType = "5w6",
                Openness = 9,
                Conscientiousness = 7,
                Extraversion = 4,
                Agreeableness = 6,
                Neuroticism = 4
            };

            return profile;
        }

        public static PersonEntity CreateSocialButterflyProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? "Social Butterfly");
            profile.Description = "Loves meeting new people, hosting events, and being the life of the party. Energized by social interactions.";
            profile.ContactInformation = "Miami, FL";
            profile.Birthday = DateTime.UtcNow.AddYears(-27);

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Comedy", "Romance", "Musical" },
                FavoriteMusicGenres = new List<string> { "Pop", "Dance", "Hip-Hop" },
                FavoriteBookGenres = new List<string> { "Self-help", "Biography", "Romance" }
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 7,
                NoveltyPreference = 9,
                EnjoysSpontaneity = true
            };

            profile.SocialPreferences = new SocialPreferences();

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Evening",
                EnergyLevelPreference = 9,
                GroupSizePreference = "Large groups"
            };

            profile.PersonalityClassifications = new PersonalityClassifications
            {
                MBTIType = "ESFP",
                EnneagramType = "7w6",
                Openness = 8,
                Conscientiousness = 5,
                Extraversion = 10,
                Agreeableness = 9,
                Neuroticism = 3
            };

            profile.LoveLanguages = new LoveLanguages
            {
                QualityTime = 9,
                WordsOfAffirmation = 8,
                PhysicalTouch = 7,
                ActsOfService = 5,
                ReceivingGifts = 6
            };

            return profile;
        }

        public static PersonEntity CreateHealthWellnessProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? "Health & Wellness Advocate");
            profile.Description = "Passionate about health, fitness, and holistic wellness. Enjoys yoga, meditation, and healthy cooking.";
            profile.ContactInformation = "Boulder, CO";
            profile.Birthday = DateTime.UtcNow.AddYears(-31);

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Documentary", "Inspirational", "Nature" },
                FavoriteMusicGenres = new List<string> { "Meditation", "Acoustic", "World" },
                FavoriteBookGenres = new List<string> { "Health", "Wellness", "Spirituality" }
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 5,
                NoveltyPreference = 6,
                EnjoysSpontaneity = false
            };

            profile.LearningPreferences = new LearningPreferences
            {
                SubjectsOfInterest = new List<string> { "Nutrition", "Mindfulness", "Fitness" },
                LearningStyles = new List<string> { "Hands-on", "Workshop" }
            };

            profile.DietaryRestrictions = new DietaryRestrictions
            {
                Restrictions = new List<string> { "Vegan", "Organic" },
                Allergies = new List<string>()
            };

            profile.NaturePreferences = new NaturePreferences
            {
                PreferredSeasons = new List<string> { "Spring", "Summer" },
                FavoriteWeatherTypes = new List<string> { "Sunny", "Mild" },
                HasPets = false,
                EnjoysGardening = true,
                EnjoysBirdWatching = true
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Morning",
                EnergyLevelPreference = 8,
                GroupSizePreference = "Small groups"
            };

            profile.PersonalityClassifications = new PersonalityClassifications
            {
                MBTIType = "INFJ",
                EnneagramType = "1w9",
                Openness = 7,
                Conscientiousness = 9,
                Extraversion = 5,
                Agreeableness = 8,
                Neuroticism = 4
            };

            return profile;
        }

        #region Safety-Critical Profiles (For Demo)

        /// <summary>
        /// PersonEntity with peanut allergy - CRITICAL for safety filtering demo
        /// Should NEVER be recommended peanut-related events
        /// </summary>
        public static PersonEntity CreatePeanutAllergyProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Peanut Allergy User {variation}");
            profile.Description = "Food enthusiast with severe peanut allergy. Loves trying new restaurants but safety is paramount.";
            profile.ContactInformation = variation % 2 == 0 ? "Seattle, WA" : "Portland, OR";
            profile.Birthday = DateTime.UtcNow.AddYears(-25 - (variation % 20));

            profile.DietaryRestrictions = new DietaryRestrictions
            {
                Allergies = new List<string> { "peanuts" },
                Restrictions = new List<string>()
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 5,
                NoveltyPreference = 7,
                EnjoysSpontaneity = true
            };

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Comedy", "Documentary", "Drama" },
                FavoriteMusicGenres = new List<string> { "Indie", "Jazz", "Pop" },
                FavoriteBookGenres = new List<string> { "Food & Cooking", "Travel", "Memoir" }
            };

            return profile;
        }

        /// <summary>
        /// Wheelchair user - requires accessible venues
        /// </summary>
        public static PersonEntity CreateWheelchairUserProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Wheelchair User {variation}");
            profile.Description = "Active and adventurous! Uses a wheelchair and values accessibility. Loves accessible outdoor trails and inclusive venues.";
            profile.ContactInformation = variation % 3 == 0 ? "Seattle, WA" : variation % 3 == 1 ? "San Francisco, CA" : "Austin, TX";
            profile.Birthday = DateTime.UtcNow.AddYears(-35 - (variation % 15));

            profile.AccessibilityNeeds = new AccessibilityNeeds
            {
                RequiresWheelchairAccess = true,
                HasLimitedMobility = true,
                SpecialConsiderations = "Requires ramp access, accessible restrooms, and wheelchair-friendly seating"
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 6,
                NoveltyPreference = 8,
                EnjoysSpontaneity = false  // Needs to plan for accessibility
            };

            profile.SocialPreferences = new SocialPreferences
            {
                SocialBatteryLevel = 7,
                EnjoysMeetingNewPeople = true
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                GroupSizePreference = "Small groups",
                PreferredTimeOfDay = "Afternoon",
                EnergyLevelPreference = 6
            };

            return profile;
        }

        /// <summary>
        /// PersonEntity with epilepsy - CANNOT see flashing lights
        /// </summary>
        public static PersonEntity CreateEpilepticProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Epileptic User {variation}");
            profile.Description = "Music and art lover with epilepsy. Avoids venues with strobe lights and flashing effects.";
            profile.ContactInformation = "Los Angeles, CA";
            profile.Birthday = DateTime.UtcNow.AddYears(-28 - (variation % 15));

            profile.SensoryPreferences = new SensoryPreferences
            {
                SensitiveToFlashingLights = true,
                CrowdSensitivity = 2,  // Very sensitive to crowds (1-3 is high sensitivity)
                PrefersQuietEnvironments = true
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 3,  // Must be cautious
                NoveltyPreference = 6,
                EnjoysSpontaneity = false
            };

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Drama", "Comedy", "Romance" },
                FavoriteMusicGenres = new List<string> { "Jazz", "Acoustic", "Classical" },
                FavoriteBookGenres = new List<string> { "Fiction", "Art", "Biography" }
            };

            return profile;
        }

        /// <summary>
        /// Deaf user - prefers visual events
        /// </summary>
        public static PersonEntity CreateDeafUserProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Deaf User {variation}");
            profile.Description = "Deaf artist and visual storyteller. Loves museums, visual arts, and ASL-interpreted events.";
            profile.ContactInformation = "New York, NY";
            profile.Birthday = DateTime.UtcNow.AddYears(-30 - (variation % 20));

            profile.AccessibilityNeeds = new AccessibilityNeeds
            {
                RequiresHearingAssistance = true,
                RequiresSignLanguageInterpreter = true,
                SpecialConsiderations = "ASL interpreter needed for spoken events"
            };

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Silent films", "Visual arts", "Documentary" },
                FavoriteMusicGenres = new List<string>(),  // Visual focus, not music
                FavoriteBookGenres = new List<string> { "Art", "Photography", "Graphic novels" }
            };

            profile.LearningPreferences = new LearningPreferences
            {
                SubjectsOfInterest = new List<string> { "Visual arts", "Design", "Photography", "ASL" },
                LearningStyles = new List<string> { "Visual", "Hands-on" }
            };

            return profile;
        }

        /// <summary>
        /// Autistic user - needs predictable, quiet environments
        /// </summary>
        public static PersonEntity CreateAutismProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Autistic User {variation}");
            profile.Description = "Tech-savvy and detail-oriented. Prefers structured, predictable environments. Sensitive to loud noises and crowds.";
            profile.ContactInformation = "Seattle, WA";
            profile.Birthday = DateTime.UtcNow.AddYears(-24 - (variation % 15));

            profile.SensoryPreferences = new SensoryPreferences
            {
                NoiseToleranceLevel = 2,  // Very sensitive to noise (low = sensitive)
                CrowdSensitivity = 1,  // Extremely sensitive to crowds (1-3 is high sensitivity)
                Claustrophobic = false,
                SensitiveToFlashingLights = true,
                PrefersQuietEnvironments = true
            };

            profile.SocialPreferences = new SocialPreferences
            {
                SocialBatteryLevel = 3,  // Low social battery
                EnjoysMeetingNewPeople = false,
                PrefersDeepConversations = true
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 2,
                NoveltyPreference = 4,  // Prefers predictable
                EnjoysSpontaneity = false  // Needs structure
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Morning",  // Less crowded
                EnergyLevelPreference = 6,
                GroupSizePreference = "One-on-one"
            };

            return profile;
        }

        #endregion

        #region Diverse Lifestyle Profiles

        public static PersonEntity CreateFoodieProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Foodie {variation}");
            profile.Description = "Passionate food lover and restaurant explorer. Always hunting for the next amazing meal.";
            profile.ContactInformation = variation % 4 == 0 ? "San Francisco, CA" : variation % 4 == 1 ? "New York, NY" : variation % 4 == 2 ? "Portland, OR" : "Austin, TX";
            profile.Birthday = DateTime.UtcNow.AddYears(-30 - (variation % 25));

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Documentary", "Foreign", "Drama" },
                FavoriteMusicGenres = new List<string> { "Jazz", "World", "R&B" },
                FavoriteBookGenres = new List<string> { "Food & Cooking", "Travel", "Memoir" }
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 7,
                NoveltyPreference = 9,  // Loves trying new things
                EnjoysSpontaneity = true
            };

            profile.LearningPreferences = new LearningPreferences
            {
                SubjectsOfInterest = new List<string> { "Culinary arts", "Wine", "Cultural studies", "Travel" },
                LearningStyles = new List<string> { "Hands-on", "Workshop", "Tasting experiences" }
            };

            return profile;
        }

        public static PersonEntity CreateGamerProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Gamer {variation}");
            profile.Description = "Competitive gamer and esports enthusiast. Loves both video games and board games.";
            profile.ContactInformation = "Seattle, WA";
            profile.Birthday = DateTime.UtcNow.AddYears(-22 - (variation % 15));

            profile.EntertainmentPreferences = new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Sci-Fi", "Fantasy", "Action", "Animation" },
                FavoriteMusicGenres = new List<string> { "Electronic", "Synthwave", "Video game soundtracks" },
                FavoriteBookGenres = new List<string> { "Fantasy", "Science Fiction", "Graphic novels" }
            };

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 5,
                NoveltyPreference = 8,
                EnjoysSpontaneity = true
            };

            profile.SocialPreferences = new SocialPreferences
            {
                SocialBatteryLevel = 6,
                EnjoysMeetingNewPeople = true
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Night",
                EnergyLevelPreference = 7,
                GroupSizePreference = "Small groups"
            };

            return profile;
        }

        public static PersonEntity CreateYoungParentProfile(string? userId = null, string? name = null, int variation = 0)
        {
            var profile = CreateMinimalProfile(userId, name ?? $"Young Parent {variation}");
            profile.Description = "Parent of young kids (ages 3-8). Looking for family-friendly activities and occasional adult time.";
            profile.ContactInformation = variation % 2 == 0 ? "Austin, TX" : "Denver, CO";
            profile.Birthday = DateTime.UtcNow.AddYears(-33 - (variation % 10));

            profile.AdventurePreferences = new AdventurePreferences
            {
                RiskTolerance = 4,  // Lower risk with kids
                NoveltyPreference = 6,
                EnjoysSpontaneity = false  // Needs planning with kids
            };

            profile.ActivityPreferences = new ActivityPreferences
            {
                PreferredTimeOfDay = "Morning",  // Kids wake early
                EnergyLevelPreference = 5,  // Tired from parenting
                GroupSizePreference = "Small groups"
            };

            profile.SocialPreferences = new SocialPreferences
            {
                SocialBatteryLevel = 4,  // Drained from kids
                EnjoysMeetingNewPeople = false  // Tired, prefers familiar faces
            };

            return profile;
        }

        #endregion

        #endregion

        #region Preferences

        public static EntertainmentPreferences CreateEntertainmentPreferences()
        {
            return new EntertainmentPreferences
            {
                FavoriteMovieGenres = new List<string> { "Sci-Fi", "Comedy", "Documentary" },
                FavoriteMusicGenres = new List<string> { "Indie Rock", "Jazz", "Electronic" },
                FavoriteBookGenres = new List<string> { "Science Fiction", "Philosophy", "Biography" }
            };
        }

        public static AdventurePreferences CreateAdventurePreferences()
        {
            return new AdventurePreferences
            {
                RiskTolerance = 7,
                NoveltyPreference = 8,
                EnjoysSpontaneity = true
            };
        }

        public static LearningPreferences CreateLearningPreferences()
        {
            return new LearningPreferences
            {
                SubjectsOfInterest = new List<string> { "Technology", "History", "Art" },
                LearningStyles = new List<string> { "Visual", "Hands-on" }
            };
        }

        public static SensoryPreferences CreateSensoryPreferences()
        {
            return new SensoryPreferences();
        }

        public static SocialPreferences CreateSocialPreferences()
        {
            return new SocialPreferences();
        }

        public static StylePreferences CreateStylePreferences()
        {
            return new StylePreferences
            {
                FavoriteColors = new List<string> { "Blue", "Green", "Purple" },
                FashionStyle = new List<string> { "Casual", "Modern" },
                HomeDecorStyle = new List<string> { "Minimalist", "Cozy" },
                AestheticPreferences = new List<string> { "Clean lines", "Natural materials" },
                CasualVsFormal = 6,
                ColorPreferences = 7
            };
        }

        public static NaturePreferences CreateNaturePreferences()
        {
            return new NaturePreferences
            {
                PreferredSeasons = new List<string> { "Spring", "Fall" },
                FavoriteWeatherTypes = new List<string> { "Sunny", "Mild rain" },
                HasPets = true,
                PetTypes = new List<string> { "Dog", "Cat" },
                EnjoysGardening = true,
                EnjoysBirdWatching = false
            };
        }

        public static GiftPreferences CreateGiftPreferences()
        {
            return new GiftPreferences
            {
                MeaningfulGiftTypes = new List<string> { "Books", "Experiences", "Handmade items" },
                PreferredGiftStyle = "Thoughtful and personal",
                LikesSurprises = true,
                PrefersChoosing = false,
                CollectsOrHobbies = new List<string> { "Vintage records", "Plants" }
            };
        }

        public static ActivityPreferences CreateActivityPreferences()
        {
            return new ActivityPreferences
            {
                PreferredTimeOfDay = "Morning",
                EnergyLevelPreference = 7,
                GroupSizePreference = "Small groups"
            };
        }

        public static AccessibilityNeeds CreateAccessibilityNeeds()
        {
            return new AccessibilityNeeds
            {
                RequiresWheelchairAccess = false,
                HasLimitedMobility = false,
                RequiresHearingAssistance = false,
                RequiresSignLanguageInterpreter = false,
                RequiresLargeText = false,
                SpecialConsiderations = ""
            };
        }

        public static DietaryRestrictions CreateDietaryRestrictions()
        {
            return new DietaryRestrictions
            {
                Restrictions = new List<string> { "Vegetarian" },
                Allergies = new List<string> { "Peanuts" }
            };
        }

        public static LoveLanguages CreateLoveLanguages()
        {
            return new LoveLanguages
            {
                QualityTime = 9,
                WordsOfAffirmation = 7,
                PhysicalTouch = 6,
                ActsOfService = 5,
                ReceivingGifts = 4
            };
        }

        public static PersonalityClassifications CreatePersonalityClassifications()
        {
            return new PersonalityClassifications
            {
                MBTIType = "INFP",
                EnneagramType = "4w5",
                Openness = 8,
                Conscientiousness = 7,
                Extraversion = 4,
                Agreeableness = 8,
                Neuroticism = 5
            };
        }

        #endregion

        #region Conversation

        public static ConversationContext CreateConversationContext(string profileId, int chunksCount = 3, int insightsCount = 2)
        {
            var conversation = new ConversationContext
            {
                Id = Guid.NewGuid().ToString(),
                ProfileId = profileId,
                UserId = $"user-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            for (int i = 0; i < chunksCount; i++)
            {
                conversation.ConversationChunks.Add(new ConversationChunk
                {
                    Text = $"Test conversation chunk {i + 1}",
                    Speaker = i % 2 == 0 ? "user" : "ai",
                    Timestamp = DateTime.UtcNow.AddMinutes(-chunksCount + i)
                });
            }

            for (int i = 0; i < insightsCount; i++)
            {
                conversation.ExtractedInsights.Add(new ExtractedInsight
                {
                    Category = i % 2 == 0 ? "hobby" : "preference",
                    Insight = $"Test insight {i + 1}",
                    Confidence = 0.8f + (i * 0.05f),
                    SourceChunk = $"Test conversation chunk {i + 1}",
                    ExtractedAt = DateTime.UtcNow.AddMinutes(-insightsCount + i)
                });
            }

            return conversation;
        }

        #endregion

        #region Embeddings

        public static EntityEmbedding CreateEntityEmbedding(string profileId, EmbeddingStatus status = EmbeddingStatus.Pending)
        {
            return new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = "This is a test profile summary with comprehensive preference information.",
                SummaryHash = EntityEmbedding.ComputeHash("This is a test profile summary with comprehensive preference information."),
                EntityLastModified = DateTime.UtcNow,
                GeneratedAt = DateTime.UtcNow,
                Status = status,
                SummaryMetadata = new SummaryMetadata
                {
                    SummaryWordCount = 50,
                    PreferenceCategories = new List<string> { "Entertainment", "Adventure", "Style" },
                    HasPersonalityData = true,
                    HasConversationData = false,
                    ConversationChunksCount = 0,
                    ExtractedInsightsCount = 0
                }
            };
        }

        public static EntityEmbedding CreateEntityEmbeddingWithVector(string profileId)
        {
            var embedding = CreateEntityEmbedding(profileId, EmbeddingStatus.Generated);
            embedding.Embedding = CreateTestEmbeddingVector();
            return embedding;
        }

        public static float[] CreateTestEmbeddingVector(int dimensions = 1536)
        {
            var random = new Random(42); // Fixed seed for reproducible tests
            var vector = new float[dimensions];
            for (int i = 0; i < dimensions; i++)
            {
                vector[i] = (float)random.NextDouble();
            }
            return vector;
        }

        #endregion
    }
}
