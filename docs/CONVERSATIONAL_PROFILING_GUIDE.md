# Conversational Profiling Guide

This guide explains how the EntityMatching API supports conversation mode for building rich profiles through natural language dialog, and how it handles file uploads.

## Table of Contents

1. [Overview](#overview)
2. [How Conversation Mode Works](#how-conversation-mode-works)
3. [File Upload Support](#file-upload-support)
4. [API Endpoints](#api-endpoints)
5. [Conversation Data Model](#conversation-data-model)
6. [AI-Powered Insight Extraction](#ai-powered-insight-extraction)
7. [SDK Examples](#sdk-examples)
8. [Advanced Features](#advanced-features)
9. [Best Practices](#best-practices)

---

## Overview

### What is Conversational Profiling?

Instead of filling out lengthy forms, users can build rich profiles by having natural conversations about themselves or their interests. The system uses AI to:

1. **Engage in natural dialog** - Ask follow-up questions to learn more
2. **Extract insights automatically** - Identify hobbies, preferences, personality traits
3. **Build searchable profiles** - Convert conversations into structured, searchable data
4. **Maintain context** - Remember previous messages for coherent conversations

### Key Benefits

✅ **User-Friendly**: Natural conversation instead of form fatigue
✅ **Rich Data Collection**: Captures nuanced information through dialog
✅ **Automatic Structuring**: AI extracts structured insights from unstructured text
✅ **Privacy-First**: Stores only essential information
✅ **Scalable**: Handles unlimited conversation length through auto-sharding

---

## How Conversation Mode Works

### The Conversation Flow

```
User: "They love hiking on weekends and enjoy mountain trails"
  ↓
System stores message → Generates AI response → Extracts insights
  ↓
AI: "Interesting! Do they prefer day hikes or multi-day backpacking trips?"
  ↓
Insights Extracted:
  - Category: hobby
  - Insight: "enjoys hiking on weekends"
  - Confidence: 0.9
```

### Architecture

```
┌─────────────┐
│   Client    │ Sends message
└──────┬──────┘
       │
       ↓
┌─────────────────────────────────────┐
│  Conversation API Endpoint          │
│  POST /v1/profiles/{id}/conversation│
└──────┬──────────────────────────────┘
       │
       ↓
┌──────────────────────────────────────────────┐
│  ConversationService                         │
│  1. Fetch conversation history              │
│  2. Generate AI response (Groq AI)          │
│  3. Extract insights (AI-powered)           │
│  4. Store chunks + insights                 │
│  5. Update metadata                         │
└──────┬───────────────────────────────────────┘
       │
       ↓
┌─────────────────────────────────────┐
│  Cosmos DB (conversations container)│
│  - ConversationDocument (sharded)   │
│  - ConversationMetadata             │
└─────────────────────────────────────┘
```

### Technology Stack

- **AI Model**: Groq AI (llama-3.3-70b-versatile)
  - Fast response times
  - High-quality conversational AI
  - Cost-effective at scale

- **Storage**: Azure Cosmos DB
  - NoSQL document database
  - Auto-sharding for large conversations
  - Partition key: `profileId`

- **Auto-Sharding**: Conversations automatically split into multiple documents when:
  - Size exceeds 1.5 MB
  - Chunk count exceeds 500 messages

---

## File Upload Support

### Privacy-First Approach: Vector-Only Upload

**Traditional file upload**: Upload resume.pdf → Server stores file → Security risk

**EntityMatching approach**: Generate embedding locally → Upload vector only → Zero PII stored

### How It Works

```
┌──────────────────────────────────────────────────────┐
│  User's Device                                       │
│                                                      │
│  1. User pastes resume text                         │
│  2. Client calls OpenAI API locally                 │
│  3. OpenAI returns 1536-dimension vector            │
│     [0.123, -0.456, 0.789, ..., 1536 numbers]      │
└──────────────────────┬───────────────────────────────┘
                       │
                       │ Upload ONLY the vector
                       ↓
┌──────────────────────────────────────────────────────┐
│  EntityMatching Server                              │
│                                                      │
│  - Receives: Float array [1536 dimensions]          │
│  - Stores: Embedding vector in Cosmos DB            │
│  - NEVER sees: Resume text, name, email, phone      │
└──────────────────────────────────────────────────────┘
```

### What Gets Uploaded

```json
{
  "embedding": [0.123, -0.456, 0.789, /* ...1533 more numbers... */],
  "embeddingModel": "text-embedding-3-small"
}
```

### What NEVER Gets Uploaded

❌ Resume text or PDF files
❌ Name, email, phone number
❌ Address or contact information
❌ Social security numbers
❌ Any personally identifiable information (PII)

### Benefits of Vector-Only Upload

1. **Privacy**: Even if database is breached, attackers get meaningless numbers
2. **GDPR Compliant**: No PII stored = minimal compliance requirements
3. **Secure**: Impossible to reverse-engineer original text from embeddings
4. **Efficient**: Vectors are smaller than documents (6KB vs potentially MB)

### Supported Embedding Models

| Model | Dimensions | Use Case |
|-------|-----------|----------|
| text-embedding-3-small | 1536 | General purpose (recommended) |
| text-embedding-3-large | 3072 | Higher accuracy, larger cost |

---

## API Endpoints

### Send Message to Conversation

```http
POST /api/v1/entities/{profileId}/conversation
Content-Type: application/json
Ocp-Apim-Subscription-Key: YOUR_API_KEY

{
  "message": "They love hiking on weekends",
  "userId": "user-123"
}
```

**Response:**
```json
{
  "aiResponse": "That's wonderful! Do they prefer mountain trails or forest paths?",
  "newInsights": [
    {
      "category": "hobby",
      "insight": "enjoys hiking on weekends",
      "confidence": 0.9,
      "sourceChunk": "They love hiking on weekends",
      "extractedAt": "2026-01-22T10:30:00Z"
    }
  ],
  "conversationId": "conv-doc-abc123"
}
```

### Get Conversation History

```http
GET /api/v1/entities/{profileId}/conversation
Ocp-Apim-Subscription-Key: YOUR_API_KEY
```

**Response:**
```json
{
  "id": "meta-profile-123",
  "profileId": "profile-123",
  "userId": "user-123",
  "conversationChunks": [
    {
      "speaker": "user",
      "text": "They love hiking on weekends",
      "timestamp": "2026-01-22T10:30:00Z"
    },
    {
      "speaker": "ai",
      "text": "That's wonderful! Do they prefer mountain trails...",
      "timestamp": "2026-01-22T10:30:01Z"
    }
  ],
  "extractedInsights": [
    {
      "category": "hobby",
      "insight": "enjoys hiking on weekends",
      "confidence": 0.9
    }
  ],
  "createdAt": "2026-01-22T10:30:00Z",
  "lastUpdated": "2026-01-22T10:30:01Z"
}
```

### Clear Conversation History

```http
DELETE /api/v1/entities/{profileId}/conversation
Ocp-Apim-Subscription-Key: YOUR_API_KEY
```

**Response:** `204 No Content`

### Upload Embedding (Vector Only)

```http
POST /api/v1/entities/{profileId}/embeddings/upload
Content-Type: application/json
Ocp-Apim-Subscription-Key: YOUR_API_KEY

{
  "embedding": [0.123, -0.456, /* ...1534 more numbers... */],
  "embeddingModel": "text-embedding-3-small"
}
```

**Response:**
```json
{
  "success": true,
  "profileId": "profile-123",
  "embeddingDimensions": 1536,
  "uploadedAt": "2026-01-22T10:30:00Z"
}
```

---

## Conversation Data Model

### ConversationContext

The complete conversation history for a profile:

```csharp
{
  "id": "meta-profile-123",
  "profileId": "profile-123",
  "userId": "user-123",
  "conversationChunks": [
    {
      "speaker": "user|ai",
      "text": "message content",
      "timestamp": "2026-01-22T10:30:00Z",
      "context": "optional metadata"
    }
  ],
  "extractedInsights": [
    {
      "category": "hobby|preference|restriction|personality|interest|lifestyle|values",
      "insight": "specific insight text",
      "confidence": 0.0-1.0,
      "sourceChunk": "original message",
      "extractedAt": "timestamp"
    }
  ],
  "createdAt": "timestamp",
  "lastUpdated": "timestamp"
}
```

### ConversationChunk

Individual message in the conversation:

| Property | Type | Description |
|----------|------|-------------|
| `speaker` | string | "user" or "ai" |
| `text` | string | Message content |
| `timestamp` | DateTime | When message was sent |
| `context` | string | Optional metadata |

### ExtractedInsight

Structured knowledge extracted from conversation:

| Property | Type | Description |
|----------|------|-------------|
| `category` | string | hobby, preference, restriction, personality, interest, lifestyle, values |
| `insight` | string | The actual information (e.g., "enjoys hiking") |
| `confidence` | float | 0.0-1.0 confidence score |
| `sourceChunk` | string | Original message text |
| `extractedAt` | DateTime | When insight was extracted |

### Insight Categories

- **hobby**: Activities they enjoy (hiking, painting, gaming)
- **preference**: Likes/dislikes (prefers mountains over beaches)
- **restriction**: Limitations (vegetarian, no alcohol, allergies)
- **personality**: Traits (introverted, adventurous, organized)
- **interest**: Topics of interest (technology, history, cooking)
- **lifestyle**: Daily habits (early riser, night owl, active)
- **values**: Core beliefs (environmentalism, family-first, honesty)

---

## AI-Powered Insight Extraction

### How Insights Are Extracted

Every message exchange (user message + AI response) is analyzed by AI to extract structured insights.

**Extraction Process:**

1. **User sends message**: "They're vegetarian and love trying new plant-based recipes"
2. **AI generates response**: "That's great! Do they have favorite cuisines for vegetarian cooking?"
3. **Extraction AI analyzes both**: Identifies structured insights
4. **Returns JSON**:
   ```json
   [
     {
       "category": "restriction",
       "insight": "vegetarian diet",
       "confidence": 0.95
     },
     {
       "category": "interest",
       "insight": "enjoys plant-based cooking",
       "confidence": 0.9
     }
   ]
   ```

### Confidence Scoring

- **0.9-1.0**: Very confident (explicitly stated)
- **0.7-0.9**: Confident (clearly implied)
- **0.5-0.7**: Moderate (somewhat implied)
- **Below 0.5**: Low confidence (discarded by default)

### Using Insights in Searches

Insights are automatically incorporated into profile summaries and embedding generation, enabling semantic search:

```javascript
// Search for vegetarian hikers who enjoy cooking
const results = await client.search.search({
  query: "vegetarian outdoor enthusiast who loves cooking",
  minSimilarity: 0.7
});
```

The conversation insights make profiles more discoverable through natural language queries.

---

## SDK Examples

### JavaScript/TypeScript SDK

```javascript
const { EntityMatchingClient } = require('@EntityMatching/sdk');

const client = new EntityMatchingClient({
  apiKey: 'YOUR_API_KEY',
  baseUrl: 'https://EntityMatching-apim.azure-api.net/v1'
});

// Send message to conversation
const response = await client.conversations.sendMessage(
  profileId,
  'user-123',
  'They love hiking on weekends and enjoy mountain trails'
);

console.log('AI Response:', response.aiResponse);
console.log('New Insights:', response.newInsights);

// Get conversation history
const history = await client.conversations.getHistory(profileId);
console.log('Total messages:', history.conversationChunks.length);
console.log('Total insights:', history.extractedInsights.length);

// Clear conversation
await client.conversations.delete(profileId);

// Upload embedding (privacy-first)
const resumeText = "Experienced software engineer...";
const embedding = await client.generateEmbedding(resumeText);
await client.embeddings.upload(profileId, {
  embedding: embedding,
  embeddingModel: 'text-embedding-3-small'
});
```

### C# SDK

```csharp
using EntityMatching.SDK;

var client = new EntityMatchingClient(new EntityMatchingClientOptions
{
    ApiKey = "YOUR_API_KEY",
    BaseUrl = "https://EntityMatching-apim.azure-api.net/v1"
});

// Send message to conversation
var response = await client.Conversations.SendMessageAsync(
    profileId,
    "user-123",
    "They love hiking on weekends and enjoy mountain trails"
);

Console.WriteLine($"AI Response: {response.AiResponse}");
Console.WriteLine($"New Insights: {response.NewInsights.Count}");

// Get conversation history
var history = await client.Conversations.GetHistoryAsync(profileId);
Console.WriteLine($"Total messages: {history.ConversationChunks.Count}");
Console.WriteLine($"Total insights: {history.ExtractedInsights.Count}");

// Clear conversation
await client.Conversations.DeleteAsync(profileId);

// Upload embedding (privacy-first)
string resumeText = "Experienced software engineer...";
await client.UploadResumeAsync(profileId, resumeText);
```

### cURL

```bash
# Send message
curl -X POST https://EntityMatching-apim.azure-api.net/v1/profiles/{profileId}/conversation \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: YOUR_API_KEY" \
  -d '{
    "message": "They love hiking on weekends",
    "userId": "user-123"
  }'

# Get history
curl -X GET https://EntityMatching-apim.azure-api.net/v1/profiles/{profileId}/conversation \
  -H "Ocp-Apim-Subscription-Key: YOUR_API_KEY"

# Clear conversation
curl -X DELETE https://EntityMatching-apim.azure-api.net/v1/profiles/{profileId}/conversation \
  -H "Ocp-Apim-Subscription-Key: YOUR_API_KEY"

# Upload embedding
curl -X POST https://EntityMatching-apim.azure-api.net/v1/profiles/{profileId}/embeddings/upload \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: YOUR_API_KEY" \
  -d '{
    "embedding": [0.123, -0.456, ...],
    "embeddingModel": "text-embedding-3-small"
  }'
```

---

## Advanced Features

### Auto-Sharding for Large Conversations

Conversations automatically split into multiple documents when they grow large:

**Trigger Conditions:**
- Document size exceeds **1.5 MB**
- Chunk count exceeds **500 messages**

**How It Works:**
1. System detects document approaching limits
2. Creates new document with incremented sequence number
3. Updates metadata to point to new active document
4. Previous documents remain for history retrieval
5. `GetHistoryAsync()` aggregates all documents seamlessly

**Benefits:**
- Support unlimited conversation length
- No performance degradation
- Automatic and transparent to clients

### Conversation Context Window

AI responses use the **last 10 messages** as context:

```csharp
public string GetConversationSummary()
{
    // Returns last 10 messages formatted for AI
    var recentChunks = ConversationChunks
        .OrderByDescending(c => c.Timestamp)
        .Take(10)
        .OrderBy(c => c.Timestamp);

    return string.Join("\n", recentChunks.Select(c =>
        $"{c.Speaker}: {c.Text}"));
}
```

This ensures:
- Coherent, contextually-aware responses
- Efficient token usage (not sending entire history)
- Consistent AI performance

### High-Confidence Insights Summary

Retrieve only the most reliable insights:

```csharp
public Dictionary<string, List<string>> GetInsightsSummary(float minConfidence = 0.6f)
{
    return ExtractedInsights
        .Where(i => i.Confidence >= minConfidence)
        .GroupBy(i => i.Category)
        .ToDictionary(
            g => g.Key,
            g => g.Select(i => i.Insight).Distinct().ToList()
        );
}
```

**Example Output:**
```json
{
  "hobby": ["enjoys hiking", "plays guitar"],
  "preference": ["prefers mountains over beaches"],
  "restriction": ["vegetarian diet"],
  "personality": ["introverted", "thoughtful"]
}
```

### Integration with Profile Summaries

Conversation insights are automatically included in profile summaries for embedding generation:

```
Profile Summary:
Name: John Doe
Bio: Software engineer...

Conversation Insights:
- Hobbies: hiking, guitar
- Preferences: mountains over beaches
- Diet: vegetarian
- Personality: introverted, thoughtful
```

This enriched summary generates better embeddings for more accurate semantic search.

---

## Best Practices

### For Developers

1. **Start Simple**: Begin with basic profile info, then use conversation for depth
   ```javascript
   // Create minimal profile
   const profile = await client.profiles.create({
     ownedByUserId: 'user-123',
     name: 'John Doe',
     isSearchable: true
   });

   // Build rich profile through conversation
   await client.conversations.sendMessage(
     profile.id,
     'user-123',
     'Tell me about your interests...'
   );
   ```

2. **Handle AI Responses Gracefully**: AI may occasionally return unexpected formats
   ```javascript
   try {
     const response = await client.conversations.sendMessage(...);
     console.log(response.aiResponse);
   } catch (error) {
     console.error('AI service unavailable:', error);
     // Fallback: Store message without AI response
   }
   ```

3. **Use Confidence Thresholds**: Filter insights by confidence for quality
   ```javascript
   const history = await client.conversations.getHistory(profileId);
   const highConfidenceInsights = history.extractedInsights
     .filter(i => i.confidence >= 0.7);
   ```

4. **Privacy-First Upload**: Always generate embeddings client-side
   ```javascript
   // ✅ Good: Embedding generated locally
   const embedding = await client.generateEmbedding(resumeText);
   await client.embeddings.upload(profileId, { embedding });

   // ❌ Bad: Sending raw text to server
   // await client.uploadRawResume(profileId, resumeText); // Doesn't exist
   ```

5. **Monitor Conversation Length**: Consider UX for very long conversations
   ```javascript
   const history = await client.conversations.getHistory(profileId);
   if (history.conversationChunks.length > 100) {
     // Suggest summarizing or starting fresh
     console.warn('Conversation is getting lengthy');
   }
   ```

### For End Users

1. **Be Conversational**: Speak naturally, as if talking to a friend
   - ✅ "They love hiking on weekends and enjoy mountain trails"
   - ❌ "Hobby: hiking. Frequency: weekends. Terrain: mountains."

2. **Provide Context**: Share stories and details, not just facts
   - ✅ "Last summer they hiked the Pacific Crest Trail and loved camping under the stars"
   - ❌ "Likes hiking"

3. **Follow AI Prompts**: The AI asks targeted questions to extract insights
   ```
   AI: "What kind of music do they enjoy?"
   User: "Mostly indie rock and folk music at coffee shops"
   → Extracts: "interest: indie rock and folk music"
   ```

4. **Iterate**: You can always add more details later
   ```
   Visit 1: Basic interests
   Visit 2: More hobbies discovered
   Visit 3: Lifestyle preferences
   ```

---

## Technical Specifications

### Cosmos DB Schema

**Container**: `conversations`
**Partition Key**: `/profileId`

**Document Types:**

1. **ConversationDocument** (id: `conv-{profileId}-{sequenceNumber}`)
   ```json
   {
     "id": "conv-profile-123-0",
     "profileId": "profile-123",
     "userId": "user-123",
     "sequenceNumber": 0,
     "isActive": true,
     "conversationChunks": [...],
     "extractedInsights": [...],
     "createdAt": "timestamp",
     "lastUpdated": "timestamp",
     "estimatedSizeBytes": 524288,
     "chunkCount": 150,
     "insightCount": 45
   }
   ```

2. **ConversationMetadata** (id: `meta-{profileId}`)
   ```json
   {
     "id": "meta-profile-123",
     "profileId": "profile-123",
     "userId": "user-123",
     "activeDocumentId": "conv-profile-123-0",
     "activeSequenceNumber": 0,
     "totalDocuments": 1,
     "totalChunks": 150,
     "totalInsights": 45,
     "createdAt": "timestamp",
     "lastUpdated": "timestamp"
   }
   ```

### AI Configuration

**Groq API Settings:**
- **Endpoint**: https://api.groq.com/openai/v1/chat/completions
- **Model**: llama-3.3-70b-versatile
- **Temperature**: 0.7 (conversational), 0.3 (extraction)
- **Max Tokens**: 500
- **System Prompt**: Focused on learning about romantic partners/dating interests

### Performance Characteristics

| Operation | Typical Latency | Notes |
|-----------|----------------|-------|
| Send Message | 1-3 seconds | Includes AI response generation |
| Get History | 100-300ms | Single partition query |
| Clear Conversation | 200-500ms | Deletes all documents + metadata |
| Upload Embedding | 50-150ms | Simple vector storage |
| Insight Extraction | 500-1500ms | AI-powered analysis |

### Rate Limits

Follow the general API rate limits for your subscription tier:

| Tier | Requests/Month | Requests/Minute |
|------|---------------|-----------------|
| Free | 5,000 | 100 |
| Premium | 100,000 | 1,000 |
| Enterprise | Unlimited | 10,000 |

---

## Troubleshooting

### Common Issues

**1. AI Response is Generic or Off-Topic**
- **Cause**: Insufficient conversation history or context
- **Solution**: Ensure you're storing all messages, check last 10 messages are relevant

**2. Insights Not Being Extracted**
- **Cause**: AI extraction failed or returned invalid JSON
- **Solution**: Check logs, verify Groq API key is valid, ensure confidence threshold isn't too high

**3. Conversation History Missing Messages**
- **Cause**: Document sharding occurred, not aggregating all documents
- **Solution**: Use `GetConversationHistoryAsync()` which auto-aggregates all documents

**4. Embedding Upload Fails with "Invalid Dimensions"**
- **Cause**: Wrong embedding model or corrupted vector
- **Solution**: Verify using text-embedding-3-small (1536) or text-embedding-3-large (3072)

**5. CORS Errors in Browser**
- **Cause**: OPTIONS preflight not handled
- **Solution**: Conversation endpoints include OPTIONS handlers, check API Gateway CORS config

---

## Related Documentation

- [Getting Started Guide](./GETTING_STARTED.md) - Quick start with EntityMatching API
- [Core Platform API](./CORE_PLATFORM_API.md) - Complete API reference
- [Embedding Architecture](./EMBEDDING_ARCHITECTURE.md) - How embeddings work
- [JavaScript SDK Guide](./SDK_JAVASCRIPT.md) - Client SDK for web/Node.js
- [C# SDK Guide](./SDK_CSHARP.md) - Client SDK for .NET

---

## Support

For questions or issues with conversational profiling:

- **Documentation**: https://github.com/iunknown21/EntityMatchingAPI/tree/master/docs
- **API Reference**: [CORE_PLATFORM_API.md](./CORE_PLATFORM_API.md)
- **Issues**: https://github.com/iunknown21/EntityMatchingAPI/issues
- **Email**: support@bystorm.com

---

Last Updated: 2026-01-22
