using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Models.Search;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Shared.Models;
using Xunit;

namespace EntityMatching.Tests.Unit
{
    /// <summary>
    /// Unit tests for EventSearchStrategy
    /// Tests query generation, safety requirements, scoring weights, and match scoring
    /// </summary>
    public class EventSearchStrategyTests
    {
        private readonly Mock<ILogger<EventSearchStrategy>> _mockLogger;
        private readonly EventSearchStrategy _strategy;

        public EventSearchStrategyTests()
        {
            _mockLogger = new Mock<ILogger<EventSearchStrategy>>();
            _strategy = new EventSearchStrategy(_mockLogger.Object);
        }

        #region GenerateSearchQueries Tests

        [Fact]
        public void GenerateSearchQueries_WithBasicProfile_ReturnsBaseQuery()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Test User"
            };
            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 31)
            };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().NotBeEmpty();
            queries.Should().Contain(q => q.Contains("events activities Seattle, WA"));
        }

        [Fact]
        public void GenerateSearchQueries_WithAllergies_IncludesSafetyQueries()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Allergy User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts", "shellfish" }
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().Contain(q => q.Contains("allergy-friendly safe events"));
        }

        [Fact]
        public void GenerateSearchQueries_WithWheelchairAccess_IncludesAccessibilityQueries()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Wheelchair User",
                AccessibilityNeeds = new AccessibilityNeeds
                {
                    RequiresWheelchairAccess = true
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().Contain(q => q.Contains("wheelchair accessible events"));
        }

        [Fact]
        public void GenerateSearchQueries_WithMusicPreferences_IncludesMusicQueries()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Music Lover",
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Jazz", "Blues", "Rock" }
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().Contain(q => q.Contains("Jazz Blues") && q.Contains("live music concerts"));
        }

        [Fact]
        public void GenerateSearchQueries_WithIntrovert_IncludesIntimateQueries()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Introvert",
                SocialPreferences = new SocialPreferences
                {
                    SocialBatteryLevel = 2
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().Contain(q => q.Contains("intimate quiet small group events"));
        }

        [Fact]
        public void GenerateSearchQueries_WithExtrovert_IncludesSocialQueries()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Extrovert",
                SocialPreferences = new SocialPreferences
                {
                    SocialBatteryLevel = 9
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().Contain(q => q.Contains("social interactive group events"));
        }

        [Fact]
        public void GenerateSearchQueries_WithHighRiskTolerance_IncludesAdventureQueries()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Thrill Seeker",
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 9
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().Contain(q => q.Contains("adventure extreme thrilling events"));
        }

        [Fact]
        public void GenerateSearchQueries_WithLowRiskTolerance_IncludesCalmQueries()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Safety First",
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 2
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().Contain(q => q.Contains("calm relaxing safe events"));
        }

        [Fact]
        public void GenerateSearchQueries_WithCategory_AppliesCategoryFilter()
        {
            // Arrange
            var profile = new PersonEntity {  Name = "Test User" };
            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                Category = "music"
            };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().OnlyContain(q => q.Contains("music"));
        }

        [Fact]
        public void GenerateSearchQueries_LimitsTo8Queries()
        {
            // Arrange - Create profile with many preferences to generate lots of queries
            var profile = new PersonEntity
            {
                
                Name = "Complex User",
                DietaryRestrictions = new DietaryRestrictions { Allergies = new List<string> { "peanuts" } },
                AccessibilityNeeds = new AccessibilityNeeds { RequiresWheelchairAccess = true },
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Jazz", "Blues" },
                    FavoriteMovieGenres = new List<string> { "Action", "Comedy" }
                },
                SocialPreferences = new SocialPreferences { SocialBatteryLevel = 2 },
                AdventurePreferences = new AdventurePreferences { RiskTolerance = 8, NoveltyPreference = 9 },
                LearningPreferences = new LearningPreferences
                {
                    SubjectsOfInterest = new List<string> { "History", "Science" }
                },
                SensoryPreferences = new SensoryPreferences { NoiseToleranceLevel = 3 },
                NaturePreferences = new NaturePreferences { EnjoysGardening = true }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var queries = _strategy.GenerateSearchQueries(profile, parameters);

            // Assert
            queries.Should().HaveCountLessThanOrEqualTo(8);
        }

        #endregion

        #region GetCriticalSafetyRequirements Tests

        [Fact]
        public void GetCriticalSafetyRequirements_WithAllergies_ReturnsCriticalRequirements()
        {
            // Arrange
            var profile = new PersonEntity
            {

                Name = "Allergy User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts", "shellfish" }
                },
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 5 // Mid-range to avoid triggering low-risk requirement
                }
            };

            // Act
            var requirements = _strategy.GetCriticalSafetyRequirements(profile);

            // Assert
            requirements.Should().HaveCount(2);
            requirements.Should().Contain(r => r.Key == "no_peanuts" && r.Importance == SafetyImportance.Critical);
            requirements.Should().Contain(r => r.Key == "no_shellfish" && r.Importance == SafetyImportance.Critical);
        }

        [Fact]
        public void GetCriticalSafetyRequirements_WithFlashingLights_ReturnsCritical()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Photosensitive User",
                SensoryPreferences = new SensoryPreferences
                {
                    SensitiveToFlashingLights = true
                }
            };

            // Act
            var requirements = _strategy.GetCriticalSafetyRequirements(profile);

            // Assert
            requirements.Should().Contain(r =>
                r.Key == "no_flashing_lights" &&
                r.Importance == SafetyImportance.Critical);
        }

        [Fact]
        public void GetCriticalSafetyRequirements_WithClaustrophobia_ReturnsCritical()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Claustrophobic User",
                SensoryPreferences = new SensoryPreferences
                {
                    Claustrophobic = true
                }
            };

            // Act
            var requirements = _strategy.GetCriticalSafetyRequirements(profile);

            // Assert
            requirements.Should().Contain(r =>
                r.Key == "no_enclosed_spaces" &&
                r.Importance == SafetyImportance.Critical);
        }

        [Fact]
        public void GetCriticalSafetyRequirements_WithNoiseSensitivity_ReturnsHigh()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Noise Sensitive User",
                SensoryPreferences = new SensoryPreferences
                {
                    NoiseToleranceLevel = 3,
                    PrefersQuietEnvironments = true
                }
            };

            // Act
            var requirements = _strategy.GetCriticalSafetyRequirements(profile);

            // Assert
            requirements.Should().Contain(r =>
                r.Key == "quiet_environment" &&
                r.Importance == SafetyImportance.High);
        }

        [Fact]
        public void GetCriticalSafetyRequirements_WithWheelchairAccess_ReturnsCritical()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Wheelchair User",
                AccessibilityNeeds = new AccessibilityNeeds
                {
                    RequiresWheelchairAccess = true
                }
            };

            // Act
            var requirements = _strategy.GetCriticalSafetyRequirements(profile);

            // Assert
            requirements.Should().Contain(r =>
                r.Key == "wheelchair_accessible" &&
                r.Importance == SafetyImportance.Critical);
        }

        [Fact]
        public void GetCriticalSafetyRequirements_WithDietaryRestrictions_ReturnsHigh()
        {
            // Arrange
            var profile = new PersonEntity
            {

                Name = "Vegan User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Restrictions = new List<string> { "Vegan", "Gluten-Free" }
                },
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 5 // Mid-range to avoid triggering low-risk requirement
                }
            };

            // Act
            var requirements = _strategy.GetCriticalSafetyRequirements(profile);

            // Assert
            requirements.Should().HaveCount(2);
            requirements.Should().AllSatisfy(r => r.Importance.Should().Be(SafetyImportance.High));
        }

        [Fact]
        public void GetCriticalSafetyRequirements_OrdersByCriticalFirst()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Multiple Requirements",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts" },
                    Restrictions = new List<string> { "Vegan" }
                },
                SensoryPreferences = new SensoryPreferences
                {
                    NoiseToleranceLevel = 3
                }
            };

            // Act
            var requirements = _strategy.GetCriticalSafetyRequirements(profile);

            // Assert
            requirements.First().Importance.Should().Be(SafetyImportance.Critical);
        }

        #endregion

        #region GetScoringWeights Tests

        [Fact]
        public void GetScoringWeights_WithDefaultProfile_ReturnsDefaultWeights()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Name = "Default User",
                SocialPreferences = new SocialPreferences
                {
                    SocialBatteryLevel = 5 // Mid-range to avoid weight adjustments
                },
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 5 // Mid-range to avoid triggering safety checks
                }
            };

            // Act
            var weights = _strategy.GetScoringWeights(profile);

            // Assert
            weights["Safety"].Should().Be(0.35);
            weights["Social"].Should().Be(0.25);
            weights["Sensory"].Should().Be(0.20);
            weights["Interest"].Should().Be(0.15);
            weights["Practical"].Should().Be(0.05);
            weights.Values.Sum().Should().BeApproximately(1.0, 0.01);
        }

        [Fact]
        public void GetScoringWeights_WithCriticalSafety_IncreasesSafetyWeight()
        {
            // Arrange
            var profile = new PersonEntity
            {

                Name = "High Safety User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts" }
                },
                SocialPreferences = new SocialPreferences
                {
                    SocialBatteryLevel = 5 // Mid-range to avoid weight adjustments
                },
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 5 // Mid-range to avoid triggering safety checks
                }
            };

            // Act
            var weights = _strategy.GetScoringWeights(profile);

            // Assert
            weights["Safety"].Should().Be(0.45);
            weights.Values.Sum().Should().BeApproximately(1.0, 0.01);
        }

        [Fact]
        public void GetScoringWeights_WithIntrovert_IncreasesSensoryWeight()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Introvert",
                SocialPreferences = new SocialPreferences
                {
                    SocialBatteryLevel = 2
                }
            };

            // Act
            var weights = _strategy.GetScoringWeights(profile);

            // Assert
            weights["Sensory"].Should().Be(0.30);
            weights["Social"].Should().Be(0.15);
        }

        [Fact]
        public void GetScoringWeights_WithExtrovert_IncreasesSocialWeight()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Extrovert",
                SocialPreferences = new SocialPreferences
                {
                    SocialBatteryLevel = 9
                }
            };

            // Act
            var weights = _strategy.GetScoringWeights(profile);

            // Assert
            weights["Social"].Should().Be(0.35);
            weights["Sensory"].Should().Be(0.15);
        }

        #endregion

        #region CalculateMatchScore Tests

        [Fact]
        public void CalculateMatchScore_WithPerfectMatch_ReturnsHighScore()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Jazz Lover",
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Jazz" }
                }
            };
            var eventItem = new Event
            {
                Id = "evt-1",
                Title = "Jazz Concert at Blue Note",
                Description = "Live jazz performance featuring top artists",
                Category = "music"
            };
            var weights = _strategy.GetScoringWeights(profile);

            // Act
            var score = _strategy.CalculateMatchScore(eventItem, profile, weights);

            // Assert
            score.Should().BeGreaterThan(0.5);
            score.Should().BeLessThanOrEqualTo(1.0);
        }

        [Fact]
        public void CalculateMatchScore_PopulatesScoringBreakdown()
        {
            // Arrange
            var profile = new PersonEntity {  Name = "Test User" };
            var eventItem = new Event
            {
                Id = "evt-1",
                Title = "Community Event",
                Description = "Family-friendly outdoor gathering",
                Category = "community"
            };
            var weights = _strategy.GetScoringWeights(profile);

            // Act
            var score = _strategy.CalculateMatchScore(eventItem, profile, weights);

            // Assert
            eventItem.ScoringBreakdown.Should().NotBeEmpty();
            eventItem.ScoringBreakdown.Should().ContainKeys("Safety", "Social", "Sensory", "Interest", "Practical");
        }

        [Fact]
        public void CalculateMatchScore_ReturnsScoreBetween0And1()
        {
            // Arrange
            var profile = new PersonEntity {  Name = "Test User" };
            var eventItem = new Event
            {
                Id = "evt-1",
                Title = "Test Event",
                Description = "Test description",
                Category = "test"
            };
            var weights = _strategy.GetScoringWeights(profile);

            // Act
            var score = _strategy.CalculateMatchScore(eventItem, profile, weights);

            // Assert
            score.Should().BeGreaterThanOrEqualTo(0.0);
            score.Should().BeLessThanOrEqualTo(1.0);
        }

        #endregion

        #region GenerateEntitySummary Tests

        [Fact]
        public void GenerateEntitySummary_IncludesBasicInfo()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "John Doe"
            };
            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 31)
            };

            // Act
            var summary = _strategy.GenerateEntitySummary(profile, parameters);

            // Assert
            summary.Should().Contain("John Doe");
            summary.Should().Contain("Seattle, WA");
        }

        [Fact]
        public void GenerateEntitySummary_IncludesCriticalSafety()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Safety User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts" }
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var summary = _strategy.GenerateEntitySummary(profile, parameters);

            // Assert
            summary.Should().Contain("CRITICAL SAFETY REQUIREMENTS");
            summary.Should().Contain("peanuts");
        }

        [Fact]
        public void GenerateEntitySummary_IncludesMusicPreferences()
        {
            // Arrange
            var profile = new PersonEntity
            {
                
                Name = "Music Lover",
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Jazz", "Blues" }
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var summary = _strategy.GenerateEntitySummary(profile, parameters);

            // Assert
            summary.Should().Contain("Music:");
            summary.Should().Contain("Jazz");
            summary.Should().Contain("Blues");
        }

        #endregion
    }
}
