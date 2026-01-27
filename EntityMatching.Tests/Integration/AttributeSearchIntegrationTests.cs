using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using EntityMatching.Shared.Models.Privacy;
using EntityMatching.Core.Models.Search;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// Integration tests for attribute-based search with privacy enforcement
    /// Tests hybrid search: semantic similarity + structured attribute filtering
    /// </summary>
    [Collection("Attribute Search Integration Tests")]
    public class AttributeSearchIntegrationTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;

        // Services
        private readonly IEntityService _profileService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IEmbeddingStorageService _embeddingStorageService;
        private readonly IAttributeFilterService _attributeFilterService;
        private readonly ISimilaritySearchService _similaritySearchService;

        // Test data tracking
        private readonly List<string> _testProfileIds = new();
        private readonly string _testUserId = $"attr-test-user-{Guid.NewGuid():N}";

        public AttributeSearchIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Load configuration
            var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: false)
                .Build();

            var cosmosConnectionString = _configuration["CosmosDb:ConnectionString"];
            var openAiKey = _configuration["OpenAI:ApiKey"];

            if (string.IsNullOrEmpty(cosmosConnectionString) || cosmosConnectionString.Contains("YOUR_"))
            {
                throw new InvalidOperationException("Cosmos DB connection string not configured");
            }

            if (string.IsNullOrEmpty(openAiKey) || openAiKey.Contains("YOUR_"))
            {
                throw new InvalidOperationException("OpenAI API key not configured");
            }

            // Initialize Cosmos client
            _cosmosClient = new CosmosClient(cosmosConnectionString);

            // Create service instances
            var profileLogger = new Mock<ILogger<EntityService>>().Object;
            var embeddingLogger = new Mock<ILogger<OpenAIEmbeddingService>>().Object;
            var storageLogger = new Mock<ILogger<EmbeddingStorageService>>().Object;
            var filterLogger = new Mock<ILogger<AttributeFilterService>>().Object;
            var searchLogger = new Mock<ILogger<SimilaritySearchService>>().Object;

            var databaseId = _configuration["CosmosDb:DatabaseId"] ?? "ProfileMatchingTestDB";
            var profilesContainerId = _configuration["CosmosDb:ProfilesContainerId"] ?? "profiles";

            _profileService = new EntityService(_cosmosClient, databaseId, profilesContainerId, profileLogger);
            _embeddingService = new OpenAIEmbeddingService(_configuration, embeddingLogger);
            _embeddingStorageService = new EmbeddingStorageService(_cosmosClient, _configuration, storageLogger);
            _attributeFilterService = new AttributeFilterService(filterLogger);
            _similaritySearchService = new SimilaritySearchService(
                _embeddingStorageService,
                _embeddingService,
                _profileService,
                _attributeFilterService,
                searchLogger);
        }

        public async Task InitializeAsync()
        {
            _output.WriteLine("Initializing attribute search test environment...");
            await _profileService.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            var skipCleanup = Environment.GetEnvironmentVariable("SKIP_TEST_CLEANUP");

            if (skipCleanup == "true" || skipCleanup == "1")
            {
                _output.WriteLine($"SKIP_TEST_CLEANUP is set - Keeping {_testProfileIds.Count} test profiles");
                _output.WriteLine($"PersonEntity IDs: {string.Join(", ", _testProfileIds)}");
                _cosmosClient?.Dispose();
                return;
            }

            _output.WriteLine($"Cleaning up {_testProfileIds.Count} test profiles...");

            foreach (var profileId in _testProfileIds)
            {
                try
                {
                    await _profileService.DeleteEntityAsync(profileId);
                    await _embeddingStorageService.DeleteEmbeddingAsync(profileId);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error cleaning up profile {profileId}: {ex.Message}");
                }
            }

            _cosmosClient?.Dispose();
            _output.WriteLine("Cleanup completed.");
        }

        #region Hybrid Search Tests (Semantic + Attribute Filtering)

        [Fact]
        public async Task HybridSearch_SemanticAndAttributeFilters_FindsCorrectMatch()
        {
            // Arrange - Create diverse profiles
            _output.WriteLine("Creating test profiles with diverse attributes...");

            // PersonEntity 1: Male, has dog, outdoor enthusiast
            var outdoor1 = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Outdoor Male 1");
            outdoor1.NaturePreferences.HasPets = true;
            outdoor1.NaturePreferences.PetTypes = new List<string> { "Dog" };
            outdoor1.LearningPreferences.SubjectsOfInterest = new List<string> { "English", "History" };
            outdoor1.Preferences.FavoriteCuisines = new List<string> { "American", "Italian" };
            outdoor1.PrivacySettings.SetFieldVisibility("naturePreferences.hasPets", FieldVisibility.Public);
            outdoor1.PrivacySettings.SetFieldVisibility("naturePreferences.petTypes", FieldVisibility.Public);
            outdoor1.PrivacySettings.SetFieldVisibility("learningPreferences.subjectsOfInterest", FieldVisibility.Public);
            outdoor1.PrivacySettings.SetFieldVisibility("preferences.favoriteCuisines", FieldVisibility.Public);

            // PersonEntity 2: Female, has cat, outdoor enthusiast (should NOT match - wrong gender and pet)
            var outdoor2 = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Outdoor Female 1");
            outdoor2.NaturePreferences.HasPets = true;
            outdoor2.NaturePreferences.PetTypes = new List<string> { "Cat" };
            outdoor2.PrivacySettings.SetFieldVisibility("naturePreferences.hasPets", FieldVisibility.Public);
            outdoor2.PrivacySettings.SetFieldVisibility("naturePreferences.petTypes", FieldVisibility.Public);

            // PersonEntity 3: Male, no pets, tech enthusiast (should NOT match - no pets, not outdoor)
            var tech1 = TestDataFactory.CreateTechEnthusiastProfile(_testUserId, "Tech Male 1");
            tech1.NaturePreferences.HasPets = false;

            // Save profiles
            await _profileService.AddEntityAsync(outdoor1);
            await _profileService.AddEntityAsync(outdoor2);
            await _profileService.AddEntityAsync(tech1);

            _testProfileIds.Add(outdoor1.Id.ToString());
            _testProfileIds.Add(outdoor2.Id.ToString());
            _testProfileIds.Add(tech1.Id.ToString());

            _output.WriteLine($"Created 3 test profiles");

            // Generate embeddings
            _output.WriteLine("Generating embeddings...");
            foreach (var profile in new[] { outdoor1, outdoor2, tech1 })
            {
                var summary = $"{profile.Name}. {profile.Description}";
                var vector = await _embeddingService.GenerateEmbeddingAsync(summary);

                var embedding = new EntityEmbedding
                {
                    Id = EntityEmbedding.GenerateId(profile.Id.ToString()),
                EntityId = profile.Id.ToString(),
                EntitySummary = summary,
                    Embedding = vector,
                    Dimensions = vector.Length,
                    EmbeddingModel = _embeddingService.ModelName,
                    Status = EmbeddingStatus.Generated
                };

                await _embeddingStorageService.UpsertEmbeddingAsync(embedding);
            }

            _output.WriteLine("Embeddings generated successfully");

            // Wait for Cosmos DB consistency
            await Task.Delay(5000);

            // Act - Search with semantic query + attribute filters
            _output.WriteLine("Executing hybrid search...");

            var searchRequest = new SearchRequest
            {
                Query = "loves hiking and outdoor adventures in nature",
                AttributeFilters = new FilterGroup
                {
                    LogicalOperator = LogicalOperator.And,
                    Filters = new List<AttributeFilter>
                    {
                        new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue },
                        new AttributeFilter { FieldPath = "naturePreferences.petTypes", Operator = FilterOperator.Contains, Value = "Dog" },
                        new AttributeFilter { FieldPath = "learningPreferences.subjectsOfInterest", Operator = FilterOperator.Contains, Value = "English" },
                        new AttributeFilter { FieldPath = "preferences.favoriteCuisines", Operator = FilterOperator.Contains, Value = "American" }
                    }
                },
                RequestingUserId = null, // Anonymous
                EnforcePrivacy = true,
                Limit = 10,
                MinSimilarity = 0.0f
            };

            var result = await _similaritySearchService.SearchByQueryAsync(
                searchRequest.Query,
                searchRequest.Limit ?? 10,
                searchRequest.MinSimilarity ?? 0.0f,
                includeEntities: false,
                searchRequest.AttributeFilters,
                metadataFilters: null,
                searchRequest.RequestingUserId,
                searchRequest.EnforcePrivacy);

            // Assert
            _output.WriteLine($"Search returned {result.TotalMatches} matches");

            result.Should().NotBeNull();
            result.Matches.Should().HaveCount(1, "only outdoor1 matches all criteria");

            var match = result.Matches.First();
            match.EntityId.Should().Be(outdoor1.Id.ToString());
            match.MatchedAttributes.Should().NotBeNull();
            match.MatchedAttributes!.Should().ContainKey("naturePreferences.hasPets");
            match.MatchedAttributes.Should().ContainKey("naturePreferences.petTypes");
            match.MatchedAttributes.Should().ContainKey("learningPreferences.subjectsOfInterest");
            match.MatchedAttributes.Should().ContainKey("preferences.favoriteCuisines");

            _output.WriteLine($"✓ Matched profile: {match.EntityName} (similarity: {match.SimilarityScore:F4})");
            _output.WriteLine($"  Matched attributes: {match.MatchedAttributes.Count}");
        }

        #endregion

        #region Privacy Enforcement Tests

        [Fact]
        public async Task AttributeSearch_PrivateField_NotSearchableByAnonymous()
        {
            // Arrange
            var profile = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Private PersonEntity");
            profile.Birthday = new DateTime(1990, 1, 1);

            // Make birthday PRIVATE
            profile.PrivacySettings.SetFieldVisibility("birthday", FieldVisibility.Private);

            await _profileService.AddEntityAsync(profile);
            _testProfileIds.Add(profile.Id.ToString());

            // Generate embedding
            var summary = $"{profile.Name}. {profile.Description}";
            var vector = await _embeddingService.GenerateEmbeddingAsync(summary);
            var embedding = new EntityEmbedding
            {
                Id = Guid.NewGuid().ToString(),
                EntityId = profile.Id.ToString(),
                EntitySummary = summary,
                Embedding = vector,
                Dimensions = vector.Length,
                EmbeddingModel = _embeddingService.ModelName,
                Status = EmbeddingStatus.Generated
            };
            await _embeddingStorageService.UpsertEmbeddingAsync(embedding);

            // Act - Search with filter on private field (anonymous user)
            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "birthday", Operator = FilterOperator.Exists }
                }
            };

            var result = await _similaritySearchService.SearchByQueryAsync(
                "outdoor enthusiast",
                limit: 10,
                minSimilarity: 0.0f,
                includeEntities: false,
                attributeFilters: filterGroup,
                metadataFilters: null,
                requestingUserId: null, // Anonymous
                enforcePrivacy: true);

            // Assert
            result.Matches.Should().BeEmpty("birthday is private and user is anonymous - fail-closed");
            _output.WriteLine("✓ Private field correctly blocked for anonymous user");
        }

        [Fact]
        public async Task AttributeSearch_PublicField_SearchableByAnonymous()
        {
            // Arrange
            var profile = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Public PersonEntity");
            profile.NaturePreferences.HasPets = true;

            // Make hasPets PUBLIC
            profile.PrivacySettings.SetFieldVisibility("naturePreferences.hasPets", FieldVisibility.Public);

            // Also need to make basic fields public for the profile to be searchable
            profile.PrivacySettings.SetFieldVisibility("name", FieldVisibility.Public);
            profile.PrivacySettings.SetFieldVisibility("bio", FieldVisibility.Public);

            await _profileService.AddEntityAsync(profile);
            _testProfileIds.Add(profile.Id.ToString());

            // Generate embedding
            var summary = $"{profile.Name}. {profile.Description}";
            var vector = await _embeddingService.GenerateEmbeddingAsync(summary);
            var embedding = new EntityEmbedding
            {
                Id = Guid.NewGuid().ToString(),
                EntityId = profile.Id.ToString(),
                EntitySummary = summary,
                Embedding = vector,
                Dimensions = vector.Length,
                EmbeddingModel = _embeddingService.ModelName,
                Status = EmbeddingStatus.Generated
            };
            await _embeddingStorageService.UpsertEmbeddingAsync(embedding);

            // Wait for Cosmos DB consistency
            await Task.Delay(5000);

            // Act - Search with filter on public field (anonymous user)
            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue }
                }
            };

            var result = await _similaritySearchService.SearchByQueryAsync(
                "outdoor enthusiast",
                limit: 10,
                minSimilarity: 0.0f,
                includeEntities: false,
                attributeFilters: filterGroup,
                metadataFilters: null,
                requestingUserId: null, // Anonymous
                enforcePrivacy: true);

            // Assert
            result.Matches.Should().NotBeEmpty("public field is searchable by anonymous users");
            result.Matches.First().EntityId.Should().Be(profile.Id.ToString());
            _output.WriteLine("✓ Public field correctly accessible to anonymous user");
        }

        [Fact]
        public async Task AttributeSearch_IsSearchableFalse_ProfileNotReturned()
        {
            // Arrange
            var profile = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Non-Searchable PersonEntity");
            profile.IsSearchable = false; // PersonEntity owner disabled search
            profile.NaturePreferences.HasPets = true;
            profile.PrivacySettings.SetFieldVisibility("naturePreferences.hasPets", FieldVisibility.Public);

            await _profileService.AddEntityAsync(profile);
            _testProfileIds.Add(profile.Id.ToString());

            // Generate embedding
            var summary = $"{profile.Name}. {profile.Description}";
            var vector = await _embeddingService.GenerateEmbeddingAsync(summary);
            var embedding = new EntityEmbedding
            {
                Id = Guid.NewGuid().ToString(),
                EntityId = profile.Id.ToString(),
                EntitySummary = summary,
                Embedding = vector,
                Dimensions = vector.Length,
                EmbeddingModel = _embeddingService.ModelName,
                Status = EmbeddingStatus.Generated
            };
            await _embeddingStorageService.UpsertEmbeddingAsync(embedding);

            // Act
            var filterGroup = new FilterGroup
            {
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue }
                }
            };

            var result = await _similaritySearchService.SearchByQueryAsync(
                "outdoor enthusiast",
                limit: 10,
                minSimilarity: 0.0f,
                includeEntities: false,
                attributeFilters: filterGroup,
                metadataFilters: null,
                requestingUserId: null,
                enforcePrivacy: true);

            // Assert
            result.Matches.Should().BeEmpty("profile.IsSearchable is false");
            _output.WriteLine("✓ Non-searchable profile correctly excluded from results");
        }

        #endregion

        #region Complex Filter Logic Tests

        [Fact]
        public async Task AttributeSearch_OrLogic_ReturnsMultipleMatches()
        {
            // Arrange - Create profiles with different attributes
            var dogOwner = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Dog Owner");
            dogOwner.NaturePreferences.HasPets = true;
            dogOwner.NaturePreferences.PetTypes = new List<string> { "Dog" };
            dogOwner.PrivacySettings.SetFieldVisibility("naturePreferences.petTypes", FieldVisibility.Public);

            var catOwner = TestDataFactory.CreateArtisticIntrovertProfile(_testUserId, "Cat Owner");
            catOwner.NaturePreferences.HasPets = true;
            catOwner.NaturePreferences.PetTypes = new List<string> { "Cat" };
            catOwner.PrivacySettings.SetFieldVisibility("naturePreferences.petTypes", FieldVisibility.Public);

            await _profileService.AddEntityAsync(dogOwner);
            await _profileService.AddEntityAsync(catOwner);

            _testProfileIds.Add(dogOwner.Id.ToString());
            _testProfileIds.Add(catOwner.Id.ToString());

            // Generate embeddings
            foreach (var profile in new[] { dogOwner, catOwner })
            {
                var summary = $"{profile.Name}. {profile.Description}";
                var vector = await _embeddingService.GenerateEmbeddingAsync(summary);
                var embedding = new EntityEmbedding
                {
                    Id = EntityEmbedding.GenerateId(profile.Id.ToString()),
                EntityId = profile.Id.ToString(),
                EntitySummary = summary,
                    Embedding = vector,
                    Dimensions = vector.Length,
                    EmbeddingModel = _embeddingService.ModelName,
                    Status = EmbeddingStatus.Generated
                };
                await _embeddingStorageService.UpsertEmbeddingAsync(embedding);
            }

            // Wait for Cosmos DB consistency
            await Task.Delay(5000);

            // Act - Search with OR logic (Dog OR Cat)
            var filterGroup = new FilterGroup
            {
                LogicalOperator = LogicalOperator.Or,
                Filters = new List<AttributeFilter>
                {
                    new AttributeFilter { FieldPath = "naturePreferences.petTypes", Operator = FilterOperator.Contains, Value = "Dog" },
                    new AttributeFilter { FieldPath = "naturePreferences.petTypes", Operator = FilterOperator.Contains, Value = "Cat" }
                }
            };

            var result = await _similaritySearchService.SearchByQueryAsync(
                "pet lover",
                limit: 10,
                minSimilarity: 0.0f,
                includeEntities: false,
                attributeFilters: filterGroup,
                metadataFilters: null,
                requestingUserId: null,
                enforcePrivacy: true);

            // Assert
            result.Matches.Should().HaveCount(2, "both dog and cat owners should match (OR logic)");
            _output.WriteLine($"✓ OR logic correctly returned {result.Matches.Count} matches");
        }

        #endregion
    }
}
