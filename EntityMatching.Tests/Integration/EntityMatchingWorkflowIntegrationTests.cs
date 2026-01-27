using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// End-to-end integration tests for the complete profile matching workflow:
    /// 1. Create profiles with diverse characteristics
    /// 2. Generate summaries for each profile
    /// 3. Generate embeddings using OpenAI
    /// 4. Run similarity searches to find matching profiles
    ///
    /// These tests require:
    /// - Cosmos DB connection (for profile and embedding storage)
    /// - OpenAI API key (for embedding generation)
    /// </summary>
    [Collection("PersonEntity Matching Integration Tests")]
    public class EntityMatchingWorkflowIntegrationTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;

        // Services
        private readonly IEntityService _profileService;
        private readonly IEntitySummaryService _profileSummaryService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IEmbeddingStorageService _embeddingStorageService;
        private readonly ISimilaritySearchService _similaritySearchService;

        // Test data tracking
        private readonly List<string> _testProfileIds = new();
        private readonly string _testUserId = $"test-user-{Guid.NewGuid():N}";

        public EntityMatchingWorkflowIntegrationTests(ITestOutputHelper output)
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
                throw new InvalidOperationException(
                    "Cosmos DB connection string not configured in testsettings.json");
            }

            if (string.IsNullOrEmpty(openAiKey) || openAiKey.Contains("YOUR_"))
            {
                throw new InvalidOperationException(
                    "OpenAI API key not configured in testsettings.json");
            }

            // Initialize Cosmos client
            _cosmosClient = new CosmosClient(cosmosConnectionString);

            // Create service instances with real implementations
            var profileLogger = new Mock<ILogger<EntityService>>().Object;
            var summaryLogger = new Mock<ILogger<EntitySummaryService>>().Object;
            var embeddingLogger = new Mock<ILogger<OpenAIEmbeddingService>>().Object;
            var storageLogger = new Mock<ILogger<EmbeddingStorageService>>().Object;
            var filterLogger = new Mock<ILogger<AttributeFilterService>>().Object;
            var searchLogger = new Mock<ILogger<SimilaritySearchService>>().Object;

            var databaseId = _configuration["CosmosDb:DatabaseId"] ?? "ProfileMatchingTestDB";
            var profilesContainerId = _configuration["CosmosDb:ProfilesContainerId"] ?? "profiles";

            _profileService = new EntityService(_cosmosClient, databaseId, profilesContainerId, profileLogger);
            _profileSummaryService = new EntitySummaryService(summaryLogger, new List<IEntitySummaryStrategy>());
            _embeddingService = new OpenAIEmbeddingService(_configuration, embeddingLogger);
            _embeddingStorageService = new EmbeddingStorageService(_cosmosClient, _configuration, storageLogger);
            var attributeFilterService = new AttributeFilterService(filterLogger);
            _similaritySearchService = new SimilaritySearchService(
                _embeddingStorageService,
                _embeddingService,
                _profileService,
                attributeFilterService,
                searchLogger);
        }

        public async Task InitializeAsync()
        {
            _output.WriteLine("Initializing test environment...");
            await _profileService.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            // Check if cleanup should be skipped (useful for inspecting test data)
            var skipCleanup = Environment.GetEnvironmentVariable("SKIP_TEST_CLEANUP");

            if (skipCleanup == "true" || skipCleanup == "1")
            {
                _output.WriteLine($"SKIP_TEST_CLEANUP is set - Keeping {_testProfileIds.Count} test profiles for inspection");
                _output.WriteLine($"PersonEntity IDs: {string.Join(", ", _testProfileIds)}");
                _cosmosClient?.Dispose();
                return;
            }

            _output.WriteLine($"Cleaning up {_testProfileIds.Count} test profiles...");

            // Clean up test profiles
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

        #region End-to-End Workflow Tests

        [Fact]
        public async Task CompleteWorkflow_CreateProfilesGenerateEmbeddingsAndSearch_FindsSimilarProfiles()
        {
            // ====== STEP 1: CREATE DIVERSE PROFILES ======
            _output.WriteLine("Step 1: Creating diverse test profiles...");

            var outdoorProfile1 = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Alex Hiker");
            var outdoorProfile2 = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "Sam Climber");
            var artistProfile1 = TestDataFactory.CreateArtisticIntrovertProfile(_testUserId, "Morgan Artist");
            var artistProfile2 = TestDataFactory.CreateArtisticIntrovertProfile(_testUserId, "River Poet");
            var techProfile = TestDataFactory.CreateTechEnthusiastProfile(_testUserId, "Casey Coder");
            var socialProfile = TestDataFactory.CreateSocialButterflyProfile(_testUserId, "Jordan Party");
            var wellnessProfile = TestDataFactory.CreateHealthWellnessProfile(_testUserId, "Taylor Zen");

            var profiles = new[]
            {
                outdoorProfile1, outdoorProfile2,
                artistProfile1, artistProfile2,
                techProfile, socialProfile, wellnessProfile
            };

            foreach (var profile in profiles)
            {
                await _profileService.AddEntityAsync(profile);
                _testProfileIds.Add(profile.Id.ToString());
                _output.WriteLine($"Created profile: {profile.Name} ({profile.Id})");
            }

            _output.WriteLine($"Created {profiles.Length} test profiles.");

            // ====== STEP 2: GENERATE SUMMARIES ======
            _output.WriteLine("\nStep 2: Generating profile summaries...");

            var summaries = new Dictionary<string, string>();
            foreach (var profile in profiles)
            {
                var summaryResult = await _profileSummaryService.GenerateSummaryAsync(profile);
                summaries[profile.Id.ToString()] = summaryResult.Summary;
                _output.WriteLine($"Generated summary for {profile.Name} ({summaryResult.Metadata.SummaryWordCount} words)");
            }

            // ====== STEP 3: GENERATE EMBEDDINGS ======
            _output.WriteLine("\nStep 3: Generating embeddings with OpenAI...");

            foreach (var profile in profiles)
            {
                var profileId = profile.Id.ToString();
                var summary = summaries[profileId];

                // Generate embedding vector
                var embeddingVector = await _embeddingService.GenerateEmbeddingAsync(summary);
                embeddingVector.Should().NotBeNull();
                embeddingVector!.Length.Should().Be(1536);

                // Store embedding
                var embedding = new EntityEmbedding
                {
                    Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = summary,
                    SummaryHash = EntityEmbedding.ComputeHash(summary),
                EntityLastModified = profile.LastModified,
                    GeneratedAt = DateTime.UtcNow,
                    Status = EmbeddingStatus.Generated,
                    Embedding = embeddingVector,
                    Dimensions = embeddingVector.Length,
                    EmbeddingModel = _embeddingService.ModelName,
                    SummaryMetadata = new SummaryMetadata
                    {
                        SummaryWordCount = summary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                        PreferenceCategories = new List<string> { "Entertainment", "Adventure", "Style" },
                        HasPersonalityData = true,
                        HasConversationData = false
                    }
                };

                await _embeddingStorageService.UpsertEmbeddingAsync(embedding);
                _output.WriteLine($"Generated and stored embedding for {profile.Name} ({embeddingVector.Length} dimensions)");
            }

            // Small delay to ensure all embeddings are indexed
            await Task.Delay(1000);

            // ====== STEP 4: SEARCH FOR SIMILAR PROFILES ======
            _output.WriteLine("\nStep 4: Running similarity searches...");

            // Test 1: Find profiles similar to first outdoor enthusiast
            _output.WriteLine("\n--- Test 1: Find profiles similar to Alex Hiker (Outdoor Enthusiast) ---");
            var outdoorMatches = await _similaritySearchService.FindSimilarEntitiesAsync(
                outdoorProfile1.Id.ToString(),
                limit: 5,
                minSimilarity: 0.5f,
                includeEntities: true);

            _output.WriteLine($"Found {outdoorMatches.TotalMatches} matches for {outdoorProfile1.Name}");
            foreach (var match in outdoorMatches.Matches)
            {
                _output.WriteLine($"  - {match.EntityName}: {match.SimilarityScore:F4}");
            }

            // Assertions for outdoor profile matches
            outdoorMatches.TotalMatches.Should().BeGreaterThan(0);
            outdoorMatches.Matches.Should().Contain(m => m.EntityId == outdoorProfile2.Id.ToString(),
                "Sam Climber should match Alex Hiker as both are outdoor enthusiasts");

            var bestMatch = outdoorMatches.Matches.First();
            bestMatch.EntityId.Should().Be(outdoorProfile2.Id.ToString(),
                "Sam Climber should be the top match for Alex Hiker");
            bestMatch.SimilarityScore.Should().BeGreaterThan(0.75f,
                "Very similar profiles should have high similarity scores (>0.75)");

            // Test 2: Find profiles similar to first artist
            _output.WriteLine("\n--- Test 2: Find profiles similar to Morgan Artist (Artistic Introvert) ---");
            var artistMatches = await _similaritySearchService.FindSimilarEntitiesAsync(
                artistProfile1.Id.ToString(),
                limit: 5,
                minSimilarity: 0.5f,
                includeEntities: true);

            _output.WriteLine($"Found {artistMatches.TotalMatches} matches for {artistProfile1.Name}");
            foreach (var match in artistMatches.Matches)
            {
                _output.WriteLine($"  - {match.EntityName}: {match.SimilarityScore:F4}");
            }

            // Assertions for artist profile matches
            artistMatches.TotalMatches.Should().BeGreaterThan(0);
            artistMatches.Matches.Should().Contain(m => m.EntityId == artistProfile2.Id.ToString(),
                "River Poet should match Morgan Artist as both are artistic introverts");

            var artistBestMatch = artistMatches.Matches.First();
            artistBestMatch.EntityId.Should().Be(artistProfile2.Id.ToString(),
                "River Poet should be the top match for Morgan Artist");

            // Test 3: Text-based query search
            _output.WriteLine("\n--- Test 3: Search with text query 'loves hiking and outdoor adventures' ---");
            var queryMatches = await _similaritySearchService.SearchByQueryAsync(
                "loves hiking and outdoor adventures in nature",
                limit: 5,
                minSimilarity: 0.5f,
                includeEntities: true);

            _output.WriteLine($"Found {queryMatches.TotalMatches} matches for query");
            foreach (var match in queryMatches.Matches)
            {
                _output.WriteLine($"  - {match.EntityName}: {match.SimilarityScore:F4}");
            }

            // Assertions for query-based search
            queryMatches.TotalMatches.Should().BeGreaterThan(0);
            queryMatches.Matches.Should().Contain(m =>
                m.EntityId == outdoorProfile1.Id.ToString() || m.EntityId == outdoorProfile2.Id.ToString(),
                "Query about outdoor adventures should match outdoor enthusiast profiles");

            // Test 4: Verify dissimilar profiles don't match well
            _output.WriteLine("\n--- Test 4: Verify outdoor profile doesn't strongly match artistic profile ---");
            var crossTypeMatches = await _similaritySearchService.FindSimilarEntitiesAsync(
                outdoorProfile1.Id.ToString(),
                limit: 10,
                minSimilarity: 0.0f,
                includeEntities: true);

            var artistMatch = crossTypeMatches.Matches.FirstOrDefault(m => m.EntityId == artistProfile1.Id.ToString());
            if (artistMatch != null)
            {
                _output.WriteLine($"Outdoor enthusiast vs Artist similarity: {artistMatch.SimilarityScore:F4}");
                artistMatch.SimilarityScore.Should().BeLessThan(bestMatch.SimilarityScore,
                    "Dissimilar profile types should have lower similarity scores than very similar profiles");
            }

            // ====== STEP 5: VERIFY METADATA ======
            _output.WriteLine("\n--- Step 5: Verifying search metadata ---");

            outdoorMatches.Metadata.Should().NotBeNull();
            outdoorMatches.Metadata.TotalEmbeddingsSearched.Should().Be(profiles.Length - 1,
                "Should search all embeddings except the reference profile");
            outdoorMatches.Metadata.SearchDurationMs.Should().BeGreaterThan(0);

            _output.WriteLine($"Search duration: {outdoorMatches.Metadata.SearchDurationMs}ms");
            _output.WriteLine($"Embeddings searched: {outdoorMatches.Metadata.TotalEmbeddingsSearched}");

            _output.WriteLine("\n✓ Complete workflow test passed!");
        }

        [Fact]
        public async Task SearchByQuery_WithDifferentQueries_ReturnsRelevantProfiles()
        {
            // ====== SETUP: CREATE AND EMBED PROFILES ======
            _output.WriteLine("Setting up test profiles...");

            var techProfile = TestDataFactory.CreateTechEnthusiastProfile(_testUserId, "Tech Person");
            var artistProfile = TestDataFactory.CreateArtisticIntrovertProfile(_testUserId, "Artist Person");
            var wellnessProfile = TestDataFactory.CreateHealthWellnessProfile(_testUserId, "Wellness Person");

            var profiles = new[] { techProfile, artistProfile, wellnessProfile };

            foreach (var profile in profiles)
            {
                await _profileService.AddEntityAsync(profile);
                _testProfileIds.Add(profile.Id.ToString());

                // Generate summary and embedding
                var summaryResult = await _profileSummaryService.GenerateSummaryAsync(profile);
                var embeddingVector = await _embeddingService.GenerateEmbeddingAsync(summaryResult.Summary);

                var embedding = new EntityEmbedding
                {
                    Id = EntityEmbedding.GenerateId(profile.Id.ToString()),
                EntityId = profile.Id.ToString(),
                EntitySummary = summaryResult.Summary,
                    SummaryHash = EntityEmbedding.ComputeHash(summaryResult.Summary),
                EntityLastModified = profile.LastModified,
                    GeneratedAt = DateTime.UtcNow,
                    Status = EmbeddingStatus.Generated,
                    Embedding = embeddingVector,
                    Dimensions = embeddingVector!.Length,
                    EmbeddingModel = _embeddingService.ModelName
                };

                await _embeddingStorageService.UpsertEmbeddingAsync(embedding);
                _output.WriteLine($"Created and embedded: {profile.Name}");
            }

            await Task.Delay(500); // Allow indexing

            // ====== TEST QUERIES ======

            // Query 1: Technology-related
            _output.WriteLine("\n--- Query 1: 'programming and software development' ---");
            var techResults = await _similaritySearchService.SearchByQueryAsync(
                "programming and software development with artificial intelligence",
                limit: 3,
                minSimilarity: 0.0f,
                includeEntities: true);

            foreach (var match in techResults.Matches)
            {
                _output.WriteLine($"  {match.EntityName}: {match.SimilarityScore:F4}");
            }

            techResults.Matches.First().EntityId.Should().Be(techProfile.Id.ToString(),
                "Tech-related query should rank tech profile highest");

            // Query 2: Art and creativity
            _output.WriteLine("\n--- Query 2: 'art, music, and creative expression' ---");
            var artResults = await _similaritySearchService.SearchByQueryAsync(
                "art, music, and creative expression through visual media",
                limit: 3,
                minSimilarity: 0.0f,
                includeEntities: true);

            foreach (var match in artResults.Matches)
            {
                _output.WriteLine($"  {match.EntityName}: {match.SimilarityScore:F4}");
            }

            artResults.Matches.First().EntityId.Should().Be(artistProfile.Id.ToString(),
                "Art-related query should rank artist profile highest");

            // Query 3: Health and wellness
            _output.WriteLine("\n--- Query 3: 'yoga, meditation, and healthy lifestyle' ---");
            var wellnessResults = await _similaritySearchService.SearchByQueryAsync(
                "yoga, meditation, and healthy lifestyle with nutrition",
                limit: 3,
                minSimilarity: 0.0f,
                includeEntities: true);

            foreach (var match in wellnessResults.Matches)
            {
                _output.WriteLine($"  {match.EntityName}: {match.SimilarityScore:F4}");
            }

            wellnessResults.Matches.First().EntityId.Should().Be(wellnessProfile.Id.ToString(),
                "Wellness-related query should rank wellness profile highest");

            _output.WriteLine("\n✓ Query-based search test passed!");
        }

        #endregion

        #region Helper Methods

        private async Task<EntityEmbedding> CreateAndStoreEmbedding(PersonEntity profile, string summary)
        {
            var embeddingVector = await _embeddingService.GenerateEmbeddingAsync(summary);

            var embedding = new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profile.Id.ToString()),
                EntityId = profile.Id.ToString(),
                EntitySummary = summary,
                SummaryHash = EntityEmbedding.ComputeHash(summary),
                EntityLastModified = profile.LastModified,
                GeneratedAt = DateTime.UtcNow,
                Status = EmbeddingStatus.Generated,
                Embedding = embeddingVector,
                Dimensions = embeddingVector!.Length,
                EmbeddingModel = _embeddingService.ModelName
            };

            await _embeddingStorageService.UpsertEmbeddingAsync(embedding);
            return embedding;
        }

        #endregion
    }
}
