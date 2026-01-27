using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Shared.Models;
using EntityMatching.Infrastructure.Services;
using EntityMatching.Tests.Helpers;
using Xunit;

namespace EntityMatching.Tests.Integration
{
    /// <summary>
    /// Integration tests for ConversationService using real Cosmos DB
    /// These tests require a valid Cosmos DB connection string in configuration
    /// </summary>
    [Collection("Cosmos DB Integration Tests")]
    public class ConversationServiceIntegrationTests : IAsyncLifetime
    {
        private readonly IConfiguration _configuration;
        private readonly CosmosClient _cosmosClient;
        private readonly ConversationService _conversationService;
        private readonly Mock<ILogger<ConversationService>> _mockLogger;
        private readonly string _testProfileId = $"test-profile-{Guid.NewGuid():N}";
        private readonly string _testUserId = $"test-user-{Guid.NewGuid():N}";

        public ConversationServiceIntegrationTests()
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
            _mockLogger = new Mock<ILogger<ConversationService>>();

            // Note: ConversationService requires HttpClient for Groq API
            // For integration tests, we'll only test Cosmos DB operations, not AI features
            var mockHttpClient = new HttpClient();

            _conversationService = new ConversationService(
                _cosmosClient,
                _configuration,
                mockHttpClient,
                _mockLogger.Object
            );
        }

        public async Task InitializeAsync()
        {
            // Container is auto-created by ConversationService constructor
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            // Clean up test data
            try
            {
                await _conversationService.ClearConversationHistoryAsync(_testProfileId);
            }
            catch
            {
                // Ignore cleanup errors
            }

            _cosmosClient?.Dispose();
        }

        #region Get Conversation Tests

        [Fact]
        public async Task GetConversationHistoryAsync_WithNonExistentProfile_ReturnsNull()
        {
            // Arrange
            var nonExistentProfileId = Guid.NewGuid().ToString();

            // Act
            var result = await _conversationService.GetConversationHistoryAsync(nonExistentProfileId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAndRetrieveConversation_StoresAndRetrievesCorrectly()
        {
            // Note: This test uses ProcessUserMessageAsync but cannot verify AI response/insights
            // because we don't have a real Groq API key in tests.
            // We're just testing the storage and retrieval mechanisms.

            // Arrange - Create conversation documents and metadata using the service
            var databaseId = _configuration["CosmosDb:DatabaseId"];
            var database = _cosmosClient.GetDatabase(databaseId);
            var conversationsContainer = database.GetContainer("conversations");

            // Manually create metadata and document to avoid Groq API call
            var metadata = new ConversationMetadata
            {
                Id = ConversationMetadata.GenerateId(_testProfileId),
                ProfileId = _testProfileId,
                UserId = _testUserId,
                ActiveDocumentId = "",
                ActiveSequenceNumber = 0,
                TotalDocuments = 1,
                TotalChunks = 2,
                TotalInsights = 1
            };

            var conversationDoc = new ConversationDocument
            {
                ProfileId = _testProfileId,
                UserId = _testUserId,
                SequenceNumber = 0,
                IsActive = true,
                ConversationChunks = new List<ConversationChunk>
                {
                    new ConversationChunk
                    {
                        Text = "Tell me about hiking",
                        Speaker = "user",
                        Timestamp = DateTime.UtcNow
                    },
                    new ConversationChunk
                    {
                        Text = "I'd love to hear about your hiking experiences!",
                        Speaker = "ai",
                        Timestamp = DateTime.UtcNow
                    }
                },
                ExtractedInsights = new List<ExtractedInsight>
                {
                    new ExtractedInsight
                    {
                        Category = "hobby",
                        Insight = "enjoys hiking",
                        Confidence = 0.9f,
                        SourceChunk = "Tell me about hiking",
                        ExtractedAt = DateTime.UtcNow
                    }
                },
                ChunkCount = 2,
                InsightCount = 1
            };

            metadata.ActiveDocumentId = conversationDoc.Id;

            // Act - Insert using new format
            await conversationsContainer.UpsertItemAsync(conversationDoc, new PartitionKey(_testProfileId));
            await conversationsContainer.UpsertItemAsync(metadata, new PartitionKey(_testProfileId));

            // Retrieve via service
            var retrieved = await _conversationService.GetConversationHistoryAsync(_testProfileId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.ProfileId.Should().Be(_testProfileId);
            retrieved.UserId.Should().Be(_testUserId);
            retrieved.ConversationChunks.Should().HaveCount(2);
            retrieved.ExtractedInsights.Should().HaveCount(1);
            retrieved.ExtractedInsights.First().Insight.Should().Be("enjoys hiking");
        }

        #endregion

        #region Clear Conversation Tests

        [Fact]
        public async Task ClearConversationHistoryAsync_RemovesConversation()
        {
            // Arrange - Create a conversation using new format
            var databaseId = _configuration["CosmosDb:DatabaseId"];
            var database = _cosmosClient.GetDatabase(databaseId);
            var conversationsContainer = database.GetContainer("conversations");

            var metadata = new ConversationMetadata
            {
                Id = ConversationMetadata.GenerateId(_testProfileId),
                ProfileId = _testProfileId,
                UserId = _testUserId,
                ActiveSequenceNumber = 0,
                TotalDocuments = 1
            };

            var conversationDoc = new ConversationDocument
            {
                ProfileId = _testProfileId,
                UserId = _testUserId,
                SequenceNumber = 0,
                IsActive = true
            };

            metadata.ActiveDocumentId = conversationDoc.Id;

            await conversationsContainer.UpsertItemAsync(conversationDoc, new PartitionKey(_testProfileId));
            await conversationsContainer.UpsertItemAsync(metadata, new PartitionKey(_testProfileId));

            // Verify it exists
            var exists = await _conversationService.GetConversationHistoryAsync(_testProfileId);
            exists.Should().NotBeNull();

            // Act - Clear the conversation
            await _conversationService.ClearConversationHistoryAsync(_testProfileId);

            // Assert - Should be gone
            var cleared = await _conversationService.GetConversationHistoryAsync(_testProfileId);
            cleared.Should().BeNull();
        }

        [Fact]
        public async Task ClearConversationHistoryAsync_WithNonExistent_DoesNotThrow()
        {
            // Arrange
            var nonExistentProfileId = Guid.NewGuid().ToString();

            // Act & Assert - Should not throw
            await _conversationService.ClearConversationHistoryAsync(nonExistentProfileId);
        }

        #endregion

        #region Insights Summary Tests

        [Fact]
        public async Task GetInsightsSummaryAsync_WithNoConversation_ReturnsEmptyString()
        {
            // Arrange
            var nonExistentProfileId = Guid.NewGuid().ToString();

            // Act
            var summary = await _conversationService.GetInsightsSummaryAsync(nonExistentProfileId);

            // Assert
            summary.Should().BeEmpty();
        }

        [Fact]
        public async Task GetInsightsSummaryAsync_WithInsights_ReturnsFormattedSummary()
        {
            // Arrange - Create a conversation with insights using new format
            var databaseId = _configuration["CosmosDb:DatabaseId"];
            var database = _cosmosClient.GetDatabase(databaseId);
            var conversationsContainer = database.GetContainer("conversations");

            var metadata = new ConversationMetadata
            {
                Id = ConversationMetadata.GenerateId(_testProfileId),
                ProfileId = _testProfileId,
                UserId = _testUserId,
                ActiveSequenceNumber = 0,
                TotalDocuments = 1,
                TotalInsights = 2
            };

            var conversationDoc = new ConversationDocument
            {
                ProfileId = _testProfileId,
                UserId = _testUserId,
                SequenceNumber = 0,
                IsActive = true,
                ExtractedInsights = new List<ExtractedInsight>
                {
                    new ExtractedInsight
                    {
                        Category = "hobby",
                        Insight = "enjoys photography",
                        Confidence = 0.95f,
                        ExtractedAt = DateTime.UtcNow
                    },
                    new ExtractedInsight
                    {
                        Category = "preference",
                        Insight = "prefers outdoor activities",
                        Confidence = 0.85f,
                        ExtractedAt = DateTime.UtcNow
                    }
                },
                InsightCount = 2
            };

            metadata.ActiveDocumentId = conversationDoc.Id;

            await conversationsContainer.UpsertItemAsync(conversationDoc, new PartitionKey(_testProfileId));
            await conversationsContainer.UpsertItemAsync(metadata, new PartitionKey(_testProfileId));

            // Act
            var summary = await _conversationService.GetInsightsSummaryAsync(_testProfileId);

            // Assert
            summary.Should().NotBeEmpty();
            summary.Should().Contain("photography");
            summary.Should().Contain("outdoor activities");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ConversationWithMultipleChunks_StoresAndRetrievesCorrectly()
        {
            // Arrange - Create conversation with multiple chunks using new format
            var databaseId = _configuration["CosmosDb:DatabaseId"];
            var database = _cosmosClient.GetDatabase(databaseId);
            var conversationsContainer = database.GetContainer("conversations");

            var metadata = new ConversationMetadata
            {
                Id = ConversationMetadata.GenerateId(_testProfileId),
                ProfileId = _testProfileId,
                UserId = _testUserId,
                ActiveSequenceNumber = 0,
                TotalDocuments = 1,
                TotalChunks = 20
            };

            var conversationDoc = new ConversationDocument
            {
                ProfileId = _testProfileId,
                UserId = _testUserId,
                SequenceNumber = 0,
                IsActive = true,
                ConversationChunks = new List<ConversationChunk>()
            };

            // Add 20 conversation chunks
            for (int i = 0; i < 20; i++)
            {
                conversationDoc.ConversationChunks.Add(new ConversationChunk
                {
                    Text = $"Message {i}",
                    Speaker = i % 2 == 0 ? "user" : "ai",
                    Timestamp = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            conversationDoc.ChunkCount = 20;
            metadata.ActiveDocumentId = conversationDoc.Id;

            // Act
            await conversationsContainer.UpsertItemAsync(conversationDoc, new PartitionKey(_testProfileId));
            await conversationsContainer.UpsertItemAsync(metadata, new PartitionKey(_testProfileId));
            var retrieved = await _conversationService.GetConversationHistoryAsync(_testProfileId);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.ConversationChunks.Should().HaveCount(20);
        }

        #endregion
    }
}
