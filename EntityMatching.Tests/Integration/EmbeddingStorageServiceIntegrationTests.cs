using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using EntityMatching.Infrastructure.Services;
using Xunit;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// Integration tests for EmbeddingStorageService using real Cosmos DB
    /// These tests require a valid Cosmos DB connection string in configuration
    /// </summary>
    [Collection("Cosmos DB Integration Tests")]
    public class EmbeddingStorageServiceIntegrationTests : IAsyncLifetime
    {
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;
        private readonly EmbeddingStorageService _embeddingService;
        private readonly Mock<ILogger<EmbeddingStorageService>> _mockLogger;
        private readonly List<string> _testProfileIds = new();

        public EmbeddingStorageServiceIntegrationTests()
        {
            // Load configuration from testsettings.json
            var testSettingsPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(testSettingsPath, optional: false)
                .Build();

            var connectionString = _configuration["CosmosDb:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("YOUR_"))
            {
                throw new InvalidOperationException(
                    "Cosmos DB connection string not configured in testsettings.json. " +
                    "Integration tests require a real Cosmos DB instance.");
            }

            _cosmosClient = new CosmosClient(connectionString);
            _mockLogger = new Mock<ILogger<EmbeddingStorageService>>();

            _embeddingService = new EmbeddingStorageService(
                _cosmosClient,
                _configuration,
                _mockLogger.Object
            );
        }

        public async Task InitializeAsync()
        {
            // Container is auto-created by EmbeddingStorageService constructor
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
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            _cosmosClient?.Dispose();
        }

        #region Get Embedding Tests

        [Fact]
        public async Task GetEmbeddingAsync_WithNonExistentProfile_ReturnsNull()
        {
            // Arrange
            var nonExistentProfileId = Guid.NewGuid().ToString();

            // Act
            var result = await _embeddingService.GetEmbeddingAsync(nonExistentProfileId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Upsert Embedding Tests

        [Fact]
        public async Task UpsertEmbeddingAsync_CreatesNewEmbedding()
        {
            // Arrange
            var profileId = Guid.NewGuid().ToString();
            _testProfileIds.Add(profileId);

            var embedding = new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = "Test profile summary for integration testing",
                SummaryHash = "test-hash-12345",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Pending
            };

            // Act
            var result = await _embeddingService.UpsertEmbeddingAsync(embedding);

            // Retrieve to verify
            var retrieved = await _embeddingService.GetEmbeddingAsync(profileId);

            // Assert
            result.Should().NotBeNull();
            retrieved.Should().NotBeNull();
            retrieved!.EntityId.Should().Be(profileId);
            retrieved.EntitySummary.Should().Be("Test profile summary for integration testing");
            retrieved.SummaryHash.Should().Be("test-hash-12345");
            retrieved.Status.Should().Be(EmbeddingStatus.Pending);
        }

        [Fact]
        public async Task UpsertEmbeddingAsync_UpdatesExistingEmbedding()
        {
            // Arrange - Create initial embedding
            var profileId = Guid.NewGuid().ToString();
            _testProfileIds.Add(profileId);

            var embedding = new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = "Original summary",
                SummaryHash = "original-hash",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Pending
            };

            await _embeddingService.UpsertEmbeddingAsync(embedding);

            // Wait a moment to ensure timestamp difference
            await Task.Delay(100);

            // Act - Update with new data
            embedding.EntitySummary = "Updated summary";
            embedding.SummaryHash = "updated-hash";
            embedding.Status = EmbeddingStatus.Generated;
            embedding.GeneratedAt = DateTime.UtcNow;

            await _embeddingService.UpsertEmbeddingAsync(embedding);

            // Retrieve to verify
            var retrieved = await _embeddingService.GetEmbeddingAsync(profileId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.EntitySummary.Should().Be("Updated summary");
            retrieved.SummaryHash.Should().Be("updated-hash");
            retrieved.Status.Should().Be(EmbeddingStatus.Generated);
        }

        #endregion

        #region Delete Embedding Tests

        [Fact]
        public async Task DeleteEmbeddingAsync_RemovesEmbedding()
        {
            // Arrange - Create an embedding
            var profileId = Guid.NewGuid().ToString();
            _testProfileIds.Add(profileId);

            var embedding = new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = "Summary to be deleted",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Pending
            };

            await _embeddingService.UpsertEmbeddingAsync(embedding);

            // Verify it exists
            var exists = await _embeddingService.GetEmbeddingAsync(profileId);
            exists.Should().NotBeNull();

            // Act - Delete the embedding
            await _embeddingService.DeleteEmbeddingAsync(profileId);

            // Assert - Should be gone
            var deleted = await _embeddingService.GetEmbeddingAsync(profileId);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteEmbeddingAsync_WithNonExistent_DoesNotThrow()
        {
            // Arrange
            var nonExistentProfileId = Guid.NewGuid().ToString();

            // Act & Assert - Should not throw
            await _embeddingService.DeleteEmbeddingAsync(nonExistentProfileId);
        }

        #endregion

        #region Query By Status Tests

        [Fact]
        public async Task GetEmbeddingsByStatusAsync_ReturnsEmbeddingsWithMatchingStatus()
        {
            // Arrange - Create embeddings with different statuses
            var pendingId1 = Guid.NewGuid().ToString();
            var pendingId2 = Guid.NewGuid().ToString();
            var generatedId = Guid.NewGuid().ToString();

            _testProfileIds.AddRange(new[] { pendingId1, pendingId2, generatedId });

            await _embeddingService.UpsertEmbeddingAsync(new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(pendingId1),
                EntityId = pendingId1,
                EntitySummary = "Pending 1",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Pending
            });

            await _embeddingService.UpsertEmbeddingAsync(new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(pendingId2),
                EntityId = pendingId2,
                EntitySummary = "Pending 2",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Pending
            });

            await _embeddingService.UpsertEmbeddingAsync(new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(generatedId),
                EntityId = generatedId,
                EntitySummary = "Generated",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Generated
            });

            // Act
            var pendingEmbeddings = await _embeddingService.GetEmbeddingsByStatusAsync(EmbeddingStatus.Pending);
            var generatedEmbeddings = await _embeddingService.GetEmbeddingsByStatusAsync(EmbeddingStatus.Generated);

            // Assert
            pendingEmbeddings.Should().HaveCountGreaterThanOrEqualTo(2);
            pendingEmbeddings.Should().OnlyContain(e => e.Status == EmbeddingStatus.Pending);

            generatedEmbeddings.Should().HaveCountGreaterThanOrEqualTo(1);
            generatedEmbeddings.Should().OnlyContain(e => e.Status == EmbeddingStatus.Generated);
        }

        #endregion

        #region Count By Status Tests

        [Fact]
        public async Task GetEmbeddingCountsByStatusAsync_ReturnsAccurateCounts()
        {
            // Arrange - Create embeddings with known statuses
            var pendingId = Guid.NewGuid().ToString();
            var generatedId = Guid.NewGuid().ToString();
            var failedId = Guid.NewGuid().ToString();

            _testProfileIds.AddRange(new[] { pendingId, generatedId, failedId });

            await _embeddingService.UpsertEmbeddingAsync(new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(pendingId),
                EntityId = pendingId,
                EntitySummary = "Pending",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Pending
            });

            await _embeddingService.UpsertEmbeddingAsync(new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(generatedId),
                EntityId = generatedId,
                EntitySummary = "Generated",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Generated
            });

            await _embeddingService.UpsertEmbeddingAsync(new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(failedId),
                EntityId = failedId,
                EntitySummary = "Failed",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Failed,
                ErrorMessage = "Test error"
            });

            // Act
            var counts = await _embeddingService.GetEmbeddingCountsByStatusAsync();

            // Assert
            counts.Should().ContainKey(EmbeddingStatus.Pending);
            counts.Should().ContainKey(EmbeddingStatus.Generated);
            counts.Should().ContainKey(EmbeddingStatus.Failed);

            counts[EmbeddingStatus.Pending].Should().BeGreaterThanOrEqualTo(1);
            counts[EmbeddingStatus.Generated].Should().BeGreaterThanOrEqualTo(1);
            counts[EmbeddingStatus.Failed].Should().BeGreaterThanOrEqualTo(1);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task UpsertEmbeddingAsync_WithLongSummary_StoresCorrectly()
        {
            // Arrange
            var profileId = Guid.NewGuid().ToString();
            _testProfileIds.Add(profileId);

            var longSummary = new string('A', 10000); // 10,000 character summary

            var embedding = new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = longSummary,
                SummaryHash = "long-hash",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Pending
            };

            // Act
            await _embeddingService.UpsertEmbeddingAsync(embedding);
            var retrieved = await _embeddingService.GetEmbeddingAsync(profileId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.EntitySummary.Should().HaveLength(10000);
        }

        [Fact]
        public async Task UpsertEmbeddingAsync_WithMetadata_StoresCorrectly()
        {
            // Arrange
            var profileId = Guid.NewGuid().ToString();
            _testProfileIds.Add(profileId);

            var embedding = new EntityEmbedding
            {
                Id = EntityEmbedding.GenerateId(profileId),
                EntityId = profileId,
                EntitySummary = "Test summary with metadata",
                SummaryHash = "meta-hash",
                GeneratedAt = DateTime.UtcNow,
                EntityLastModified = DateTime.UtcNow,
                Status = EmbeddingStatus.Generated,
                SummaryMetadata = new SummaryMetadata
                {
                    SummaryWordCount = 100,
                    HasPersonalityData = true,
                    HasConversationData = true,
                    PreferenceCategories = new List<string> { "Entertainment", "Adventure" }
                }
            };

            // Act
            await _embeddingService.UpsertEmbeddingAsync(embedding);
            var retrieved = await _embeddingService.GetEmbeddingAsync(profileId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.SummaryMetadata.Should().NotBeNull();
            retrieved.SummaryMetadata!.SummaryWordCount.Should().Be(100);
            retrieved.SummaryMetadata.HasPersonalityData.Should().BeTrue();
            retrieved.SummaryMetadata.PreferenceCategories.Should().Contain("Entertainment");
        }

        #endregion
    }
}
