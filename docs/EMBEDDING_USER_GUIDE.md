# Profile Embedding System - User Guide

## Overview

The Profile Embedding System automatically converts partner profiles into AI-powered vector embeddings, enabling semantic search and intelligent matching. This guide covers the APIs and operations available for managing profile embeddings.

## Table of Contents

- [Automatic Processing](#automatic-processing)
- [Profile Management APIs](#profile-management-apis)
- [Conversational Profile Building APIs](#conversational-profile-building-apis)
- [Embedding Status Monitoring](#embedding-status-monitoring)
- [Manual Operations](#manual-operations)
- [Troubleshooting](#troubleshooting)

---

## Automatic Processing

The embedding system works **automatically** in the background. When you create or update a profile, the system handles embedding generation without any manual intervention.

### Automatic Workflow

1. **Create/Update Profile** → Profile is saved to Cosmos DB
2. **Nightly Summary Generation** (2 AM UTC) → Text summary created, Status=Pending
3. **Embedding Processing** (every 30 min) → Vector embedding generated, Status=Generated
4. **Ready for Matching** → Profile can now be used for semantic search

**Timeline**: From profile creation to ready-for-matching typically takes **less than 24 hours** (maximum) or **as little as 30 minutes** if created just before a processing run.

---

## Profile Management APIs

### 1. Create a Profile

**Endpoint**: `POST /api/profiles`

**Request Body**:
```json
{
  "name": "Sarah",
  "bio": "Outdoor enthusiast who loves hiking and photography",
  "ownedByUserId": "user-123",
  "entertainmentPreferences": {
    "favoriteMovieGenres": ["Adventure", "Documentary"],
    "favoriteMusic": ["Indie", "Folk"]
  },
  "adventurePreferences": {
    "activityLevel": 8,
    "riskTolerance": 7,
    "noveltySeeking": 9
  },
  // ... other preferences
}
```

**Response**:
```json
{
  "id": "profile-abc123",
  "name": "Sarah",
  "createdAt": "2025-01-15T10:30:00Z",
  "lastModified": "2025-01-15T10:30:00Z"
  // ... full profile
}
```

**What Happens Next**:
- Profile is saved to Cosmos DB
- Next nightly run (2 AM UTC) creates text summary and EntityEmbedding document with Status=Pending
- Next 30-minute timer processes pending embedding and generates vector

---

### 2. Update a Profile

**Endpoint**: `PUT /api/profiles/{profileId}`

**Request Body**: (Same as Create, but with updated fields)

```json
{
  "id": "profile-abc123",
  "name": "Sarah",
  "bio": "Updated bio with new interests",
  // ... updated preferences
}
```

**Response**: Updated profile

**What Happens Next**:
- Profile `lastModified` timestamp is updated
- Next nightly run detects the change (compares timestamps)
- Regenerates summary with Status=Pending
- New embedding vector is generated, replacing the old one

---

### 3. Get a Profile

**Endpoint**: `GET /api/profiles/{profileId}`

**Response**:
```json
{
  "id": "profile-abc123",
  "name": "Sarah",
  "bio": "Outdoor enthusiast...",
  "lastModified": "2025-01-15T10:30:00Z"
  // ... full profile
}
```

**Note**: This returns the profile data only, not the embedding. Embeddings are managed internally by the system.

---

### 4. Get All Profiles for User

**Endpoint**: `GET /api/profiles?userId={userId}`

**Response**:
```json
[
  {
    "id": "profile-abc123",
    "name": "Sarah",
    // ...
  },
  {
    "id": "profile-def456",
    "name": "Mike",
    // ...
  }
]
```

---

### 5. Delete a Profile

**Endpoint**: `DELETE /api/profiles/{profileId}`

**Response**: `204 No Content`

**What Happens Next**:
- Profile is deleted from Cosmos DB
- EntityEmbedding document is **NOT** automatically deleted (for audit trail)
- You may want to call the admin API to delete the embedding if desired

---

## Conversational Profile Building APIs

The Conversational Profile Building feature allows you to build rich partner profiles through natural language conversations with an AI assistant. As users chat about their partner, the AI extracts structured insights that enhance the profile's depth beyond what can be captured through forms alone.

### How It Works

1. **User sends conversational message** → AI assistant responds naturally
2. **AI extracts insights** → Categories like hobbies, preferences, personality traits
3. **Insights stored** → Added to conversation context with confidence scores
4. **Summary generation** → Insights included in nightly profile summary generation
5. **Embedding generation** → Conversational data improves matching accuracy

**Example Flow**:
```
User: "My partner loves trying new restaurants, especially ethnic food"
  ↓
AI Response: "That's great! Does your partner have any favorite cuisines?"
  ↓
Extracted Insights:
  - Category: "preference", Insight: "enjoys trying new restaurants", Confidence: 0.9
  - Category: "preference", Insight: "prefers ethnic food", Confidence: 0.85
```

---

### 1. Send Conversation Message

**Endpoint**: `POST /api/v1/entities/{profileId}/conversation`

**Purpose**: Send a user message about the profile and receive an AI response with extracted insights

**Request Body**:
```json
{
  "message": "My partner loves hiking and being outdoors",
  "userId": "user-123"  // Optional, for ownership verification
}
```

**Response**:
```json
{
  "aiResponse": "That sounds wonderful! Hiking is a great way to stay active. Does your partner prefer challenging mountain trails or more relaxed nature walks?",
  "newInsights": [
    {
      "category": "hobby",
      "insight": "enjoys hiking",
      "confidence": 0.95,
      "sourceChunk": "My partner loves hiking and being outdoors",
      "extractedAt": "2025-01-15T14:30:00Z"
    },
    {
      "category": "preference",
      "insight": "prefers outdoor activities",
      "confidence": 0.85,
      "sourceChunk": "My partner loves hiking and being outdoors",
      "extractedAt": "2025-01-15T14:30:00Z"
    }
  ],
  "conversationId": "conv-abc123"
}
```

**Insight Categories**:
- `hobby` - Activities, interests, pastimes
- `preference` - Likes, dislikes, tendencies
- `restriction` - Dietary, accessibility, allergies
- `personality` - Traits, behaviors, characteristics
- `value` - Beliefs, principles, priorities

**Confidence Scores**:
- `0.9-1.0` - Very confident (direct statement)
- `0.7-0.9` - Confident (clear implication)
- `0.5-0.7` - Moderate (possible inference)
- `<0.5` - Low confidence (filtered out in summaries)

**Example Conversation**:
```bash
# First message
POST /api/v1/entities/profile-123/conversation
{
  "message": "My partner Sarah loves outdoor activities",
  "userId": "user-123"
}

# Response
{
  "aiResponse": "That's great! What kind of outdoor activities does Sarah enjoy most?",
  "newInsights": [
    {
      "category": "preference",
      "insight": "enjoys outdoor activities",
      "confidence": 0.9
    }
  ]
}

# Follow-up message
POST /api/v1/entities/profile-123/conversation
{
  "message": "She loves hiking and photography, especially sunrise shots",
  "userId": "user-123"
}

# Response
{
  "aiResponse": "Sunrise photography during hikes sounds amazing! Does she prefer mountain trails or coastal paths?",
  "newInsights": [
    {
      "category": "hobby",
      "insight": "enjoys hiking",
      "confidence": 0.95
    },
    {
      "category": "hobby",
      "insight": "passionate about photography",
      "confidence": 0.9
    },
    {
      "category": "preference",
      "insight": "prefers sunrise for photography",
      "confidence": 0.85
    }
  ]
}
```

**Error Responses**:
- `400 Bad Request` - Message is empty or invalid JSON
- `404 Not Found` - Profile doesn't exist or access denied
- `500 Internal Server Error` - AI processing error

---

### 2. Get Conversation History

**Endpoint**: `GET /api/v1/entities/{profileId}/conversation?userId={userId}`

**Purpose**: Retrieve complete conversation history and all extracted insights for a profile

**Query Parameters**:
- `userId` (optional) - Verify ownership before returning conversation

**Response**:
```json
{
  "id": "conv-abc123",
  "profileId": "profile-123",
  "userId": "user-123",
  "createdAt": "2025-01-15T14:00:00Z",
  "lastUpdated": "2025-01-15T14:35:00Z",
  "conversationChunks": [
    {
      "text": "My partner loves hiking and being outdoors",
      "timestamp": "2025-01-15T14:30:00Z",
      "speaker": "user",
      "context": "discussing hobbies"
    },
    {
      "text": "That sounds wonderful! Does your partner prefer challenging mountain trails or relaxed nature walks?",
      "timestamp": "2025-01-15T14:30:05Z",
      "speaker": "ai",
      "context": ""
    },
    {
      "text": "She loves challenging mountain hikes, the harder the better",
      "timestamp": "2025-01-15T14:31:00Z",
      "speaker": "user",
      "context": "discussing activity preferences"
    }
  ],
  "extractedInsights": [
    {
      "category": "hobby",
      "insight": "enjoys hiking",
      "confidence": 0.95,
      "sourceChunk": "My partner loves hiking and being outdoors",
      "extractedAt": "2025-01-15T14:30:00Z"
    },
    {
      "category": "preference",
      "insight": "prefers outdoor activities",
      "confidence": 0.85,
      "sourceChunk": "My partner loves hiking and being outdoors",
      "extractedAt": "2025-01-15T14:30:00Z"
    },
    {
      "category": "preference",
      "insight": "prefers challenging physical activities",
      "confidence": 0.9,
      "sourceChunk": "She loves challenging mountain hikes, the harder the better",
      "extractedAt": "2025-01-15T14:31:00Z"
    }
  ]
}
```

**If No Conversation Exists**:
```json
{
  "id": "new-conv-id",
  "profileId": "profile-123",
  "conversationChunks": [],
  "extractedInsights": []
}
```

**Use Cases**:
- Display conversation history in UI
- Review extracted insights
- Export conversation data
- Audit what the AI has learned

---

### 3. Delete Conversation History

**Endpoint**: `DELETE /api/v1/entities/{profileId}/conversation?userId={userId}`

**Purpose**: Clear all conversation history and insights for a profile

**Query Parameters**:
- `userId` (optional) - Verify ownership before deleting

**Response**: `204 No Content`

**What Happens Next**:
- ConversationContext document is deleted from Cosmos DB
- All conversation chunks removed
- All extracted insights removed
- **Note**: Profile summary already generated from insights will remain until next regeneration

**Use Cases**:
- User wants to start fresh
- Incorrect insights were extracted
- Privacy/data deletion request

**Example**:
```bash
DELETE /api/v1/entities/profile-123/conversation?userId=user-123
# Response: 204 No Content
```

---

### Integration with Profile Summaries

Conversational insights are **automatically included** in nightly profile summary generation:

**Without Conversation**:
```
Sarah's profile (created via form):
- Adventure level: 8/10
- Risk tolerance: 7/10
- Favorite activities: Hiking, Photography
```

**With Conversation**:
```
Sarah's profile (form + conversation):
- Adventure level: 8/10
- Risk tolerance: 7/10
- Favorite activities: Hiking, Photography

INSIGHTS FROM CONVERSATIONS:
- Enjoys challenging mountain hikes (confidence: 0.9)
- Passionate about sunrise photography (confidence: 0.85)
- Prefers outdoor activities over indoor (confidence: 0.8)
- Early riser for photography sessions (confidence: 0.75)
```

**Impact on Embeddings**:
- Richer summaries → More nuanced vector embeddings
- Better semantic matching → Improved recommendations
- Captures personality → Beyond structured preferences

---

### Best Practices

#### 1. Guide the Conversation
Ask follow-up questions based on AI responses to build depth:
```
User: "My partner loves food"
AI: "What kind of cuisine does your partner enjoy?"
User: "She's really into Thai and Vietnamese food"
AI: "Does she prefer spicy dishes or milder flavors?"
User: "The spicier the better!"
```

#### 2. Be Specific
Specific details yield higher-confidence insights:
```
❌ "My partner likes movies"
   → Insight: "enjoys movies" (confidence: 0.6)

✅ "My partner loves sci-fi movies, especially ones with time travel"
   → Insight: "enjoys sci-fi movies" (confidence: 0.9)
   → Insight: "interested in time travel themes" (confidence: 0.85)
```

#### 3. Natural Language
Write conversationally, not in bullet points:
```
❌ "Hobbies: hiking, photography, cooking"
   → AI struggles to extract context

✅ "My partner Sarah loves spending weekends hiking in the mountains
    and taking landscape photos. She's also passionate about cooking
    vegetarian meals."
   → Multiple high-confidence insights with context
```

#### 4. Review Insights
Periodically check extracted insights for accuracy:
```bash
GET /api/v1/entities/{profileId}/conversation

# Review extractedInsights array
# If incorrect, delete and restart conversation
```

#### 5. Combine with Structured Data
Use conversation to **enhance** structured profile data, not replace it:
- **Structured form**: Core preferences (activity level, risk tolerance)
- **Conversation**: Nuances (favorite hiking trails, photography style)
- **Together**: Complete picture for matching

---

### Example: Complete Conversational Profile Building

```bash
# Step 1: Create base profile with structured data
POST /api/v1/entities
{
  "name": "Sarah",
  "ownedByUserId": "user-123",
  "adventurePreferences": {
    "activityLevel": 8,
    "riskTolerance": 7
  }
}
# Response: { "id": "profile-abc" }

# Step 2: Start conversation to add depth
POST /api/v1/entities/profile-abc/conversation
{
  "message": "Sarah loves outdoor activities, especially hiking",
  "userId": "user-123"
}
# Response: AI asks follow-up about hiking preferences

# Step 3: Continue conversation
POST /api/v1/entities/profile-abc/conversation
{
  "message": "She prefers challenging mountain trails and loves sunrise hikes",
  "userId": "user-123"
}
# Response: AI extracts insights about difficulty preference and timing

# Step 4: Add more context
POST /api/v1/entities/profile-abc/conversation
{
  "message": "She's also really into photography, especially landscape shots",
  "userId": "user-123"
}
# Response: AI extracts photography-related insights

# Step 5: Review what was learned
GET /api/v1/entities/profile-abc/conversation
# Response: Full conversation history + extracted insights

# Step 6: Wait for nightly processing
# Nightly at 2 AM UTC:
#   - Summary generated including conversation insights
#   - EntityEmbedding created with Status=Pending

# Step 7: Wait for embedding generation
# Every 30 minutes:
#   - Embedding vector generated
#   - Status changed to Generated
#   - Profile ready for semantic matching
```

**Result**: Profile with both structured preferences AND conversational depth, leading to more accurate matching.

---

## Embedding Status Monitoring

### Check Embedding Status (Admin API)

**Endpoint**: `GET /api/super/embeddings/{profileId}`

**Response**:
```json
{
  "id": "embedding_profile-abc123",
  "profileId": "profile-abc123",
  "profileSummary": "Sarah is an outdoor enthusiast...",
  "summaryHash": "a1b2c3d4e5f6...",
  "status": "Generated",
  "embeddingModel": "text-embedding-3-small",
  "dimensions": 1536,
  "embedding": [0.123, -0.456, 0.789, ...],
  "generatedAt": "2025-01-15T11:00:00Z",
  "profileLastModified": "2025-01-15T10:30:00Z",
  "retryCount": 0,
  "errorMessage": null
}
```

**Status Values**:
- `Pending` - Summary created, waiting for embedding generation
- `Generated` - Embedding vector successfully created
- `Failed` - Embedding generation failed after max retries (5)

---

### Get Embedding Statistics (Admin API)

**Endpoint**: `GET /api/super/embeddings/stats`

**Response**:
```json
{
  "pending": 12,
  "generated": 485,
  "failed": 3,
  "totalProfiles": 500
}
```

**Use Case**: Monitor system health and processing backlog

---

### Get All Embeddings by Status (Admin API)

**Endpoint**: `GET /api/super/embeddings?status={status}&limit={limit}`

**Parameters**:
- `status`: `Pending`, `Generated`, or `Failed`
- `limit`: Optional, max results to return (default: all)

**Example**: `GET /api/super/embeddings?status=Pending&limit=50`

**Response**:
```json
[
  {
    "id": "embedding_profile-abc123",
    "profileId": "profile-abc123",
    "status": "Pending",
    "generatedAt": "2025-01-15T02:00:00Z"
    // ...
  },
  // ... more embeddings
]
```

**Use Case**: Find all pending embeddings waiting to be processed

---

## Manual Operations

### Force Regenerate Embedding (Admin API)

**Endpoint**: `POST /api/super/embeddings/{profileId}/regenerate`

**Request Body** (Optional):
```json
{
  "model": "text-embedding-3-large"  // Optional: switch to large model
}
```

**Response**:
```json
{
  "message": "Embedding queued for regeneration",
  "profileId": "profile-abc123",
  "status": "Pending",
  "requestedModel": "text-embedding-3-large"
}
```

**What Happens Next**:
1. EntityEmbedding document status is set to `Pending`
2. If model specified, `embeddingModel` field is updated as a hint
3. Next 30-minute timer picks it up and regenerates

**Use Cases**:
- Upgrade specific profiles to `text-embedding-3-large` for better matching
- Fix failed embeddings after resolving issues
- Force refresh after major profile changes

---

### Delete Embedding (Admin API)

**Endpoint**: `DELETE /api/super/embeddings/{profileId}`

**Response**: `204 No Content`

**What Happens Next**:
- EntityEmbedding document is deleted from Cosmos DB
- Next nightly run will recreate it (if profile still exists)

**Use Case**: Clean up orphaned embeddings for deleted profiles

---

### Trigger Manual Processing (Admin API)

**Endpoint**: `POST /api/super/embeddings/process`

**Response**:
```json
{
  "message": "Processing triggered manually",
  "totalPending": 12,
  "batchSize": 50,
  "estimatedBatches": 1
}
```

**What Happens Next**:
- Immediately processes up to 50 pending embeddings (doesn't wait for 30-min timer)
- Same logic as automatic timer-triggered processing

**Use Case**: Process embeddings immediately after bulk profile imports

---

## Troubleshooting

### Why is my embedding still "Pending"?

**Common Causes**:
1. **Normal delay**: Processing runs every 30 minutes, so up to 30-min wait is normal
2. **High volume**: If >50 embeddings are pending, some will wait for next batch
3. **OpenAI API issues**: Rate limits or API downtime can delay processing
4. **Configuration disabled**: Check `ENABLE_EMBEDDING_GENERATION=true` in settings

**Check Status**: `GET /api/super/embeddings/{profileId}`

**Manual Fix**: `POST /api/super/embeddings/process` to trigger immediate processing

---

### Why is my embedding "Failed"?

**Common Causes**:
1. **OpenAI API errors**: API key invalid, quota exceeded, network issues
2. **Invalid summary**: Profanity, empty text, or malformed content
3. **Max retries exceeded**: Failed 5 times and gave up

**Check Error**:
```bash
GET /api/super/embeddings/{profileId}
```

Look at the `errorMessage` field for details.

**Manual Fix**:
```bash
POST /api/super/embeddings/{profileId}/regenerate
```

This resets retry count and tries again.

---

### How do I upgrade a profile to text-embedding-3-large?

**Option 1: Admin API** (Recommended)
```bash
POST /api/super/embeddings/{profileId}/regenerate
Content-Type: application/json

{
  "model": "text-embedding-3-large"
}
```

**Option 2: Direct Database Update**
1. Open Cosmos DB Data Explorer
2. Find EntityEmbedding document: `embedding_{profileId}`
3. Set `status = 0` (Pending)
4. Set `embeddingModel = "text-embedding-3-large"`
5. Save document
6. Wait for next processing run (30 min)

---

### How do I monitor processing in real-time?

**Application Insights Query**:
```kusto
traces
| where message contains "ProcessPendingEmbeddings"
| where timestamp > ago(1h)
| order by timestamp desc
```

**Look for log messages**:
- `"Starting pending embeddings processing"`
- `"Found {Count} pending embeddings to process"`
- `"Successfully generated {Count} embeddings"`
- `"Pending embeddings processing completed"`

---

### Configuration Reference

**Environment Variables** (local.settings.json or Azure App Settings):

| Key | Default | Description |
|-----|---------|-------------|
| `ApiKeys__OpenAI` | Required | OpenAI API key |
| `Embeddings__DefaultModel` | `text-embedding-3-small` | Default embedding model |
| `ENABLE_EMBEDDING_GENERATION` | `true` | Enable/disable processing |
| `EMBEDDING_BATCH_SIZE` | `50` | Max embeddings per batch |
| `EMBEDDING_MAX_RETRIES` | `5` | Max retry attempts before marking as Failed |
| `EMBEDDING_INFRASTRUCTURE_ENABLED` | `true` | Enable/disable summary generation |

---

## Model Comparison

| Model | Dimensions | Cost per 1M tokens | Use Case |
|-------|-----------|-------------------|----------|
| `text-embedding-3-small` | 1536 | $0.02 | Default for all profiles, cost-effective |
| `text-embedding-3-large` | 3072 | $0.13 | Premium profiles needing higher accuracy |

**Recommendation**: Start with `text-embedding-3-small` for all profiles. Upgrade specific profiles to `text-embedding-3-large` only when:
- Profile has very complex preferences
- User is a premium subscriber
- Higher matching accuracy is critical

**Cost Comparison** (1000 profiles):
- Small model: 1000 × $0.00002 = **$0.02**
- Large model: 1000 × $0.00013 = **$0.13**

---

## API Quick Reference

| Operation | Method | Endpoint |
|-----------|--------|----------|
| **Profile Management** | | |
| Create Profile | POST | `/api/v1/entities` |
| Update Profile | PUT | `/api/v1/entities/{id}` |
| Get Profile | GET | `/api/v1/entities/{id}` |
| Delete Profile | DELETE | `/api/v1/entities/{id}` |
| **Conversational Profile Building** | | |
| Send Conversation Message | POST | `/api/v1/entities/{profileId}/conversation` |
| Get Conversation History | GET | `/api/v1/entities/{profileId}/conversation` |
| Delete Conversation | DELETE | `/api/v1/entities/{profileId}/conversation` |
| **Embedding Status Monitoring (Admin)** | | |
| Get Embedding Status | GET | `/api/super/embeddings/{profileId}` |
| Get Embedding Stats | GET | `/api/super/embeddings/stats` |
| List Embeddings | GET | `/api/super/embeddings?status={status}` |
| **Manual Operations (Admin)** | | |
| Regenerate Embedding | POST | `/api/super/embeddings/{profileId}/regenerate` |
| Delete Embedding | DELETE | `/api/super/embeddings/{profileId}` |
| Trigger Processing | POST | `/api/super/embeddings/process` |

---

## Support

For issues or questions:
- Check Application Insights logs for detailed error messages
- Review configuration settings in local.settings.json
- Verify OpenAI API key is valid and has sufficient quota
- Monitor Cosmos DB for EntityEmbedding documents

**Nightly Summary Function**: Runs at 2 AM UTC daily
**Embedding Processing Function**: Runs every 30 minutes
**Batch Size**: 50 embeddings per processing run
**Max Retries**: 5 attempts before marking as Failed
