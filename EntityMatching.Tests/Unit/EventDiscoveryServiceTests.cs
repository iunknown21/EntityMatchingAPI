using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Search;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Shared.Models;
using Xunit;

namespace EntityMatching.Tests.Unit
{
    /// <summary>
    /// Unit tests for EventDiscoveryService
    /// Uses mocks to test orchestration logic without external dependencies
    /// </summary>
    public class EventDiscoveryServiceTests
    {
        private readonly Mock<IEntityService> _mockProfileService;
        private readonly Mock<IThingSearchStrategy<EventSearchParams, Event>> _mockSearchStrategy;
        private readonly Mock<IWebSearchService> _mockWebSearchService;
        private readonly Mock<ILogger<EventDiscoveryService>> _mockLogger;
        private readonly EventDiscoveryService _service;

        public EventDiscoveryServiceTests()
        {
            _mockProfileService = new Mock<IEntityService>();
            _mockSearchStrategy = new Mock<IThingSearchStrategy<EventSearchParams, Event>>();
            _mockWebSearchService = new Mock<IWebSearchService>();
            _mockLogger = new Mock<ILogger<EventDiscoveryService>>();

            _service = new EventDiscoveryService(
                _mockProfileService.Object,
                _mockSearchStrategy.Object,
                _mockWebSearchService.Object,
                _mockLogger.Object);
        }

        #region DiscoverViaWebSearchAsync Tests

        [Fact]
        public async Task DiscoverViaWebSearchAsync_WithValidProfile_ReturnsEvents()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity
            {
                Name = "Test User",
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Jazz" }
                }
            };
            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                MaxResults = 10
            };

            var queries = new List<string> { "jazz concerts Seattle, WA January 2025" };
            var safetyRequirements = new List<SafetyRequirement>();
            var scoringWeights = new Dictionary<string, double>
            {
                { "Safety", 0.35 },
                { "Social", 0.25 },
                { "Sensory", 0.20 },
                { "Interest", 0.15 },
                { "Practical", 0.05 }
            };

            var webSearchEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Jazz Concert",
                    Description = "Live jazz performance",
                    Location = "Seattle",
                    Category = "music",
                    Source = "web_search"
                }
            };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(safetyRequirements);
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(scoringWeights);
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(It.IsAny<Event>(), profile, scoringWeights))
                .Returns(0.85);
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webSearchEvents);

            // Act
            var result = await _service.DiscoverViaWebSearchAsync(profileId, parameters, 10);

            // Assert
            result.Should().NotBeNull();
            result.Matches.Should().NotBeEmpty();
            result.Matches.Should().HaveCount(1);
            result.Matches[0].Title.Should().Be("Jazz Concert");
            result.Matches[0].MatchScore.Should().Be(0.85);
            result.Metadata.SearchMode.Should().Be("web_search");
            result.Metadata.ProfileId.Should().Be(profileId);
            result.Metadata.WebSearchResults.Should().Be(1);
            result.Metadata.EmbeddingResults.Should().Be(0);
        }

        [Fact]
        public async Task DiscoverViaWebSearchAsync_WithNonExistentProfile_ReturnsEmptyResult()
        {
            // Arrange
            var profileId = "non-existent-profile";
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync((PersonEntity?)null);

            // Act
            var result = await _service.DiscoverViaWebSearchAsync(profileId, parameters, 10);

            // Assert
            result.Should().NotBeNull();
            result.Matches.Should().BeEmpty();
            result.TotalMatches.Should().Be(0);
        }

        [Fact]
        public async Task DiscoverViaWebSearchAsync_GeneratesMultipleQueries()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity { Name = "Test User" };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            var queries = new List<string>
            {
                "query1",
                "query2",
                "query3",
                "query4",
                "query5",
                "query6"
            };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(new List<SafetyRequirement>());
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(new Dictionary<string, double> { { "Safety", 1.0 } });
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Event>());

            // Act
            await _service.DiscoverViaWebSearchAsync(profileId, parameters, 10);

            // Assert
            _mockWebSearchService.Verify(
                x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(6)); // Should execute all 6 queries
        }

        [Fact]
        public async Task DiscoverViaWebSearchAsync_AppliesSafetyFiltering()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity
            {
                Name = "Allergy User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts" }
                },
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 5 // Mid-range to avoid triggering low-risk requirement
                }
            };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            var queries = new List<string> { "food events Seattle" };
            var safetyRequirements = new List<SafetyRequirement>
            {
                new SafetyRequirement("no_peanuts", "No peanuts", SafetyImportance.Critical)
            };

            var webSearchEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Peanut Festival",
                    Description = "Celebration featuring peanut dishes",
                    Category = "food"
                },
                new Event
                {
                    Id = "evt-2",
                    Title = "Safe Food Event",
                    Description = "Allergy-friendly food festival",
                    Category = "food"
                }
            };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(safetyRequirements);
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(new Dictionary<string, double> { { "Safety", 1.0 } });
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(It.IsAny<Event>(), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.7);
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webSearchEvents);

            // Act
            var result = await _service.DiscoverViaWebSearchAsync(profileId, parameters, 10);

            // Assert
            result.Matches.Should().HaveCount(1);
            result.Matches[0].Title.Should().Be("Safe Food Event");
        }

        [Fact]
        public async Task DiscoverViaWebSearchAsync_SetsMatchScores()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity { Name = "Test User" };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            var queries = new List<string> { "events Seattle" };
            var webSearchEvents = new List<Event>
            {
                new Event { Id = "evt-1", Title = "Event 1" },
                new Event { Id = "evt-2", Title = "Event 2" }
            };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(new List<SafetyRequirement>());
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(new Dictionary<string, double> { { "Safety", 1.0 } });
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(It.IsAny<Event>(), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.75);
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webSearchEvents);

            // Act
            var result = await _service.DiscoverViaWebSearchAsync(profileId, parameters, 10);

            // Assert
            result.Matches.Should().AllSatisfy(evt => evt.MatchScore.Should().Be(0.75));
        }

        [Fact]
        public async Task DiscoverViaWebSearchAsync_LimitsResults()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity { Name = "Test User" };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };
            var limit = 5;

            var queries = new List<string> { "events Seattle" };
            var webSearchEvents = Enumerable.Range(1, 20)
                .Select(i => new Event { Id = $"evt-{i}", Title = $"Event {i}" })
                .ToList();

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(new List<SafetyRequirement>());
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(new Dictionary<string, double> { { "Safety", 1.0 } });
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(It.IsAny<Event>(), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.7);
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webSearchEvents);

            // Act
            var result = await _service.DiscoverViaWebSearchAsync(profileId, parameters, limit);

            // Assert
            result.Matches.Should().HaveCount(limit);
        }

        #endregion

        #region DiscoverViaEmbeddingsAsync Tests

        [Fact]
        public async Task DiscoverViaEmbeddingsAsync_ReturnsPlaceholderResult()
        {
            // Arrange
            var profileId = "test-profile-123";
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            // Act
            var result = await _service.DiscoverViaEmbeddingsAsync(profileId, parameters, 10);

            // Assert
            result.Should().NotBeNull();
            result.Matches.Should().BeEmpty();
            result.Metadata.SearchMode.Should().Be("embeddings");
            result.Metadata.ProfileId.Should().Be(profileId);
        }

        #endregion

        #region DiscoverHybridAsync Tests

        [Fact]
        public async Task DiscoverHybridAsync_FallsBackToWebSearch()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity { Name = "Test User" };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            var queries = new List<string> { "events Seattle" };
            var webSearchEvents = new List<Event>
            {
                new Event { Id = "evt-1", Title = "Hybrid Event" }
            };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(new List<SafetyRequirement>());
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(new Dictionary<string, double> { { "Safety", 1.0 } });
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(It.IsAny<Event>(), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.8);
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webSearchEvents);

            // Act
            var result = await _service.DiscoverHybridAsync(profileId, parameters, 10);

            // Assert
            result.Should().NotBeNull();
            result.Matches.Should().HaveCount(1);
            result.Metadata.SearchMode.Should().Be("hybrid");
            result.Metadata.WebSearchResults.Should().Be(1);
            result.Metadata.EmbeddingResults.Should().Be(0);
        }

        [Fact]
        public async Task DiscoverHybridAsync_DeduplicatesResults()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity { Name = "Test User" };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            var queries = new List<string> { "events Seattle" };
            var webSearchEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Jazz Concert",
                    Location = "Seattle Blue Note",
                    EventDate = new DateTime(2025, 1, 15)
                },
                new Event
                {
                    Id = "evt-2",
                    Title = "Jazz Concert",  // Duplicate
                    Location = "Seattle Blue Note",
                    EventDate = new DateTime(2025, 1, 15)
                },
                new Event
                {
                    Id = "evt-3",
                    Title = "Rock Concert",
                    Location = "Seattle Arena",
                    EventDate = new DateTime(2025, 1, 20)
                }
            };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(new List<SafetyRequirement>());
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(new Dictionary<string, double> { { "Safety", 1.0 } });
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(It.IsAny<Event>(), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.8);
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webSearchEvents);

            // Act
            var result = await _service.DiscoverHybridAsync(profileId, parameters, 10);

            // Assert
            result.Matches.Should().HaveCount(2); // Duplicates removed
            result.Matches.Should().ContainSingle(e => e.Title == "Jazz Concert");
            result.Matches.Should().ContainSingle(e => e.Title == "Rock Concert");
        }

        [Fact]
        public async Task DiscoverHybridAsync_RanksByMatchScore()
        {
            // Arrange
            var profileId = "test-profile-123";
            var profile = new PersonEntity { Name = "Test User" };
            var parameters = new EventSearchParams { Location = "Seattle, WA" };

            var queries = new List<string> { "events Seattle" };
            var webSearchEvents = new List<Event>
            {
                new Event { Id = "evt-1", Title = "Event 1" },
                new Event { Id = "evt-2", Title = "Event 2" },
                new Event { Id = "evt-3", Title = "Event 3" }
            };

            _mockProfileService.Setup(x => x.GetEntityAsync(profileId))
                .ReturnsAsync(profile);
            _mockSearchStrategy.Setup(x => x.GenerateSearchQueries(profile, parameters))
                .Returns(queries);
            _mockSearchStrategy.Setup(x => x.GetCriticalSafetyRequirements(profile))
                .Returns(new List<SafetyRequirement>());
            _mockSearchStrategy.Setup(x => x.GetScoringWeights(profile))
                .Returns(new Dictionary<string, double> { { "Safety", 1.0 } });

            // Set different scores for different events
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(
                    It.Is<Event>(e => e.Id == "evt-1"), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.5);
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(
                    It.Is<Event>(e => e.Id == "evt-2"), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.9);
            _mockSearchStrategy.Setup(x => x.CalculateMatchScore(
                    It.Is<Event>(e => e.Id == "evt-3"), profile, It.IsAny<Dictionary<string, double>>()))
                .Returns(0.7);

            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webSearchEvents);

            // Act
            var result = await _service.DiscoverHybridAsync(profileId, parameters, 10);

            // Assert
            result.Matches[0].Id.Should().Be("evt-2"); // Highest score (0.9)
            result.Matches[1].Id.Should().Be("evt-3"); // Medium score (0.7)
            result.Matches[2].Id.Should().Be("evt-1"); // Lowest score (0.5)
        }

        #endregion
    }
}
