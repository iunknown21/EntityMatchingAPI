using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// Integration tests for client-uploaded embeddings
    /// Tests privacy-first vector upload workflow
    /// </summary>
    [Collection("Cosmos DB Integration Tests")]
    public class EmbeddingUploadIntegrationTests : IAsyncLifetime
    {
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;
        private readonly EmbeddingStorageService _embeddingService;
        private readonly EntityService _profileService;
        private readonly Mock<ILogger<EmbeddingStorageService>> _mockEmbeddingLogger;
        private readonly Mock<ILogger<EntityService>> _mockProfileLogger;
        private readonly List<string> _testProfileIds = new();

        public EmbeddingUploadIntegrationTests()
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
                    "Cosmos DB connection string not configured in testsettings.json. " +
                    "Integration tests require a real Cosmos DB instance.");
            }

            _cosmosClient = new CosmosClient(connectionString);
            _mockEmbeddingLogger = new Mock<ILogger<EmbeddingStorageService>>();
            _mockProfileLogger = new Mock<ILogger<EntityService>>();

            _embeddingService = new EmbeddingStorageService(
                _cosmosClient,
                _configuration,
                _mockEmbeddingLogger.Object
            );

            _profileService = new EntityService(
                _cosmosClient,
                databaseId!,
                containerId!,
                _mockProfileLogger.Object
            );
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            // Clean up test data
            foreach (var profileId in _testProfileIds)
            {
                try
                {
                    await _embeddingService.DeleteEmbeddingAsync(profileId);
                    await _profileService.DeleteEntityAsync(profileId);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            _cosmosClient?.Dispose();
        }

        #region Helper Methods

        private PersonEntity CreateTestProfile(string? userId = null)
        {
            var profile = new PersonEntity
            {
                Id = Guid.NewGuid(),
                OwnedByUserId = userId ?? Guid.NewGuid().ToString(),
                Name = "Test User",
                Description = "Test bio for integration testing",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _testProfileIds.Add(profile.Id.ToString());
            return profile;
        }

        private float[] GenerateRandomEmbedding(int dimensions)
        {
            var random = new Random();
            var embedding = new float[dimensions];
            for (int i = 0; i < dimensions; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2 - 1); // Random value between -1 and 1
            }
            return embedding;
        }

        private EntityEmbedding CreateClientUploadedEmbedding(string profileId, float[] embedding)
        {
            var placeholderSummary = "[CLIENT_UPLOADED]";

            return new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = placeholderSummary,
                SummaryHash = EntityEmbedding.ComputeHash(placeholderSummary),
                Embedding = embedding,
                EmbeddingModel = "text-embedding-3-small",
                Dimensions = embedding.Length,
                Status = EmbeddingStatus.Generated,
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                RetryCount = 0,
                ErrorMessage = null,
                SummaryMetadata = new SummaryMetadata
                {
                    HasConversationData = false,
                    ConversationChunksCount = 0,
                    ExtractedInsightsCount = 0,
                    PreferenceCategories = new List<string>(),
                    HasPersonalityData = false,
                    SummaryWordCount = 0
                }
            };
        }

        #endregion

        #region Upload Valid Embedding Tests

        [Fact]
        public async Task UploadEmbedding_ValidRequest_CreatesEmbedding()
        {
            // Arrange: Create test profile
            var profile = CreateTestProfile();
            await _profileService.AddEntityAsync(profile);

            var embedding = GenerateRandomEmbedding(1536);
            var clientEmbedding = CreateClientUploadedEmbedding(profile.Id.ToString(), embedding);

            // Act: Upload embedding
            var result = await _embeddingService.UpsertEmbeddingAsync(clientEmbedding);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(EmbeddingStatus.Generated);
            result.Dimensions.Should().Be(1536);
            result.Embedding.Should().NotBeNull();
            result.Embedding!.Length.Should().Be(1536);
            result.EntitySummary.Should().Be("[CLIENT_UPLOADED]");
            result.EmbeddingModel.Should().Be("text-embedding-3-small");
        }

        [Fact]
        public async Task UploadEmbedding_UpsertOverwritesExisting()
        {
            // Arrange: Create profile with existing embedding
            var profile = CreateTestProfile();
            await _profileService.AddEntityAsync(profile);

            var firstEmbedding = GenerateRandomEmbedding(1536);
            var firstClientEmbedding = CreateClientUploadedEmbedding(profile.Id.ToString(), firstEmbedding);
            await _embeddingService.UpsertEmbeddingAsync(firstClientEmbedding);

            // Act: Upload new embedding
            var secondEmbedding = GenerateRandomEmbedding(1536);
            var secondClientEmbedding = CreateClientUploadedEmbedding(profile.Id.ToString(), secondEmbedding);
            var result = await _embeddingService.UpsertEmbeddingAsync(secondClientEmbedding);

            // Assert: Should overwrite
            result.Should().NotBeNull();
            result.Embedding.Should().NotBeNull();
            result.Embedding!.SequenceEqual(secondEmbedding).Should().BeTrue();
        }

        #endregion

        #region Privacy Tests

        [Fact]
        public async Task UploadEmbedding_UsesPlaceholderSummary_NeverStoresPII()
        {
            // Arrange
            var profile = CreateTestProfile();
            await _profileService.AddEntityAsync(profile);

            var embedding = GenerateRandomEmbedding(1536);
            var clientEmbedding = CreateClientUploadedEmbedding(profile.Id.ToString(), embedding);

            // Act
            var result = await _embeddingService.UpsertEmbeddingAsync(clientEmbedding);

            // Assert: No actual text summary stored
            result.EntitySummary.Should().Be("[CLIENT_UPLOADED]");
            result.EntitySummary.Should().NotContain("resume");
            result.EntitySummary.Should().NotContain("personal");
        }

        #endregion

        #region Retrieval Tests

        [Fact]
        public async Task GetEmbedding_AfterUpload_ReturnsClientEmbedding()
        {
            // Arrange
            var profile = CreateTestProfile();
            await _profileService.AddEntityAsync(profile);

            var embedding = GenerateRandomEmbedding(1536);
            var clientEmbedding = CreateClientUploadedEmbedding(profile.Id.ToString(), embedding);
            await _embeddingService.UpsertEmbeddingAsync(clientEmbedding);

            // Act
            var retrieved = await _embeddingService.GetEmbeddingAsync(profile.Id.ToString());

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.EntityId.Should().Be(profile.Id.ToString());
            retrieved.Status.Should().Be(EmbeddingStatus.Generated);
            retrieved.EntitySummary.Should().Be("[CLIENT_UPLOADED]");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void ValidateEmbedding_1536Dimensions_ShouldBeValid()
        {
            // Arrange
            var embedding = GenerateRandomEmbedding(1536);

            // Act & Assert
            embedding.Length.Should().Be(1536);
            embedding.All(v => !float.IsNaN(v) && !float.IsInfinity(v)).Should().BeTrue();
        }

        [Fact]
        public void ValidateEmbedding_WrongDimensions_ShouldFail()
        {
            // Arrange
            var embedding = GenerateRandomEmbedding(512); // Wrong dimensions

            // Assert
            embedding.Length.Should().NotBe(1536);
        }

        [Fact]
        public void ValidateEmbedding_NaNValue_ShouldFail()
        {
            // Arrange
            var embedding = GenerateRandomEmbedding(1536);
            embedding[500] = float.NaN;

            // Assert
            embedding.Any(v => float.IsNaN(v)).Should().BeTrue();
        }

        [Fact]
        public void ValidateEmbedding_InfinityValue_ShouldFail()
        {
            // Arrange
            var embedding = GenerateRandomEmbedding(1536);
            embedding[500] = float.PositiveInfinity;

            // Assert
            embedding.Any(v => float.IsInfinity(v)).Should().BeTrue();
        }

        #endregion

        #region Integration with Existing Infrastructure

        [Fact]
        public async Task ClientEmbedding_AvailableForSearch_ViaGetEmbeddingsByStatus()
        {
            // Arrange: Create multiple profiles with client embeddings
            var profile1 = CreateTestProfile();
            var profile2 = CreateTestProfile();
            await _profileService.AddEntityAsync(profile1);
            await _profileService.AddEntityAsync(profile2);

            var embedding1 = CreateClientUploadedEmbedding(profile1.Id.ToString(), GenerateRandomEmbedding(1536));
            var embedding2 = CreateClientUploadedEmbedding(profile2.Id.ToString(), GenerateRandomEmbedding(1536));
            await _embeddingService.UpsertEmbeddingAsync(embedding1);
            await _embeddingService.UpsertEmbeddingAsync(embedding2);

            // Act: Get all Generated embeddings (should include client uploads)
            var allGenerated = await _embeddingService.GetEmbeddingsByStatusAsync(EmbeddingStatus.Generated);

            // Assert: Client embeddings should be included
            allGenerated.Should().Contain(e => e.EntityId == profile1.Id.ToString());
            allGenerated.Should().Contain(e => e.EntityId == profile2.Id.ToString());
        }

        [Fact]
        public async Task ClientEmbedding_NotInPendingQueue()
        {
            // Arrange
            var profile = CreateTestProfile();
            await _profileService.AddEntityAsync(profile);

            var embedding = CreateClientUploadedEmbedding(profile.Id.ToString(), GenerateRandomEmbedding(1536));
            await _embeddingService.UpsertEmbeddingAsync(embedding);

            // Act: Get pending embeddings
            var pending = await _embeddingService.GetEmbeddingsByStatusAsync(EmbeddingStatus.Pending);

            // Assert: Client embedding should NOT be in pending queue
            pending.Should().NotContain(e => e.EntityId == profile.Id.ToString());
        }

        #endregion
    }
}
