using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Shared.Models;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Tests.Helpers;
using Xunit;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// Integration tests for EntityService using real Cosmos DB
    /// These tests require a valid Cosmos DB connection string in configuration
    /// </summary>
    [Collection("Cosmos DB Integration Tests")]
    public class EntityServiceIntegrationTests : IAsyncLifetime
    {
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;
        private readonly EntityService _profileService;
        private readonly Mock<ILogger<EntityService>> _mockLogger;
        private readonly string _testUserId = $"test-user-{Guid.NewGuid():N}";

        public EntityServiceIntegrationTests()
        {
            // Load configuration from testsettings.json
            var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: false)
                .Build();

            var connectionString = _configuration["CosmosDb:ConnectionString"];
            var databaseId = _configuration["CosmosDb:DatabaseId"];
            var containerId = _configuration["CosmosDb:ProfilesContainerId"];

            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("YOUR_"))
            {
                throw new InvalidOperationException(
                    "Cosmos DB connection string not configured in local.settings.json. " +
                    "Integration tests require a real Cosmos DB instance.");
            }

            _cosmosClient = new CosmosClient(connectionString);
            _mockLogger = new Mock<ILogger<EntityService>>();

            _profileService = new EntityService(
                _cosmosClient,
                databaseId!,
                containerId!,
                _mockLogger.Object
            );
        }

        public async Task InitializeAsync()
        {
            // Ensure service is initialized
            await _profileService.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            // Clean up test data
            var profiles = await _profileService.GetAllEntitiesAsync(_testUserId);
            foreach (var profile in profiles)
            {
                await _profileService.DeleteEntityAsync(profile.Id.ToString());
            }

            _cosmosClient?.Dispose();
        }

        #region Create and Read Tests

        [Fact]
        public async Task CreateProfile_WithValidData_SavesAndRetrievesProfile()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId, "Integration Test PersonEntity");

            // Act - Create
            await _profileService.AddEntityAsync(profile);

            // Act - Retrieve
            var retrieved = await _profileService.GetEntityAsync(profile.Id.ToString());

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(profile.Id);
            retrieved.Name.Should().Be("Integration Test PersonEntity");
            retrieved.OwnedByUserId.Should().Be(_testUserId);
            retrieved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task CreateProfile_WithCompleteData_SavesAllPreferences()
        {
            // Arrange
            var profile = TestDataFactory.CreateCompleteProfile(_testUserId);

            // Act
            await _profileService.AddEntityAsync(profile);
            var retrieved = await _profileService.GetEntityAsync(profile.Id.ToString());

            // Assert
            retrieved.Should().NotBeNull();
            var personEntity = retrieved as PersonEntity;
            personEntity.Should().NotBeNull();
            personEntity!.EntertainmentPreferences.Should().NotBeNull();
            personEntity.EntertainmentPreferences.FavoriteMovieGenres.Should().HaveCount(3);
            personEntity.StylePreferences.FavoriteColors.Should().Contain("Blue");
            personEntity.NaturePreferences.PreferredSeasons.Should().Contain("Spring");
            personEntity.DietaryRestrictions.Restrictions.Should().Contain("Vegetarian");
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task UpdateProfile_ModifiesExistingProfile()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId, "Original Name");
            await _profileService.AddEntityAsync(profile);

            // Act
            profile.Name = "Updated Name";
            profile.Description = "Updated bio";
            await _profileService.UpdateEntityAsync(profile);

            // Retrieve
            var updated = await _profileService.GetEntityAsync(profile.Id.ToString());

            // Assert
            updated.Should().NotBeNull();
            updated!.Name.Should().Be("Updated Name");
            updated.Description.Should().Be("Updated bio");
            updated.LastModified.Should().BeAfter(updated.CreatedAt);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteProfile_RemovesProfileFromDatabase()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId);
            await _profileService.AddEntityAsync(profile);

            // Verify exists
            var exists = await _profileService.GetEntityAsync(profile.Id.ToString());
            exists.Should().NotBeNull();

            // Act
            await _profileService.DeleteEntityAsync(profile.Id.ToString());

            // Assert
            var deleted = await _profileService.GetEntityAsync(profile.Id.ToString());
            deleted.Should().BeNull();
        }

        #endregion

        #region Query Tests

        [Fact]
        public async Task GetAllEntitiesAsync_WithUserId_ReturnsOnlyUserProfiles()
        {
            // Arrange
            var profile1 = TestDataFactory.CreateMinimalProfile(_testUserId, "PersonEntity 1");
            var profile2 = TestDataFactory.CreateMinimalProfile(_testUserId, "PersonEntity 2");
            var otherUserProfile = TestDataFactory.CreateMinimalProfile("other-user", "Other PersonEntity");

            await _profileService.AddEntityAsync(profile1);
            await _profileService.AddEntityAsync(profile2);
            await _profileService.AddEntityAsync(otherUserProfile);

            // Act
            var userProfiles = await _profileService.GetAllEntitiesAsync(_testUserId);

            // Assert
            userProfiles.Should().HaveCount(2);
            userProfiles.Should().OnlyContain(p => p.OwnedByUserId == _testUserId);
            userProfiles.Select(p => p.Name).Should().Contain(new[] { "PersonEntity 1", "PersonEntity 2" });

            // Cleanup other user's profile
            await _profileService.DeleteEntityAsync(otherUserProfile.Id.ToString());
        }

        [Fact]
        public async Task SearchEntitiesAsync_FindsMatchingProfiles()
        {
            // Arrange
            var profile1 = TestDataFactory.CreateMinimalProfile(_testUserId, "John Doe");
            var profile2 = TestDataFactory.CreateMinimalProfile(_testUserId, "Jane Smith");
            var profile3 = TestDataFactory.CreateMinimalProfile(_testUserId, "Johnny Walker");

            await _profileService.AddEntityAsync(profile1);
            await _profileService.AddEntityAsync(profile2);
            await _profileService.AddEntityAsync(profile3);

            // Act
            var results = await _profileService.SearchEntitiesAsync("john");

            // Assert
            results.Should().HaveCountGreaterThanOrEqualTo(2);
            results.Select(p => p.Name).Should().Contain(new[] { "John Doe", "Johnny Walker" });
        }

        #endregion

        #region Ownership Tests

        [Fact]
        public async Task GetEntityAsync_WithWrongUserId_ReturnsNull()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId, "Owned PersonEntity");
            await _profileService.AddEntityAsync(profile);

            // Act
            var result = await _profileService.GetEntityAsync(profile.Id.ToString(), "wrong-user-id");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetEntityAsync_WithCorrectUserId_ReturnsProfile()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId, "Owned PersonEntity");
            await _profileService.AddEntityAsync(profile);

            // Act
            var result = await _profileService.GetEntityAsync(profile.Id.ToString(), _testUserId);

            // Assert
            result.Should().NotBeNull();
            result!.OwnedByUserId.Should().Be(_testUserId);
        }

        #endregion

        #region Complex PersonEntity Tests

        [Fact]
        public async Task CompleteProfileLifecycle_CreateUpdateSearchDelete_WorksEndToEnd()
        {
            // Create
            var profile = TestDataFactory.CreateCompleteProfile(_testUserId);
            await _profileService.AddEntityAsync(profile);

            // Read
            var retrieved = await _profileService.GetEntityAsync(profile.Id.ToString());
            retrieved.Should().NotBeNull();

            // Update
            retrieved!.Name = "Updated Name";
            var personEntity = retrieved as PersonEntity;
            personEntity.Should().NotBeNull();
            personEntity!.StylePreferences.FavoriteColors = new List<string> { "Red", "Yellow" };
            await _profileService.UpdateEntityAsync(retrieved);

            // Search
            var searchResults = await _profileService.SearchEntitiesAsync("updated");
            searchResults.Should().Contain(p => p.Id == profile.Id);

            // Delete
            await _profileService.DeleteEntityAsync(profile.Id.ToString());
            var deleted = await _profileService.GetEntityAsync(profile.Id.ToString());
            deleted.Should().BeNull();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task CreateProfile_WithLongText_SavesCorrectly()
        {
            // Arrange
            var longBio = new string('A', 5000); // 5000 character bio
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId);
            profile.Description = longBio;

            // Act
            await _profileService.AddEntityAsync(profile);
            var retrieved = await _profileService.GetEntityAsync(profile.Id.ToString());

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Description.Should().HaveLength(5000);
        }

        [Fact]
        public async Task CreateProfile_WithSpecialCharacters_SavesCorrectly()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId);
            profile.Name = "Test 测试 テスト 🎉 <>&\"'";
            profile.Description = "Special chars: émojis 😀, quotes \"test\", tags <html>";

            // Act
            await _profileService.AddEntityAsync(profile);
            var retrieved = await _profileService.GetEntityAsync(profile.Id.ToString());

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Test 测试 テスト 🎉 <>&\"'");
            retrieved.Description.Should().Contain("😀");
        }

        #endregion

        #region Additional Test Cases (converted from unit tests)

        [Fact]
        public async Task GetEntityAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = await _profileService.GetEntityAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task AddEntityAsync_SetsTimestampsCorrectly()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId, "Timestamp Test PersonEntity");
            var beforeAdd = DateTime.UtcNow;

            // Act
            await _profileService.AddEntityAsync(profile);

            // Retrieve to verify it was saved
            var retrieved = await _profileService.GetEntityAsync(profile.Id.ToString());

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.CreatedAt.Should().BeOnOrAfter(beforeAdd);
            retrieved.LastModified.Should().BeOnOrAfter(beforeAdd);
            retrieved.CreatedAt.Should().BeCloseTo(retrieved.LastModified, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UpdateEntityAsync_UpdatesLastModifiedTimestamp()
        {
            // Arrange
            var profile = TestDataFactory.CreateMinimalProfile(_testUserId, "Update Timestamp Test");
            await _profileService.AddEntityAsync(profile);

            // Wait a moment to ensure timestamp difference
            await Task.Delay(100);

            var originalLastModified = profile.LastModified;
            var beforeUpdate = DateTime.UtcNow;

            // Act
            profile.Name = "Updated Name";
            await _profileService.UpdateEntityAsync(profile);

            // Retrieve to verify
            var retrieved = await _profileService.GetEntityAsync(profile.Id.ToString());

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Updated Name");
            retrieved.LastModified.Should().BeOnOrAfter(beforeUpdate);
            retrieved.LastModified.Should().BeAfter(originalLastModified);
        }

        #endregion
    }
}
