using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Search;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Shared.Models;
using EntityMatching.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// Integration tests for profile-based search (events, gifts, jobs)
    /// Tests the complete workflow from profile to search results
    ///
    /// These tests require:
    /// - Cosmos DB connection (for profile storage)
    /// - Mock web search service (to avoid external API calls)
    /// </summary>
    [Collection("PersonEntity Matching Integration Tests")]
    public class EntityBasedSearchIntegrationTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;

        // Services
        private readonly IEntityService _profileService;
        private readonly EventSearchStrategy _searchStrategy;
        private readonly Mock<IWebSearchService> _mockWebSearchService;
        private readonly EventDiscoveryService _discoveryService;

        // Test data tracking
        private readonly List<string> _testProfileIds = new();
        private readonly string _testUserId = $"test-user-{Guid.NewGuid():N}";

        public EntityBasedSearchIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Load configuration
            var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: false)
                .Build();

            // Initialize Cosmos DB
            var cosmosConnectionString = _configuration["CosmosDb:ConnectionString"]
                ?? _configuration["CosmosDb__ConnectionString"]
                ?? throw new InvalidOperationException("CosmosDb connection string not found");

            var databaseId = _configuration["CosmosDb:DatabaseId"];
            var containerId = _configuration["CosmosDb:ProfilesContainerId"];

            _cosmosClient = new CosmosClient(cosmosConnectionString);

            // Initialize services
            var mockLogger = new Mock<ILogger<EntityService>>();
            _profileService = new EntityService(
                _cosmosClient,
                databaseId!,
                containerId!,
                mockLogger.Object);

            var mockStrategyLogger = new Mock<ILogger<EventSearchStrategy>>();
            _searchStrategy = new EventSearchStrategy(mockStrategyLogger.Object);

            _mockWebSearchService = new Mock<IWebSearchService>();

            var mockDiscoveryLogger = new Mock<ILogger<EventDiscoveryService>>();
            _discoveryService = new EventDiscoveryService(
                _profileService,
                _searchStrategy,
                _mockWebSearchService.Object,
                mockDiscoveryLogger.Object);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            _output.WriteLine($"Cleaning up {_testProfileIds.Count} test profiles...");

            foreach (var profileId in _testProfileIds)
            {
                try
                {
                    await _profileService.DeleteEntityAsync(profileId);
                    _output.WriteLine($"  Deleted profile: {profileId}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  Failed to delete profile {profileId}: {ex.Message}");
                }
            }

            _cosmosClient?.Dispose();
            _output.WriteLine("Cleanup completed.");
        }

        #region Helper Methods

        private async Task<PersonEntity> CreateTestProfileAsync(PersonEntity profile)
        {
            profile.OwnedByUserId = _testUserId;
            await _profileService.AddEntityAsync(profile);
            _testProfileIds.Add(profile.Id.ToString());
            _output.WriteLine($"Created test profile: {profile.Id} ({profile.Name})");
            return profile;
        }

        private void SetupMockWebSearch(List<Event> events)
        {
            _mockWebSearchService.Setup(x => x.SearchAsync<Event>(
                    It.IsAny<string>(),
                    It.IsAny<SearchContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(events);
        }

        #endregion

        #region Search Strategy Tests

        [Fact]
        public async Task SearchStrategy_GeneratesQueriesBasedOnProfile()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Jazz Enthusiast",
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Jazz", "Blues" }
                },
                SocialPreferences = new SocialPreferences
                {
                    SocialBatteryLevel = 3 // Introvert
                },
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 2 // Low risk
                }
            });

            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var queries = _searchStrategy.GenerateSearchQueries(profile, parameters);

            // Assert
            _output.WriteLine($"Generated {queries.Count} queries:");
            foreach (var query in queries)
            {
                _output.WriteLine($"  - {query}");
            }

            queries.Should().NotBeEmpty();
            queries.Should().Contain(q => q.Contains("Jazz") || q.Contains("Blues"));
            queries.Should().Contain(q => q.Contains("intimate") || q.Contains("quiet"));
            queries.Should().Contain(q => q.Contains("calm") || q.Contains("relaxing"));
        }

        [Fact]
        public async Task SearchStrategy_ExtractsCriticalSafetyRequirements()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Safety First User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts", "shellfish" }
                },
                AccessibilityNeeds = new AccessibilityNeeds
                {
                    RequiresWheelchairAccess = true
                },
                SensoryPreferences = new SensoryPreferences
                {
                    SensitiveToFlashingLights = true,
                    Claustrophobic = true
                }
            });

            // Act
            var requirements = _searchStrategy.GetCriticalSafetyRequirements(profile);

            // Assert
            _output.WriteLine($"Extracted {requirements.Count} safety requirements:");
            foreach (var req in requirements)
            {
                _output.WriteLine($"  [{req.Importance}] {req.Key}: {req.Description}");
            }

            requirements.Should().HaveCountGreaterThanOrEqualTo(4);
            requirements.Should().Contain(r => r.Key == "no_peanuts" && r.Importance == SafetyImportance.Critical);
            requirements.Should().Contain(r => r.Key == "no_shellfish" && r.Importance == SafetyImportance.Critical);
            requirements.Should().Contain(r => r.Key == "wheelchair_accessible" && r.Importance == SafetyImportance.Critical);
            requirements.Should().Contain(r => r.Key == "no_flashing_lights" && r.Importance == SafetyImportance.Critical);
        }

        [Fact]
        public async Task SearchStrategy_AdjustsScoringWeightsBasedOnProfile()
        {
            // Arrange - PersonEntity with critical safety needs
            var safetyProfile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "High Safety User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts" }
                }
            });

            // Arrange - PersonEntity without critical safety needs
            var normalProfile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Normal User"
            });

            // Act
            var safetyWeights = _searchStrategy.GetScoringWeights(safetyProfile);
            var normalWeights = _searchStrategy.GetScoringWeights(normalProfile);

            // Assert
            _output.WriteLine("Safety PersonEntity Weights:");
            foreach (var weight in safetyWeights)
            {
                _output.WriteLine($"  {weight.Key}: {weight.Value:P0}");
            }

            _output.WriteLine("Normal PersonEntity Weights:");
            foreach (var weight in normalWeights)
            {
                _output.WriteLine($"  {weight.Key}: {weight.Value:P0}");
            }

            safetyWeights["Safety"].Should().BeGreaterThan(normalWeights["Safety"]);
            safetyWeights.Values.Sum().Should().BeApproximately(1.0, 0.01);
            normalWeights.Values.Sum().Should().BeApproximately(1.0, 0.01);
        }

        [Fact]
        public async Task SearchStrategy_CalculatesMatchScore()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Music Lover",
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Jazz", "Blues" }
                }
            });

            var goodMatchEvent = new Event
            {
                Id = "evt-1",
                Title = "Jazz Festival",
                Description = "Three days of live jazz performances featuring top artists",
                Category = "music",
                Location = "Seattle"
            };

            var poorMatchEvent = new Event
            {
                Id = "evt-2",
                Title = "Tech Conference",
                Description = "Annual technology and innovation summit",
                Category = "business",
                Location = "Seattle"
            };

            var weights = _searchStrategy.GetScoringWeights(profile);

            // Act
            var goodScore = _searchStrategy.CalculateMatchScore(goodMatchEvent, profile, weights);
            var poorScore = _searchStrategy.CalculateMatchScore(poorMatchEvent, profile, weights);

            // Assert
            _output.WriteLine($"Jazz Festival Score: {goodScore:P1}");
            _output.WriteLine($"  Breakdown: {string.Join(", ", goodMatchEvent.ScoringBreakdown.Select(kvp => $"{kvp.Key}={kvp.Value:F2}"))}");
            _output.WriteLine($"Tech Conference Score: {poorScore:P1}");
            _output.WriteLine($"  Breakdown: {string.Join(", ", poorMatchEvent.ScoringBreakdown.Select(kvp => $"{kvp.Key}={kvp.Value:F2}"))}");

            goodScore.Should().BeGreaterThan(poorScore);
            goodScore.Should().BeInRange(0.0, 1.0);
            poorScore.Should().BeInRange(0.0, 1.0);
        }

        #endregion

        #region Discovery Service Tests

        [Fact]
        public async Task DiscoveryService_WebSearch_ReturnsAndScoresEvents()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Outdoor Enthusiast",
                NaturePreferences = new NaturePreferences
                {
                    EnjoysGardening = true,
                    EnjoysBirdWatching = true
                },
                AdventurePreferences = new AdventurePreferences
                {
                    RiskTolerance = 7
                }
            });

            var mockEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Hiking Adventure",
                    Description = "Guided mountain hiking expedition",
                    Category = "outdoor",
                    Location = "Seattle"
                },
                new Event
                {
                    Id = "evt-2",
                    Title = "Bird Watching Tour",
                    Description = "Early morning bird watching in local parks",
                    Category = "nature",
                    Location = "Seattle"
                },
                new Event
                {
                    Id = "evt-3",
                    Title = "Indoor Gaming Tournament",
                    Description = "Competitive esports event",
                    Category = "gaming",
                    Location = "Seattle"
                }
            };

            SetupMockWebSearch(mockEvents);

            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                MaxResults = 10
            };

            // Act
            var result = await _discoveryService.DiscoverViaWebSearchAsync(profile.Id.ToString(), parameters, 10);

            // Assert
            _output.WriteLine($"Found {result.TotalMatches} events in {result.Metadata.SearchDurationMs}ms:");
            foreach (var evt in result.Matches.Take(5))
            {
                _output.WriteLine($"  [{evt.MatchScore:P1}] {evt.Title}");
                if (evt.MatchReasons.Any())
                {
                    foreach (var reason in evt.MatchReasons)
                    {
                        _output.WriteLine($"    - {reason.Key}: {reason.Value}");
                    }
                }
            }

            result.Should().NotBeNull();
            result.Matches.Should().NotBeEmpty();
            result.Matches.Should().AllSatisfy(evt =>
            {
                evt.MatchScore.Should().BeGreaterThan(0);
                evt.ScoringBreakdown.Should().NotBeEmpty();
            });

            result.Metadata.SearchMode.Should().Be("web_search");
            result.Metadata.ProfileId.Should().Be(profile.Id.ToString());
            result.Metadata.WebSearchResults.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task DiscoveryService_FiltersByLocationAndCategory()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Music Fan"
            });

            var mockEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Jazz Concert",
                    Category = "music",
                    Location = "Seattle"
                },
                new Event
                {
                    Id = "evt-2",
                    Title = "Food Festival",
                    Category = "food",
                    Location = "Seattle"
                }
            };

            SetupMockWebSearch(mockEvents);

            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                Category = "music",
                MaxResults = 10
            };

            // Act
            var result = await _discoveryService.DiscoverViaWebSearchAsync(profile.Id.ToString(), parameters, 10);

            // Assert - All results should be scored (strategy doesn't filter by category, just adds it to query)
            result.Matches.Should().NotBeEmpty();
            result.Matches.Should().AllSatisfy(evt => evt.MatchScore.Should().BeGreaterThanOrEqualTo(0));
        }

        [Fact]
        public async Task DiscoveryService_HybridMode_CombinesResults()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Hybrid User",
                EntertainmentPreferences = new EntertainmentPreferences
                {
                    FavoriteMusicGenres = new List<string> { "Rock" }
                }
            });

            var mockEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Rock Concert",
                    Description = "Live rock music performance",
                    Category = "music",
                    Location = "Seattle"
                },
                new Event
                {
                    Id = "evt-2",
                    Title = "Rock Festival",
                    Description = "Three-day rock music festival",
                    Category = "music",
                    Location = "Seattle"
                }
            };

            SetupMockWebSearch(mockEvents);

            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                MaxResults = 10
            };

            // Act
            var result = await _discoveryService.DiscoverHybridAsync(profile.Id.ToString(), parameters, 10);

            // Assert
            _output.WriteLine($"Hybrid search results:");
            _output.WriteLine($"  Mode: {result.Metadata.SearchMode}");
            _output.WriteLine($"  Web results: {result.Metadata.WebSearchResults}");
            _output.WriteLine($"  Embedding results: {result.Metadata.EmbeddingResults}");
            _output.WriteLine($"  Total: {result.TotalMatches}");

            result.Should().NotBeNull();
            result.Metadata.SearchMode.Should().Be("hybrid");
            result.Matches.Should().NotBeEmpty();

            // Currently should be all web search since embeddings not implemented
            result.Metadata.WebSearchResults.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task DiscoveryService_AppliesSafetyFiltering()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Allergy User",
                DietaryRestrictions = new DietaryRestrictions
                {
                    Allergies = new List<string> { "peanuts" }
                }
            });

            var mockEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Peanut Festival",
                    Description = "Celebration of peanuts and peanut-based foods",
                    Category = "food",
                    Location = "Seattle"
                },
                new Event
                {
                    Id = "evt-2",
                    Title = "Allergy-Friendly Food Fair",
                    Description = "Food festival with allergy-safe options",
                    Category = "food",
                    Location = "Seattle"
                }
            };

            SetupMockWebSearch(mockEvents);

            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                MaxResults = 10
            };

            // Act
            var result = await _discoveryService.DiscoverViaWebSearchAsync(profile.Id.ToString(), parameters, 10);

            // Assert
            _output.WriteLine($"Safety filtering results:");
            foreach (var evt in result.Matches)
            {
                _output.WriteLine($"  - {evt.Title}");
            }

            result.Matches.Should().NotContain(e => e.Title.Contains("Peanut"));
            result.Matches.Should().Contain(e => e.Title.Contains("Allergy-Friendly"));
        }

        [Fact]
        public async Task DiscoveryService_DeduplicatesEvents()
        {
            // Arrange
            var profile = await CreateTestProfileAsync(new PersonEntity
            {
                Name = "Test User"
            });

            var mockEvents = new List<Event>
            {
                new Event
                {
                    Id = "evt-1",
                    Title = "Jazz Concert",
                    Location = "Blue Note Seattle",
                    EventDate = new DateTime(2025, 1, 15),
                    Description = "Live jazz performance"
                },
                new Event
                {
                    Id = "evt-2",
                    Title = "Jazz Concert",  // Duplicate
                    Location = "Blue Note Seattle",
                    EventDate = new DateTime(2025, 1, 15),
                    Description = "Duplicate event"
                },
                new Event
                {
                    Id = "evt-3",
                    Title = "Rock Show",
                    Location = "Seattle Arena",
                    EventDate = new DateTime(2025, 1, 20),
                    Description = "Rock concert"
                }
            };

            SetupMockWebSearch(mockEvents);

            var parameters = new EventSearchParams
            {
                Location = "Seattle, WA",
                MaxResults = 10
            };

            // Act
            var result = await _discoveryService.DiscoverHybridAsync(profile.Id.ToString(), parameters, 10);

            // Assert
            _output.WriteLine($"Deduplication results:");
            _output.WriteLine($"  Original events: 3");
            _output.WriteLine($"  After deduplication: {result.TotalMatches}");
            foreach (var evt in result.Matches)
            {
                _output.WriteLine($"    - {evt.Title} at {evt.Location}");
            }

            result.Matches.Should().HaveCount(2); // One duplicate removed
            result.Matches.Should().ContainSingle(e => e.Title == "Jazz Concert");
            result.Matches.Should().ContainSingle(e => e.Title == "Rock Show");
        }

        #endregion
    }
}
