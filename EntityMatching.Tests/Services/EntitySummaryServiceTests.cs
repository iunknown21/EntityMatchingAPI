using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Shared.Models;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Infrastructure.Services.SummaryStrategies;
using Xunit;

namespace EntityMatching.Tests.Services
{
    /// <summary>
    /// Unit tests for EntitySummaryService
    /// Tests summary generation, metadata tracking, and conversation integration
    /// </summary>
    public class EntitySummaryServiceTests
    {
        private readonly Mock<ILogger<EntitySummaryService>> _mockLogger;
        private readonly Mock<ILogger<PersonSummaryStrategy>> _mockPersonStrategyLogger;
        private readonly EntitySummaryService _summaryService;

        public EntitySummaryServiceTests()
        {
            _mockLogger = new Mock<ILogger<EntitySummaryService>>();
            _mockPersonStrategyLogger = new Mock<ILogger<PersonSummaryStrategy>>();

            var strategies = new List<IEntitySummaryStrategy>
            {
                new PersonSummaryStrategy(_mockPersonStrategyLogger.Object)
            };

            _summaryService = new EntitySummaryService(_mockLogger.Object, strategies);
        }

        #region Basic Summary Generation

        [Fact]
        public async Task GenerateSummaryAsync_WithMinimalProfile_ReturnsBasicSummary()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "John Doe"
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Should().NotBeNull();
            result.Summary.Should().Contain("John Doe");
            result.Metadata.Should().NotBeNull();
            result.Metadata.SummaryWordCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithBioAndLocation_IncludesInSummary()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Jane Smith",
                Description = "A creative professional who loves art and music",
                ContactInformation = "Seattle, WA"
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Summary.Should().Contain("Jane Smith");
            result.Summary.Should().Contain("creative professional");
            result.Summary.Should().Contain("Seattle");
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithAge_IncludesInSummary()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Bob Johnson",
                Birthday = DateTime.Today.AddYears(-35)
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert - Use the actual computed Age value
            result.Summary.Should().Contain(profile.Age.ToString());
            result.Summary.Should().Contain("years old");
        }

        #endregion

        #region Preference Categories

        [Fact]
        public async Task GenerateSummaryAsync_WithStylePreferences_IncludesStyleSection()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Alice Cooper",
                StylePreferences = new StylePreferences
                {
                    FavoriteColors = new List<string> { "Blue", "Green" },
                    FashionStyle = new List<string> { "Casual", "Sporty" }
                }
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Summary.Should().Contain("=== Style & Aesthetics ===");
            result.Summary.Should().Contain("Blue");
            result.Summary.Should().Contain("Green");
            result.Summary.Should().Contain("Casual");
            result.Metadata.PreferenceCategories.Should().Contain("Style");
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithNaturePreferences_IncludesNatureSection()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Nature Lover",
                NaturePreferences = new NaturePreferences
                {
                    PreferredSeasons = new List<string> { "Spring", "Summer" },
                    HasPets = true,
                    PetTypes = new List<string> { "Dog", "Cat" },
                    EnjoysGardening = true
                }
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Summary.Should().Contain("=== Nature & Environment ===");
            result.Summary.Should().Contain("Spring");
            result.Summary.Should().Contain("Summer");
            result.Summary.Should().Contain("Dog");
            result.Summary.Should().Contain("gardening");
            result.Metadata.PreferenceCategories.Should().Contain("Nature");
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithGiftPreferences_IncludesGiftSection()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Gift Enthusiast",
                GiftPreferences = new GiftPreferences
                {
                    MeaningfulGiftTypes = new List<string> { "Books", "Handmade items" },
                    PreferredGiftStyle = "Thoughtful and personal",
                    LikesSurprises = true,
                    CollectsOrHobbies = new List<string> { "Vintage records", "Stamps" }
                }
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Summary.Should().Contain("=== Gift Preferences ===");
            result.Summary.Should().Contain("Books");
            result.Summary.Should().Contain("surprises");
            result.Summary.Should().Contain("Vintage records");
            result.Metadata.PreferenceCategories.Should().Contain("Gift");
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithMultiplePreferences_IncludesAllCategories()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Well-Rounded Person",
                EntertainmentPreferences = new EntertainmentPreferences(),
                AdventurePreferences = new AdventurePreferences(),
                LearningPreferences = new LearningPreferences(),
                SensoryPreferences = new SensoryPreferences(),
                SocialPreferences = new SocialPreferences()
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Metadata.PreferenceCategories.Should().Contain("Entertainment");
            result.Metadata.PreferenceCategories.Should().Contain("Adventure");
            result.Metadata.PreferenceCategories.Should().Contain("Learning");
            result.Metadata.PreferenceCategories.Should().Contain("Sensory");
            result.Metadata.PreferenceCategories.Should().Contain("Social");
        }

        #endregion

        #region Accessibility and Dietary

        [Fact]
        public async Task GenerateSummaryAsync_WithAccessibilityNeeds_IncludesAccessibilitySection()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Accessible User",
                AccessibilityNeeds = new AccessibilityNeeds
                {
                    RequiresWheelchairAccess = true,
                    RequiresHearingAssistance = true,
                    SpecialConsiderations = "Prefers quiet environments"
                }
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Summary.Should().Contain("=== Accessibility Needs ===");
            result.Summary.Should().Contain("wheelchair access");
            result.Summary.Should().Contain("hearing assistance");
            result.Summary.Should().Contain("quiet environments");
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithDietaryRestrictions_IncludesDietarySection()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Careful Eater",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Restrictions = new List<string> { "Vegetarian", "Gluten-free" },
                    Allergies = new List<string> { "Peanuts", "Shellfish" }
                }
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Summary.Should().Contain("=== Dietary Restrictions ===");
            result.Summary.Should().Contain("Vegetarian");
            result.Summary.Should().Contain("Gluten-free");
            result.Summary.Should().Contain("Peanuts");
            result.Summary.Should().Contain("Shellfish");
        }

        #endregion

        #region Conversation Integration

        [Fact]
        public async Task GenerateSummaryAsync_WithConversationContext_IncludesInsights()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Chat User"
            };

            var conversation = new ConversationContext
            {
                Id = Guid.NewGuid().ToString(),
                ProfileId = profile.Id.ToString(),
                ConversationChunks = new List<ConversationChunk>
                {
                    new ConversationChunk { Text = "I love hiking", Speaker = "user" },
                    new ConversationChunk { Text = "That's great! Tell me more.", Speaker = "ai" }
                },
                ExtractedInsights = new List<ExtractedInsight>
                {
                    new ExtractedInsight
                    {
                        Category = "hobby",
                        Insight = "Enjoys hiking",
                        Confidence = 0.95f
                    },
                    new ExtractedInsight
                    {
                        Category = "preference",
                        Insight = "Prefers outdoor activities",
                        Confidence = 0.85f
                    }
                }
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile, conversation);

            // Assert
            result.Summary.Should().Contain("=== Additional Insights from Conversations ===");
            result.Metadata.HasConversationData.Should().BeTrue();
            result.Metadata.ConversationChunksCount.Should().Be(2);
            result.Metadata.ExtractedInsightsCount.Should().Be(2);
        }

        [Fact]
        public async Task GenerateSummaryAsync_WithoutConversation_DoesNotIncludeConversationSection()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "No Chat User"
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile, null);

            // Assert
            result.Summary.Should().NotContain("=== Additional Insights from Conversations ===");
            result.Metadata.HasConversationData.Should().BeFalse();
            result.Metadata.ConversationChunksCount.Should().Be(0);
            result.Metadata.ExtractedInsightsCount.Should().Be(0);
        }

        #endregion

        #region Metadata Tests

        [Fact]
        public async Task GenerateSummaryAsync_CalculatesWordCountCorrectly()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Word Counter",
                Description = "This is a bio with exactly ten words here."
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Metadata.SummaryWordCount.Should().BeGreaterThan(10);
        }

        [Fact]
        public async Task GenerateSummaryAsync_TracksPersonalityData()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Personality Test",
                PersonalityClassifications = new PersonalityClassifications()
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile);

            // Assert
            result.Metadata.HasPersonalityData.Should().BeTrue();
        }

        #endregion

        #region Comprehensive PersonEntity Test

        [Fact]
        public async Task GenerateSummaryAsync_WithComprehensiveProfile_GeneratesDetailedSummary()
        {
            // Arrange
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                Name = "Comprehensive User",
                Description = "A well-rounded individual",
                ContactInformation = "Portland, OR",
                Birthday = DateTime.Today.AddYears(-30),
                EntertainmentPreferences = new EntertainmentPreferences(),
                AdventurePreferences = new AdventurePreferences(),
                LearningPreferences = new LearningPreferences(),
                SensoryPreferences = new SensoryPreferences(),
                SocialPreferences = new SocialPreferences(),
                StylePreferences = new StylePreferences
                {
                    FavoriteColors = new List<string> { "Purple" }
                },
                NaturePreferences = new NaturePreferences
                {
                    PreferredSeasons = new List<string> { "Fall" }
                },
                GiftPreferences = new GiftPreferences
                {
                    LikesSurprises = true
                },
                AccessibilityNeeds = new AccessibilityNeeds
                {
                    SpecialConsiderations = "None"
                },
                DietaryRestrictions = new DietaryRestrictions
                {
                    Restrictions = new List<string> { "Vegan" }
                },
                LoveLanguages = new LoveLanguages(),
                PersonalityClassifications = new PersonalityClassifications()
            };

            var conversation = new ConversationContext
            {
                ExtractedInsights = new List<ExtractedInsight>
                {
                    new ExtractedInsight
                    {
                        Category = "hobby",
                        Insight = "Plays guitar",
                        Confidence = 0.9f
                    }
                },
                ConversationChunks = new List<ConversationChunk>
                {
                    new ConversationChunk { Text = "I play guitar", Speaker = "user" }
                }
            };

            // Act
            var result = await _summaryService.GenerateSummaryAsync(profile, conversation);

            // Assert
            result.Should().NotBeNull();
            result.Summary.Should().Contain("Comprehensive User");
            result.Summary.Should().Contain("Portland");
            result.Summary.Should().Contain(profile.Age.ToString()); // Use computed Age value

            // Should have all sections
            result.Metadata.PreferenceCategories.Count.Should().BeGreaterThan(5);
            result.Metadata.HasPersonalityData.Should().BeTrue();
            result.Metadata.HasConversationData.Should().BeTrue();
            result.Metadata.SummaryWordCount.Should().BeGreaterThan(20);
        }

        #endregion
    }
}
