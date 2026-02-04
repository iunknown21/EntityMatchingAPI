using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Utilities;
using EntityMatching.Shared.Models;
using EntityMatching.Infrastructure.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for managing conversational context using Groq AI
    /// </summary>
    public class ConversationService : IConversationService
    {
        private readonly Container _conversationContainer;
        private readonly Container _entitiesContainer;
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseId;
        private readonly HttpClient _httpClient;
        private readonly string _groqApiKey;
        private readonly ILogger<ConversationService> _logger;
        private const string GroqChatEndpoint = "https://api.groq.com/openai/v1/chat/completions";
        private const string ConversationModel = "llama-3.3-70b-versatile"; // Fast, efficient model
        private const string ContainerName = "conversations";
        private const long MAX_DOCUMENT_SIZE = 1_572_864; // 1.5MB (75% of 2MB limit)
        private const int MAX_CHUNKS_PER_DOCUMENT = 500;

        public ConversationService(
            CosmosClient cosmosClient,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ConversationService> logger)
        {
            _cosmosClient = cosmosClient;
            _databaseId = configuration["CosmosDb:DatabaseId"];
            _conversationContainer = cosmosClient.GetDatabase(_databaseId).GetContainer(ContainerName);
            _entitiesContainer = cosmosClient.GetDatabase(_databaseId).GetContainer("entities");
            _httpClient = httpClient;
            _groqApiKey = configuration["ApiKeys:Groq"] ?? configuration["ApiKeys__Groq"];
            _logger = logger;

            // Initialize container on startup
            InitializeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initialize Cosmos DB container - creates if doesn't exist
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Cosmos DB conversations container");

                var database = _cosmosClient.GetDatabase(_databaseId);

                // Create container if it doesn't exist
                // Partition by entityId for efficient querying
                ContainerProperties containerProperties = new ContainerProperties
                {
                    Id = ContainerName,
                    PartitionKeyPath = "/entityId"
                };

                // Don't specify throughput - serverless mode compatible
                await database.CreateContainerIfNotExistsAsync(containerProperties);
                _logger.LogInformation($"Conversations container initialized: {ContainerName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing conversations container");
                throw;
            }
        }

        /// <summary>
        /// Process user message and generate AI response with insight extraction
        /// </summary>
        public async Task<ConversationResponse> ProcessUserMessageAsync(string entityId, string userId, string message, string? systemPrompt = null)
        {
            try
            {
                _logger.LogInformation("Processing conversation message for entity {EntityId}", entityId);

                // Get entity to verify it exists
                var entity = await GetEntityAsync(entityId);
                if (entity == null)
                {
                    throw new InvalidOperationException($"Entity {entityId} not found");
                }

                // Get or create metadata
                var metadata = await GetMetadataAsync(entityId);
                if (metadata == null)
                {
                    // New conversation - systemPrompt is required
                    if (string.IsNullOrEmpty(systemPrompt))
                    {
                        throw new InvalidOperationException("systemPrompt is required for new conversations");
                    }
                    metadata = await CreateMetadataAsync(entityId, userId, systemPrompt);
                }

                // Get active document
                var activeDoc = await GetActiveDocumentAsync(metadata);

                // Use stored system prompt from metadata (set during conversation creation)
                var effectivePrompt = metadata.SystemPrompt
                    ?? throw new InvalidOperationException("No system prompt found in conversation metadata");

                // Generate AI response using current context and stored prompt
                var aiResponse = await GenerateAIResponseAsync(activeDoc, message, effectivePrompt);

                // Extract insights
                var newInsights = await ExtractInsightsAsync(message, aiResponse, activeDoc);

                // Create chunks
                var userChunk = new ConversationChunk
                {
                    Text = message,
                    Timestamp = DateTime.UtcNow,
                    Speaker = "user"
                };

                var aiChunk = new ConversationChunk
                {
                    Text = aiResponse,
                    Timestamp = DateTime.UtcNow,
                    Speaker = "ai"
                };

                // Check if need to create new document BEFORE adding
                if (ShouldCreateNewDocument(activeDoc))
                {
                    // Mark current as inactive
                    activeDoc.IsActive = false;
                    await _conversationContainer.UpsertItemAsync(activeDoc, new PartitionKey(entityId));

                    _logger.LogInformation("Creating new conversation document for entity {EntityId}, sequence {Seq}",
                        entityId, metadata.ActiveSequenceNumber + 1);

                    // Create new active document
                    activeDoc = await CreateNewDocumentAsync(metadata, userId);
                }

                // Add chunks and insights to active document
                activeDoc.ConversationChunks.Add(userChunk);
                activeDoc.ConversationChunks.Add(aiChunk);
                activeDoc.ExtractedInsights.AddRange(newInsights);
                activeDoc.ChunkCount = activeDoc.ConversationChunks.Count;
                activeDoc.InsightCount = activeDoc.ExtractedInsights.Count;
                activeDoc.EstimatedSizeBytes = DocumentSizeEstimator.EstimateSize(activeDoc);
                activeDoc.LastUpdated = DateTime.UtcNow;

                // Save active document
                await _conversationContainer.UpsertItemAsync(activeDoc, new PartitionKey(entityId));

                // Update metadata
                metadata.TotalChunks += 2;
                metadata.TotalInsights += newInsights.Count;
                metadata.LastUpdated = DateTime.UtcNow;
                await _conversationContainer.UpsertItemAsync(metadata, new PartitionKey(entityId));

                _logger.LogInformation("Conversation processed. Doc size: {Size}KB, Total docs: {Docs}",
                    activeDoc.EstimatedSizeBytes / 1024, metadata.TotalDocuments);

                return new ConversationResponse
                {
                    AiResponse = aiResponse,
                    NewInsights = newInsights,
                    ConversationId = activeDoc.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing conversation message for entity {EntityId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// Get conversation history for an entity (aggregates all documents)
        /// </summary>
        public async Task<ConversationContext?> GetConversationHistoryAsync(string entityId)
        {
            try
            {
                var documents = await GetAllDocumentsAsync(entityId);
                if (!documents.Any())
                    return null;

                return ConversationContext.Aggregate(documents);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Clear conversation history for an entity (deletes all documents and metadata)
        /// </summary>
        public async Task ClearConversationHistoryAsync(string entityId)
        {
            // Delete all conversation documents
            var allDocs = await GetAllDocumentsAsync(entityId);
            foreach (var doc in allDocs)
            {
                await _conversationContainer.DeleteItemAsync<ConversationDocument>(
                    doc.Id,
                    new PartitionKey(entityId)
                );
            }

            // Delete metadata
            var metadataId = ConversationMetadata.GenerateId(entityId);
            try
            {
                await _conversationContainer.DeleteItemAsync<ConversationMetadata>(
                    metadataId,
                    new PartitionKey(entityId)
                );
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Metadata doesn't exist, that's fine
            }

            _logger.LogInformation("Cleared conversation history for entity {EntityId}", entityId);
        }

        /// <summary>
        /// Get formatted insights summary for use in AI prompts
        /// </summary>
        public async Task<string> GetInsightsSummaryAsync(string entityId)
        {
            var context = await GetConversationHistoryAsync(entityId);
            return context?.GetInsightsSummary() ?? "";
        }

        /// <summary>
        /// Get conversation documents for an entity with optional pagination
        /// </summary>
        public async Task<List<ConversationDocument>> GetConversationDocumentsAsync(
            string entityId,
            int? startSequence = null,
            int? limit = null)
        {
            var query = "SELECT * FROM c WHERE c.entityId = @entityId AND c.id != @metadataId";

            if (startSequence.HasValue)
                query += " AND c.sequenceNumber >= @startSequence";

            query += " ORDER BY c.sequenceNumber ASC";

            var queryDef = new QueryDefinition(query)
                .WithParameter("@entityId", entityId)
                .WithParameter("@metadataId", ConversationMetadata.GenerateId(entityId));

            if (startSequence.HasValue)
                queryDef = queryDef.WithParameter("@startSequence", startSequence.Value);

            var queryRequestOptions = limit.HasValue
                ? new QueryRequestOptions { MaxItemCount = limit.Value }
                : null;

            var iterator = _conversationContainer.GetItemQueryIterator<ConversationDocument>(
                queryDef,
                requestOptions: queryRequestOptions
            );

            var results = new List<ConversationDocument>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);

                if (limit.HasValue && results.Count >= limit.Value)
                    break;
            }

            return results;
        }

        /// <summary>
        /// Get conversation metadata for an entity
        /// </summary>
        public Task<ConversationMetadata?> GetConversationMetadataAsync(string entityId)
        {
            return GetMetadataAsync(entityId);
        }

        // ============= PRIVATE HELPER METHODS =============

        /// <summary>
        /// Get conversation metadata for an entity
        /// </summary>
        private async Task<ConversationMetadata?> GetMetadataAsync(string entityId)
        {
            try
            {
                var id = ConversationMetadata.GenerateId(entityId);
                var response = await _conversationContainer.ReadItemAsync<ConversationMetadata>(
                    id,
                    new PartitionKey(entityId)
                );
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Create new conversation metadata and first document
        /// </summary>
        private async Task<ConversationMetadata> CreateMetadataAsync(string entityId, string userId, string systemPrompt)
        {
            var firstDoc = new ConversationDocument
            {
                EntityId = entityId,
                UserId = userId,
                SequenceNumber = 0,
                IsActive = true
            };

            await _conversationContainer.UpsertItemAsync(firstDoc, new PartitionKey(entityId));

            var metadata = new ConversationMetadata
            {
                Id = ConversationMetadata.GenerateId(entityId),
                EntityId = entityId,
                UserId = userId,
                ActiveDocumentId = firstDoc.Id,
                ActiveSequenceNumber = 0,
                TotalDocuments = 1,
                SystemPrompt = systemPrompt
            };

            await _conversationContainer.UpsertItemAsync(metadata, new PartitionKey(entityId));
            _logger.LogInformation("Created conversation metadata for entity {EntityId}", entityId);
            return metadata;
        }

        /// <summary>
        /// Get the active (writable) conversation document
        /// </summary>
        private async Task<ConversationDocument> GetActiveDocumentAsync(ConversationMetadata metadata)
        {
            var response = await _conversationContainer.ReadItemAsync<ConversationDocument>(
                metadata.ActiveDocumentId,
                new PartitionKey(metadata.EntityId)
            );
            return response.Resource;
        }

        /// <summary>
        /// Get all conversation documents for an entity, ordered by sequence number
        /// </summary>
        private async Task<List<ConversationDocument>> GetAllDocumentsAsync(string entityId)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.entityId = @entityId AND c.id != @metadataId ORDER BY c.sequenceNumber ASC"
            )
            .WithParameter("@entityId", entityId)
            .WithParameter("@metadataId", ConversationMetadata.GenerateId(entityId));

            var iterator = _conversationContainer.GetItemQueryIterator<ConversationDocument>(query);
            var results = new List<ConversationDocument>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        /// <summary>
        /// Check if a new document should be created based on size/chunk count
        /// </summary>
        private bool ShouldCreateNewDocument(ConversationDocument doc)
        {
            return doc.EstimatedSizeBytes >= MAX_DOCUMENT_SIZE ||
                   doc.ChunkCount >= MAX_CHUNKS_PER_DOCUMENT;
        }

        /// <summary>
        /// Create a new conversation document and update metadata
        /// </summary>
        private async Task<ConversationDocument> CreateNewDocumentAsync(
            ConversationMetadata metadata,
            string userId)
        {
            var newDoc = new ConversationDocument
            {
                EntityId = metadata.EntityId,
                UserId = userId,
                SequenceNumber = metadata.ActiveSequenceNumber + 1,
                IsActive = true
            };

            await _conversationContainer.UpsertItemAsync(newDoc, new PartitionKey(metadata.EntityId));

            // Update metadata
            metadata.ActiveDocumentId = newDoc.Id;
            metadata.ActiveSequenceNumber = newDoc.SequenceNumber;
            metadata.TotalDocuments++;

            _logger.LogInformation("Created new conversation document for entity {EntityId}, sequence {Seq}",
                metadata.EntityId, newDoc.SequenceNumber);

            return newDoc;
        }

        /// <summary>
        /// Get entity from database
        /// </summary>
        private async Task<Entity?> GetEntityAsync(string entityId)
        {
            try
            {
                var response = await _entitiesContainer.ReadItemAsync<Entity>(entityId, new PartitionKey(entityId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Generate AI response using Groq
        /// </summary>
        private async Task<string> GenerateAIResponseAsync(ConversationDocument doc, string userMessage, string systemPrompt)
        {
            try
            {
                // Get last 10 messages from current document
                var conversationHistory = string.Join("\n",
                    doc.ConversationChunks
                        .OrderByDescending(c => c.Timestamp)
                        .Take(10)
                        .Reverse()
                        .Select(c => $"{c.Speaker}: {c.Text}")
                );

                var requestBody = new
                {
                    model = ConversationModel,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = $"Previous conversation:\n{conversationHistory}\n\nUser: {userMessage}" }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

                var response = await _httpClient.PostAsync(GroqChatEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Groq API error: {Error}", responseContent);
                    return "I'm having trouble connecting right now. Could you try again?";
                }

                var apiResponse = JsonSerializer.Deserialize<GroqApiResponse>(responseContent);
                return apiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "I'm not sure how to respond to that.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response");
                return "I encountered an error. Please try again.";
            }
        }

        /// <summary>
        /// Extract insights from conversation using Groq AI
        /// TODO: This is a basic implementation. Consider adding:
        /// - More sophisticated NLP for better insight extraction
        /// - Confidence scoring based on conversation patterns
        /// - Deduplication of similar insights
        /// </summary>
        private async Task<List<ExtractedInsight>> ExtractInsightsAsync(string userMessage, string aiResponse, ConversationDocument doc)
        {
            try
            {
                var extractionPrompt = $@"Analyze this conversation exchange and extract specific insights about the person being discussed.
Focus on concrete facts, preferences, hobbies, restrictions, personality traits, or interests.

User: {userMessage}
AI: {aiResponse}

IMPORTANT: Return ONLY a valid JSON array, with no additional text or explanation.

Format:
[
  {{""category"": ""hobby"", ""insight"": ""enjoys hiking"", ""confidence"": 0.9}},
  {{""category"": ""preference"", ""insight"": ""prefers Italian food"", ""confidence"": 0.8}}
]

Valid categories: hobby, preference, restriction, personality, interest, lifestyle, values
Confidence should be 0.0-1.0 based on how explicit the information is.
Return empty array [] if no clear insights found.
Do not include any text before or after the JSON array.";

                var requestBody = new
                {
                    model = ConversationModel,
                    messages = new[]
                    {
                        new { role = "user", content = extractionPrompt }
                    },
                    temperature = 0.3, // Lower temp for more consistent extraction
                    max_tokens = 500
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

                var response = await _httpClient.PostAsync(GroqChatEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to extract insights: {Error}", responseContent);
                    return new List<ExtractedInsight>();
                }

                var apiResponse = JsonSerializer.Deserialize<GroqApiResponse>(responseContent);
                var insightsText = apiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "[]";

                // Extract JSON array from the response (handle cases where AI adds explanatory text)
                string insightsJson = insightsText.Trim();

                // Look for JSON array in the response
                int jsonStart = insightsJson.IndexOf('[');
                int jsonEnd = insightsJson.LastIndexOf(']');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    insightsJson = insightsJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
                }
                else
                {
                    // No JSON array found, return empty list
                    _logger.LogWarning("No JSON array found in insight extraction response: {Response}", insightsText);
                    return new List<ExtractedInsight>();
                }

                // Parse the JSON array of insights
                var insights = JsonSerializer.Deserialize<List<InsightDto>>(insightsJson) ?? new List<InsightDto>();

                return insights.Select(i => new ExtractedInsight
                {
                    Category = i.Category,
                    Insight = i.Insight,
                    Confidence = i.Confidence,
                    SourceChunk = userMessage,
                    ExtractedAt = DateTime.UtcNow
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting insights from conversation");
                return new List<ExtractedInsight>(); // Return empty list on error
            }
        }

        // DTO for parsing insights from Groq response
        private class InsightDto
        {
            [System.Text.Json.Serialization.JsonPropertyName("category")]
            public string Category { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("insight")]
            public string Insight { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("confidence")]
            public float Confidence { get; set; } = 0.5f;
        }
    }
}
