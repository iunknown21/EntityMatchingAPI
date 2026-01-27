using FluentAssertions;
using Microsoft.Extensions.Configuration;
using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Search;
using EntityMatching.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// API-level integration tests that call Azure Functions via HTTP
    ///
    /// These tests require the Azure Functions app to be running:
    /// - Local: Run 'func start' in ProfileMatching.Functions directory
    /// - Azure: Set API_BASE_URL and API_FUNCTION_KEY environment variables
    ///
    /// To run against local Functions:
    ///   1. Terminal 1: cd ProfileMatching.Functions && func start
    ///   2. Terminal 2: dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
    ///
    /// To run against Azure:
    ///   $env:API_BASE_URL = "https://your-app.azurewebsites.net"
    ///   $env:API_FUNCTION_KEY = "your-function-key"
    ///   dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
    /// </summary>
    [Collection("API Integration Tests")]
    public class EntityMatchingApiTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _output;
        private readonly ApiTestHelper _api;
        private readonly IConfiguration _configuration;
        private readonly List<string> _testProfileIds = new();
        private readonly string _testUserId = $"api-test-user-{Guid.NewGuid():N}";

        public EntityMatchingApiTests(ITestOutputHelper output)
        {
            _output = output;

            // Load configuration
            var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: false)
                .AddEnvironmentVariables()
                .Build();

            _api = new ApiTestHelper(_configuration);
        }

        public async Task InitializeAsync()
        {
            _output.WriteLine($"Testing against: {_api.GetBaseUrl()}");

            // Check if API is available
            var isAvailable = await _api.IsApiAvailableAsync();
            if (!isAvailable)
            {
                _output.WriteLine("WARNING: API is not available at {0}", _api.GetBaseUrl());
                _output.WriteLine("Make sure Functions app is running:");
                _output.WriteLine("  Local: cd ProfileMatching.Functions && func start");
                _output.WriteLine("  Azure: Set API_BASE_URL environment variable");
            }
        }

        public async Task DisposeAsync()
        {
            // Check if cleanup should be skipped (useful for inspecting test data)
            var skipCleanup = Environment.GetEnvironmentVariable("SKIP_TEST_CLEANUP");

            if (skipCleanup == "true" || skipCleanup == "1")
            {
                _output.WriteLine($"SKIP_TEST_CLEANUP is set - Keeping {_testProfileIds.Count} test profiles for inspection");
                _output.WriteLine($"PersonEntity IDs: {string.Join(", ", _testProfileIds)}");
                _api?.Dispose();
                return;
            }

            _output.WriteLine($"Cleaning up {_testProfileIds.Count} test profiles...");

            // Clean up test profiles
            foreach (var profileId in _testProfileIds)
            {
                try
                {
                    await _api.DeleteAsync($"/api/v1/profiles/{profileId}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error cleaning up profile {profileId}: {ex.Message}");
                }
            }

            _api?.Dispose();
            _output.WriteLine("Cleanup completed.");
        }

        #region PersonEntity CRUD Tests

        [Fact]
        public async Task Api_CreateProfile_ReturnsCreatedProfile()
        {
            // Arrange
            var newProfile = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "API Test - Hiker");

            // Act
            var response = await _api.PostAsync("/api/v1/profiles", newProfile);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdProfile = await response.Content.ReadFromJsonAsync<PersonEntity>();
            createdProfile.Should().NotBeNull();
            createdProfile!.Name.Should().Be("API Test - Hiker");
            createdProfile.OwnedByUserId.Should().Be(_testUserId);

            // Track for cleanup
            _testProfileIds.Add(createdProfile.Id.ToString());

            _output.WriteLine($"Created profile via API: {createdProfile.Id}");
        }

        [Fact]
        public async Task Api_GetProfile_ReturnsProfile()
        {
            // Arrange - Create a profile first
            var newProfile = TestDataFactory.CreateArtisticIntrovertProfile(_testUserId, "API Test - Artist");
            var createResponse = await _api.PostAsync("/api/v1/profiles", newProfile);
            var createdProfile = await createResponse.Content.ReadFromJsonAsync<PersonEntity>();
            _testProfileIds.Add(createdProfile!.Id.ToString());

            // Act - Get the profile
            var response = await _api.GetAsync($"/api/v1/profiles/{createdProfile.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var retrievedProfile = await response.Content.ReadFromJsonAsync<PersonEntity>();
            retrievedProfile.Should().NotBeNull();
            retrievedProfile!.Id.Should().Be(createdProfile.Id);
            retrievedProfile.Name.Should().Be("API Test - Artist");

            _output.WriteLine($"Retrieved profile via API: {retrievedProfile.Id}");
        }

        [Fact]
        public async Task Api_UpdateProfile_UpdatesProfile()
        {
            // Arrange - Create a profile
            var newProfile = TestDataFactory.CreateTechEnthusiastProfile(_testUserId, "API Test - Original");
            var createResponse = await _api.PostAsync("/api/v1/profiles", newProfile);
            var createdProfile = await createResponse.Content.ReadFromJsonAsync<PersonEntity>();
            _testProfileIds.Add(createdProfile!.Id.ToString());

            // Modify the profile
            createdProfile.Name = "API Test - Updated";
            createdProfile.Description = "Updated bio via API test";

            // Act - Update the profile
            var response = await _api.PutAsync($"/api/v1/profiles/{createdProfile.Id}", createdProfile);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedProfile = await response.Content.ReadFromJsonAsync<PersonEntity>();
            updatedProfile.Should().NotBeNull();
            updatedProfile!.Name.Should().Be("API Test - Updated");
            updatedProfile.Description.Should().Be("Updated bio via API test");

            _output.WriteLine($"Updated profile via API: {updatedProfile.Id}");
        }

        [Fact]
        public async Task Api_GetAllProfiles_ReturnsUserProfiles()
        {
            // Arrange - Create multiple profiles
            var profile1 = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "API Test - PersonEntity 1");
            var profile2 = TestDataFactory.CreateArtisticIntrovertProfile(_testUserId, "API Test - PersonEntity 2");

            var create1 = await _api.PostAsync("/api/v1/profiles", profile1);
            var create2 = await _api.PostAsync("/api/v1/profiles", profile2);

            var created1 = await create1.Content.ReadFromJsonAsync<PersonEntity>();
            var created2 = await create2.Content.ReadFromJsonAsync<PersonEntity>();

            _testProfileIds.Add(created1!.Id.ToString());
            _testProfileIds.Add(created2!.Id.ToString());

            // Act - Get all profiles for user
            var response = await _api.GetAsync("/api/v1/profiles", $"userId={_testUserId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var profiles = await response.Content.ReadFromJsonAsync<List<PersonEntity>>();
            profiles.Should().NotBeNull();
            profiles!.Should().HaveCountGreaterThanOrEqualTo(2);
            profiles.Should().Contain(p => p.Name == "API Test - PersonEntity 1");
            profiles.Should().Contain(p => p.Name == "API Test - PersonEntity 2");

            _output.WriteLine($"Retrieved {profiles.Count} profiles via API for user {_testUserId}");
        }

        [Fact]
        public async Task Api_DeleteProfile_RemovesProfile()
        {
            // Arrange - Create a profile
            var newProfile = TestDataFactory.CreateSocialButterflyProfile(_testUserId, "API Test - ToDelete");
            var createResponse = await _api.PostAsync("/api/v1/profiles", newProfile);
            var createdProfile = await createResponse.Content.ReadFromJsonAsync<PersonEntity>();
            var profileId = createdProfile!.Id.ToString();

            // Act - Delete the profile
            var deleteResponse = await _api.DeleteAsync($"/api/v1/profiles/{profileId}");

            // Assert
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify it's gone
            var getResponse = await _api.GetAsync($"/api/v1/profiles/{profileId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _output.WriteLine($"Deleted profile via API: {profileId}");
        }

        #endregion

        #region End-to-End Workflow Tests

        [Fact]
        public async Task Api_CompleteWorkflow_CreateProfilesAndSearch_FindsMatches()
        {
            // ====== STEP 1: CREATE PROFILES ======
            _output.WriteLine("Step 1: Creating profiles via API...");

            var outdoor1 = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "API Hiker 1");
            var outdoor2 = TestDataFactory.CreateOutdoorAdventureProfile(_testUserId, "API Hiker 2");
            var artist1 = TestDataFactory.CreateArtisticIntrovertProfile(_testUserId, "API Artist 1");
            var tech1 = TestDataFactory.CreateTechEnthusiastProfile(_testUserId, "API Techie 1");

            var profiles = new[] { outdoor1, outdoor2, artist1, tech1 };
            var createdProfiles = new List<PersonEntity>();

            foreach (var profile in profiles)
            {
                var response = await _api.PostAsync("/api/v1/profiles", profile);
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var created = await response.Content.ReadFromJsonAsync<PersonEntity>();
                createdProfiles.Add(created!);
                _testProfileIds.Add(created!.Id.ToString());

                _output.WriteLine($"Created: {created.Name} ({created.Id})");
            }

            // ====== STEP 2: TRIGGER EMBEDDING GENERATION ======
            _output.WriteLine("\nStep 2: Triggering embedding generation via API...");

            // Note: This requires AdminFunctions to be enabled
            var processResponse = await _api.PostAsync("/api/admin/embeddings/process", $"limit=10");

            if (processResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _output.WriteLine("WARNING: /api/admin/embeddings/process not found - AdminFunctions may be disabled");
                _output.WriteLine("Skipping embedding generation and search tests");
                return;
            }

            processResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            _output.WriteLine("Embedding generation triggered successfully");

            // Wait for processing
            await Task.Delay(5000);

            // ====== STEP 3: SEARCH BY QUERY ======
            _output.WriteLine("\nStep 3: Searching for profiles via API...");

            var searchRequest = new SearchRequest
            {
                Query = "loves hiking and outdoor adventures in nature",
                Limit = 5,
                MinSimilarity = 0.0f,
                IncludeEntities = true
            };

            var searchResponse = await _api.PostAsync("/api/v1/profiles/search", searchRequest);

            if (searchResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _output.WriteLine("WARNING: /api/v1/profiles/search not found - SearchFunctions may be disabled");
                _output.WriteLine("Skipping search tests");
                return;
            }

            searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var searchResult = await searchResponse.Content.ReadFromJsonAsync<SearchResult>();
            searchResult.Should().NotBeNull();
            searchResult!.Matches.Should().NotBeEmpty();

            _output.WriteLine($"Found {searchResult.TotalMatches} matches for outdoor query:");
            foreach (var match in searchResult.Matches.Take(3))
            {
                _output.WriteLine($"  - {match.EntityName}: {match.SimilarityScore:F4}");
            }

            // Should find the outdoor profiles
            searchResult.Matches.Should().Contain(m =>
                m.EntityName!.Contains("Hiker"),
                "Search for outdoor activities should match hiking profiles");

            // ====== STEP 4: FIND SIMILAR PROFILES ======
            _output.WriteLine("\nStep 4: Finding similar profiles via API...");

            var outdoorProfileId = createdProfiles.First(p => p.Name!.Contains("Hiker")).Id;
            var similarResponse = await _api.GetAsync(
                $"/api/v1/profiles/{outdoorProfileId}/similar",
                "limit=5&minSimilarity=0.0&includeEntities=true");

            if (similarResponse.StatusCode == HttpStatusCode.NotFound)
            {
                _output.WriteLine("WARNING: /api/v1/profiles/{id}/similar not found - SearchFunctions may be disabled");
                return;
            }

            similarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var similarResult = await similarResponse.Content.ReadFromJsonAsync<SearchResult>();
            similarResult.Should().NotBeNull();
            similarResult!.Matches.Should().NotBeEmpty();

            _output.WriteLine($"Found {similarResult.TotalMatches} similar profiles:");
            foreach (var match in similarResult.Matches.Take(3))
            {
                _output.WriteLine($"  - {match.EntityName}: {match.SimilarityScore:F4}");
            }

            // The other outdoor profile should be the top match
            var topMatch = similarResult.Matches.First();
            topMatch.EntityName.Should().Contain("Hiker",
                "Most similar profile should be the other outdoor enthusiast");

            _output.WriteLine("\n✓ Complete API workflow test passed!");
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Api_GetProfile_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await _api.GetAsync($"/api/v1/profiles/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            _output.WriteLine("GetProfile with invalid ID correctly returned 404");
        }

        [Fact]
        public async Task Api_GetProfiles_WithoutUserId_ReturnsBadRequest()
        {
            // Act
            var response = await _api.GetAsync("/api/v1/profiles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            _output.WriteLine("GetProfiles without userId correctly returned 400");
        }

        [Fact]
        public async Task Api_SearchProfiles_WithEmptyQuery_ReturnsBadRequest()
        {
            // Arrange
            var searchRequest = new SearchRequest
            {
                Query = "",
                Limit = 10
            };

            // Act
            var response = await _api.PostAsync("/api/v1/profiles/search", searchRequest);

            // Skip if SearchFunctions not enabled
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _output.WriteLine("SearchFunctions not enabled - skipping test");
                return;
            }

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            _output.WriteLine("Search with empty query correctly returned 400");
        }

        #endregion
    }
}
