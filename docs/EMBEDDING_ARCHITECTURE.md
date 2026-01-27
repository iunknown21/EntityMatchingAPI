# Profile Embedding System - Technical Architecture

## Overview

This document describes the complete technical architecture and data flow for the Profile Embedding System. It covers the entire pipeline from profile creation to vector embedding generation, including all services, data models, and scheduled functions.

---

## Table of Contents

- [System Architecture](#system-architecture)
- [Data Flow](#data-flow)
- [Core Components](#core-components)
- [Data Models](#data-models)
- [Processing Pipeline](#processing-pipeline)
- [Error Handling & Retry Logic](#error-handling--retry-logic)
- [Performance & Optimization](#performance--optimization)
- [Monitoring & Observability](#monitoring--observability)

---

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         USER INTERACTIONS                            │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       AZURE FUNCTIONS (API)                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │  Profiles    │  │ Conversation │  │    Admin     │              │
│  │  Functions   │  │  Functions   │  │  Functions   │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      SERVICE LAYER                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │   Profile    │  │ Conversation │  │ProfileSummary│              │
│  │   Service    │  │   Service    │  │   Service    │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
│                                                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │  Embedding   │  │ Embedding    │  │    OpenAI    │              │
│  │   Storage    │  │   Service    │  │ Embedding    │              │
│  │   Service    │  │ (Interface)  │  │   Service    │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    DATA & EXTERNAL SERVICES                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │  Cosmos DB   │  │   OpenAI     │  │ Application  │              │
│  │              │  │Embeddings API│  │  Insights    │              │
│  │ - profiles   │  │              │  │              │              │
│  │ - embeddings │  │              │  │              │              │
│  │ - conversatio│  │              │  │              │              │
│  │   ns (multi- │  │              │  │              │              │
│  │   document)  │  │              │  │              │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    SCHEDULED FUNCTIONS                               │
│  ┌──────────────────────────────────────────────────────┐           │
│  │  GenerateProfileSummariesFunction                    │           │
│  │  Trigger: Nightly at 2 AM UTC                        │           │
│  │  Schedule: "0 0 2 * * *"                             │           │
│  └──────────────────────────────────────────────────────┘           │
│                          │                                           │
│                          ▼                                           │
│  ┌──────────────────────────────────────────────────────┐           │
│  │  ProcessPendingEmbeddingsFunction                    │           │
│  │  Trigger: Every 30 minutes                           │           │
│  │  Schedule: "0 */30 * * * *"                          │           │
│  └──────────────────────────────────────────────────────┘           │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow

### Two Embedding Generation Paths

EntityMatchingAPI supports TWO ways to generate embeddings:

1. **Server-Generated (Automatic)**: Nightly batch processing of profile summaries
2. **Client-Uploaded (Privacy-First)**: Users generate embeddings locally and upload vectors only

---

### Path 1: Server-Generated Embeddings (Automatic)

```
┌─────────────────────────────────────────────────────────────────────┐
│ PHASE 1: Profile Creation/Update                                    │
└─────────────────────────────────────────────────────────────────────┘

User Creates/Updates Profile
         │
         ▼
┌─────────────────────┐
│ POST /api/profiles  │  ──────┐
└─────────────────────┘        │
                               ▼
                    ┌──────────────────────┐
                    │   EntityService     │
                    │  .AddProfileAsync()  │
                    └──────────────────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │  Cosmos DB           │
                    │  Container: profiles │
                    │                      │
                    │  Document saved with:│
                    │  - id: profile-123   │
                    │  - lastModified: NOW │
                    └──────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ PHASE 2: Nightly Summary Generation (2 AM UTC)                      │
└─────────────────────────────────────────────────────────────────────┘

Scheduled Trigger (2 AM UTC)
         │
         ▼
┌───────────────────────────────────┐
│ GenerateProfileSummariesFunction  │
└───────────────────────────────────┘
         │
         ├─────────────────────────────────────────────────────┐
         │                                                      │
         ▼                                                      ▼
┌────────────────────┐                              ┌──────────────────┐
│ Query Cosmos DB    │                              │ For each profile │
│ for all profiles   │                              └──────────────────┘
│ (id, lastModified) │                                       │
└────────────────────┘                                       │
         │                                                    │
         ▼                                                    ▼
┌────────────────────────────────────┐         ┌─────────────────────────┐
│ Get existing EntityEmbedding      │         │ Load full profile       │
│ from Cosmos DB (if exists)         │         │ Load conversation       │
└────────────────────────────────────┘         │ (if exists)             │
         │                                      └─────────────────────────┘
         ▼                                                    │
┌────────────────────────────────────┐                       ▼
│ Check if needs regeneration:       │         ┌─────────────────────────┐
│                                    │         │ ProfileSummaryService   │
│ IF (no embedding exists)           │         │ .GenerateSummaryAsync() │
│    → Process                       │         └─────────────────────────┘
│ ELSE IF (profile.lastModified >   │                       │
│          embedding.profileLast     │                       │
│          Modified)                 │                       ▼
│    → Process                       │         ┌─────────────────────────┐
│ ELSE                               │         │ Generate text summary:  │
│    → Skip                          │         │                         │
└────────────────────────────────────┘         │ "Sarah is an outdoor    │
         │                                      │ enthusiast who loves    │
         │                                      │ hiking and photography. │
         │                                      │ Preferences:            │
         │                                      │ - Activity level: 8/10  │
         ▼                                      │ - Risk tolerance: 7/10  │
┌────────────────────────────────────┐         │ - Favorite genres:      │
│ Generate SHA256 hash of summary    │         │   Adventure, Docs       │
│ Compare with existing hash         │         │ ..."                    │
│                                    │         └─────────────────────────┘
│ IF hash unchanged:                 │                       │
│    → Skip (no actual changes)      │                       ▼
│ ELSE:                              │         ┌─────────────────────────┐
│    → Continue                      │         │ Compute SHA256 hash     │
└────────────────────────────────────┘         │ of summary text         │
         │                                      └─────────────────────────┘
         ▼                                                    │
┌────────────────────────────────────┐                       ▼
│ Create/Update EntityEmbedding:    │         ┌─────────────────────────┐
│                                    │         │ Upsert to Cosmos DB:    │
│ {                                  │         │ Container: embeddings   │
│   id: "embedding_profile-123",    │         │                         │
│   profileId: "profile-123",       │         │ Document:               │
│   profileSummary: "Sarah is...",  │◀────────│ - id: embedding_prof... │
│   summaryHash: "a1b2c3...",       │         │ - profileId             │
│   status: EmbeddingStatus.Pending,│         │ - profileSummary        │
│   generatedAt: NOW,               │         │ - summaryHash           │
│   profileLastModified: NOW,       │         │ - status: Pending       │
│   embedding: null,                │         │ - embedding: null       │
│   embeddingModel: null,           │         │ - retryCount: 0         │
│   dimensions: null,               │         └─────────────────────────┘
│   retryCount: 0,                  │
│   errorMessage: null              │
│ }                                  │
└────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ PHASE 3: Embedding Generation (Every 30 minutes)                    │
└─────────────────────────────────────────────────────────────────────┘

Scheduled Trigger (*/30 minutes)
         │
         ▼
┌───────────────────────────────────┐
│ ProcessPendingEmbeddingsFunction  │
└───────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Check if enabled:                  │
│ ENABLE_EMBEDDING_GENERATION=true   │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ EmbeddingStorageService                            │
│ .GetEmbeddingsByStatusAsync(                       │
│    EmbeddingStatus.Pending,                        │
│    limit: 50  // EMBEDDING_BATCH_SIZE              │
│ )                                                   │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Cosmos DB Query:                   │
│                                    │
│ SELECT * FROM c                    │
│ WHERE c.status = 0  // Pending     │
│ LIMIT 50                           │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Result: List<EntityEmbedding>     │
│ [                                  │
│   { profileId: "profile-123",      │
│     profileSummary: "Sarah is..." },│
│   { profileId: "profile-456",      │
│     profileSummary: "Mike is..." },│
│   ...                              │
│ ]                                  │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Filter out embeddings that have    │
│ reached max retries (default: 5)   │
│                                    │
│ For each with retryCount >= 5:     │
│   - Set status = Failed            │
│   - Set errorMessage               │
│   - Save to Cosmos DB              │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ Build batch for OpenAI:                            │
│                                                    │
│ summariesToEmbed = [                               │
│   "Sarah is an outdoor enthusiast...",            │
│   "Mike is a foodie who loves...",                │
│   ...                                              │
│ ]                                                  │
│                                                    │
│ indexMap = {                                       │
│   0 → EntityEmbedding(profile-123),              │
│   1 → EntityEmbedding(profile-456),              │
│   ...                                              │
│ }                                                  │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ OpenAIEmbeddingService                             │
│ .GenerateEmbeddingsBatchAsync(summariesToEmbed)    │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ HTTP POST                                          │
│ https://api.openai.com/v1/embeddings               │
│                                                    │
│ Headers:                                           │
│   Authorization: Bearer sk-...                     │
│   Content-Type: application/json                   │
│                                                    │
│ Body:                                              │
│ {                                                  │
│   "input": [                                       │
│     "Sarah is an outdoor enthusiast...",          │
│     "Mike is a foodie who loves...",              │
│     ...                                            │
│   ],                                               │
│   "model": "text-embedding-3-small",              │
│   "encoding_format": "float"                       │
│ }                                                  │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ OpenAI API Response:                               │
│                                                    │
│ {                                                  │
│   "object": "list",                               │
│   "data": [                                        │
│     {                                              │
│       "object": "embedding",                      │
│       "index": 0,                                  │
│       "embedding": [                              │
│         0.0234, -0.0567, 0.0891, ..., // 1536 dims│
│       ]                                            │
│     },                                             │
│     {                                              │
│       "index": 1,                                  │
│       "embedding": [                              │
│         -0.0123, 0.0456, -0.0789, ...,           │
│       ]                                            │
│     },                                             │
│     ...                                            │
│   ],                                               │
│   "model": "text-embedding-3-small",              │
│   "usage": {                                       │
│     "prompt_tokens": 512,                         │
│     "total_tokens": 512                           │
│   }                                                │
│ }                                                  │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ Parse response into Dictionary<int, float[]>:      │
│                                                    │
│ embeddingVectors = {                               │
│   0 → [0.0234, -0.0567, ...],  // 1536 floats    │
│   1 → [-0.0123, 0.0456, ...],                     │
│   ...                                              │
│ }                                                  │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ For each embedding in response:                    │
│                                                    │
│   index = 0                                        │
│   vector = [0.0234, -0.0567, ...]                │
│   embeddingDoc = indexMap[index]                  │
│                                                    │
│   Update document:                                 │
│     embeddingDoc.embedding = vector                │
│     embeddingDoc.status = Generated                │
│     embeddingDoc.embeddingModel = "text-embed..." │
│     embeddingDoc.dimensions = 1536                 │
│     embeddingDoc.errorMessage = null               │
│     embeddingDoc.retryCount = 0                    │
│                                                    │
│   EmbeddingStorageService                          │
│     .UpsertEmbeddingAsync(embeddingDoc)           │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ Cosmos DB Update:                                  │
│ Container: embeddings                              │
│                                                    │
│ Document:                                          │
│ {                                                  │
│   id: "embedding_profile-123",                    │
│   profileId: "profile-123",                       │
│   profileSummary: "Sarah is...",                  │
│   summaryHash: "a1b2c3...",                       │
│   status: 1,  // Generated                        │
│   embedding: [0.0234, -0.0567, ...],  // 1536     │
│   embeddingModel: "text-embedding-3-small",       │
│   dimensions: 1536,                                │
│   generatedAt: "2025-01-15T02:30:00Z",           │
│   profileLastModified: "2025-01-15T01:00:00Z",   │
│   retryCount: 0,                                   │
│   errorMessage: null                               │
│ }                                                  │
└────────────────────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────────────────────┐
│ Log Statistics:                                    │
│                                                    │
│ "Pending embeddings processing completed in       │
│  1234ms. Stats: Total Pending=12, Generated=12,   │
│  Failed=0, Retried=0, Errors=0"                   │
│                                                    │
│ Application Insights:                              │
│   - Tokens used: 512                              │
│   - Cost: ~$0.00001                               │
│   - Processing time: 1234ms                        │
│   - Success rate: 100%                            │
└────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ PHASE 4: Error Handling & Retry (If OpenAI API fails)               │
└─────────────────────────────────────────────────────────────────────┘

OpenAI API Returns Error
         │
         ▼
┌────────────────────────────────────┐
│ OpenAIEmbeddingService             │
│ Retry Logic (max 3 attempts):      │
│                                    │
│ Attempt 1: FAIL (429 Rate Limit)   │
│   → Wait 2^1 = 2 seconds           │
│ Attempt 2: FAIL (500 Server Error) │
│   → Wait 2^2 = 4 seconds           │
│ Attempt 3: FAIL                    │
│   → Return null                    │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ ProcessPendingEmbeddingsFunction   │
│ Handles failed embeddings:         │
│                                    │
│ For each embedding that failed:    │
│   embeddingDoc.retryCount++        │
│   embeddingDoc.errorMessage =      │
│     "Embedding generation failed..." │
│                                    │
│   IF retryCount >= 5:              │
│     embeddingDoc.status = Failed   │
│   ELSE:                            │
│     Keep status = Pending          │
│     (will retry on next run)       │
│                                    │
│   Save to Cosmos DB                │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Next Processing Run (30 min later) │
│ Will retry pending embeddings      │
│ (up to 5 total attempts)           │
└────────────────────────────────────┘
```

---

### Path 2: Client-Uploaded Embeddings (Privacy-First) ⭐ NEW

```
┌─────────────────────────────────────────────────────────────────────┐
│ CLIENT-SIDE: User's Device/Browser                                  │
└─────────────────────────────────────────────────────────────────────┘

User provides resume/document text
         │
         ▼
┌────────────────────────────────────┐
│ Client calls OpenAI API directly   │
│ (from user's device, NOT server)   │
│                                    │
│ const embedding = await openai     │
│   .embeddings.create({             │
│     model: "text-embedding-3-small"│
│     input: resumeText              │
│   });                              │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Receives 1536-dimensional vector:  │
│ [0.123, -0.456, 0.789, ...]       │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ CRITICAL: Discard original text!   │
│ Original resume/document text is   │
│ DELETED from memory                │
│ Only the vector remains            │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ POST /api/v1/entities/{id}/        │
│      embeddings/upload             │
│                                    │
│ Body:                              │
│ {                                  │
│   "Embedding": [0.123, -0.456,...]│
│   "EmbeddingModel": "text-embed..." │
│ }                                  │
└────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│ SERVER-SIDE: EntityMatchingAPI                                      │
└─────────────────────────────────────────────────────────────────────┘

EmbeddingUploadFunctions
         │
         ▼
┌────────────────────────────────────┐
│ Validate Request:                  │
│ ✓ Embedding array exists           │
│ ✓ Exactly 1536 dimensions          │
│ ✓ All floats valid (no NaN/Inf)   │
│ ✓ Profile exists                   │
│ ✓ User has permission (optional)   │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Create EntityEmbedding:           │
│                                    │
│ {                                  │
│   id: "embedding_profile-123",    │
│   profileId: "profile-123",       │
│   profileSummary: "[CLIENT_       │
│                     UPLOADED]",   │  ← Privacy marker!
│   summaryHash: hash("[CLIENT...") │
│   embedding: [0.123, -0.456, ...],│  ← Client vector
│   embeddingModel: "text-embed..." │
│   dimensions: 1536,                │
│   status: Generated,               │  ← Immediate!
│   generatedAt: NOW,                │
│   retryCount: 0                    │
│ }                                  │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ EmbeddingStorageService            │
│ .UpsertEmbeddingAsync()            │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ Cosmos DB: embeddings container    │
│                                    │
│ Document saved with:               │
│ - Vector: [0.123, -0.456, ...]    │
│ - Summary: "[CLIENT_UPLOADED]"    │
│ - Status: Generated                │
│                                    │
│ ✅ Embedding immediately available │
│    for vector search!              │
└────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│ PRIVACY GUARANTEE                                                    │
└─────────────────────────────────────────────────────────────────────┘

Server receives: [0.123, -0.456, 0.789, ..., 1536 floats]
Server NEVER sees: Resume text, name, contact info, work history

Even with database access, an attacker gets:
  ❌ No resume text
  ❌ No personal information
  ❌ Just meaningless numbers

Mathematical guarantee: Cannot reconstruct original text from embedding vector
```

### Key Differences: Server-Generated vs Client-Uploaded

| Aspect | Server-Generated | Client-Uploaded |
|--------|-----------------|-----------------|
| **Who generates embedding?** | Server (via OpenAI API) | Client (user's device) |
| **Source text stored?** | ✅ Yes (as ProfileSummary) | ❌ Never (placeholder only) |
| **PII exposure?** | ⚠️ Server sees profile summary | ✅ Zero PII to server |
| **Processing latency** | 24 hours (nightly + 30 min) | Immediate (as soon as uploaded) |
| **Status** | Pending → Generated | Generated (immediate) |
| **ProfileSummary value** | "Sarah is an outdoor enthusiast..." | "[CLIENT_UPLOADED]" |
| **Regeneration** | ✅ Yes (on profile changes) | ❌ No (skipped by nightly job) |
| **Use case** | Conversational profiling | Resume/document upload |
| **Privacy level** | Standard | Maximum |

### Skipping Client-Uploaded Embeddings During Regeneration

The `GenerateProfileSummariesFunction` (nightly job) automatically skips client-uploaded embeddings:

```csharp
// In GenerateProfileSummariesFunction.cs (line 95-100)
if (existing.ProfileSummary == "[CLIENT_UPLOADED]")
{
    // Client-uploaded embedding - never regenerate
    _logger.LogDebug("Skipping {ProfileId} - has client-uploaded embedding", profile.Id);
    stats.SkippedProfiles++;
    // Continue to next profile
}
```

**Why?**
- Client embeddings represent user-provided vectors from sources we don't have (resumes, documents)
- Regenerating would overwrite user's custom embedding with server-generated one
- Privacy guarantee: Once uploaded, client embedding stays until user explicitly replaces it

---

## Core Components

### 1. ProfileSummaryService

**Location**: `EntityMatching.Infrastructure/Services/ProfileSummaryService.cs`

**Purpose**: Generate comprehensive text summaries from UserProfile + ConversationContext

**Key Methods**:
- `GenerateSummaryAsync(UserProfile profile, ConversationContext? conversation)` → `SummaryResult`

**Summary Generation Logic**:
1. Extract data from 13+ preference categories
2. Include conversation insights (if available)
3. Format as natural language text
4. Return summary + metadata (word count, categories included, etc.)

**Example Summary**:
```
Sarah is an outdoor enthusiast who loves hiking and photography.

ENTERTAINMENT: Favorite movie genres include Adventure and Documentary.
Enjoys Indie and Folk music.

ADVENTURE: High activity level (8/10) with strong risk tolerance (7/10)
and loves trying new experiences (novelty: 9/10).

SENSORY: Prefers quiet environments (noise sensitivity: 3/10) and
natural lighting. Comfortable in small groups.

INSIGHTS FROM CONVERSATIONS:
- Mentioned loving sunrise hikes in the mountains
- Prefers vegetarian restaurants
- Interested in wildlife photography workshops
```

---

### 2. EmbeddingStorageService

**Location**: `EntityMatching.Infrastructure/Services/EmbeddingStorageService.cs`

**Purpose**: CRUD operations for EntityEmbedding documents in Cosmos DB

**Key Methods**:
- `GetEmbeddingAsync(string profileId)` → `EntityEmbedding?`
- `UpsertEmbeddingAsync(EntityEmbedding embedding)` → `EntityEmbedding`
- `DeleteEmbeddingAsync(string profileId)` → `void`
- `GetEmbeddingsByStatusAsync(EmbeddingStatus status, int? limit)` → `List<EntityEmbedding>`
- `GetEmbeddingCountsByStatusAsync()` → `Dictionary<EmbeddingStatus, int>`

**Cosmos DB Configuration**:
- Database: `EntityMatchingDb`
- Container: `embeddings`
- Partition Key: `/profileId`
- Auto-creates container on initialization (serverless mode)

**Query Example**:
```csharp
var query = new QueryDefinition("SELECT * FROM c WHERE c.status = @status")
    .WithParameter("@status", (int)EmbeddingStatus.Pending);
```

**Important**: Uses `(int)status` instead of `status.ToString()` because Cosmos DB stores enum as integer.

---

### 3. OpenAIEmbeddingService

**Location**: `EntityMatching.Infrastructure/Services/OpenAIEmbeddingService.cs`

**Purpose**: Generate vector embeddings via OpenAI Embeddings API

**Key Methods**:
- `GenerateEmbeddingAsync(string text)` → `float[]?` (single embedding)
- `GenerateEmbeddingAsync(string text, string model)` → `float[]?` (with model override)
- `GenerateEmbeddingsBatchAsync(List<string> texts, string? model)` → `Dictionary<int, float[]>?` (batch)

**Configuration**:
- API Key: `ApiKeys__OpenAI`
- Default Model: `Embeddings__DefaultModel` (default: "text-embedding-3-small")
- Max Retries: `EMBEDDING_MAX_RETRIES` (default: 3 at service level, 5 at function level)
- Timeout: 2 minutes (configured in DI registration)

**Retry Logic**:
- Attempt 1: Immediate
- Attempt 2: Wait 2 seconds
- Attempt 3: Wait 4 seconds
- Rate limiting (429): Automatically retries with exponential backoff

**Batch Processing**:
- Max inputs per API call: 2048 (OpenAI limit)
- Default batch size: 50 (configurable via `EMBEDDING_BATCH_SIZE`)
- Returns dictionary mapping input index to embedding vector

**Model Support**:
| Model | Dimensions | Cost per 1M tokens |
|-------|-----------|-------------------|
| `text-embedding-3-small` | 1536 | $0.02 |
| `text-embedding-3-large` | 3072 | $0.13 |

---

### 4. GenerateProfileSummariesFunction

**Location**: `EntityMatching.Functions/GenerateProfileSummariesFunction.cs`

**Trigger**: Timer - Nightly at 2 AM UTC
**Schedule**: `"0 0 2 * * *"`

**Process Flow**:
1. Check if enabled (`EMBEDDING_INFRASTRUCTURE_ENABLED`)
2. Query all profiles (id + lastModified only) for efficiency
3. For each profile:
   - Check if EntityEmbedding exists
   - Check if profile was modified since embedding generated
   - If needs processing:
     - Load full profile + conversation
     - Generate summary
     - Compute hash
     - Compare with existing hash (skip if unchanged)
     - Create/update EntityEmbedding with Status=Pending
4. Process in batches (default: 10 at a time)
5. Log statistics

**Optimization**:
- Uses minimal projection (`SELECT c.id, c.lastModified`) to reduce RU costs
- Only loads full profiles that need processing
- Uses SHA256 hash to detect actual content changes (not just timestamp)

---

### 5. ProcessPendingEmbeddingsFunction

**Location**: `EntityMatching.Functions/ProcessPendingEmbeddingsFunction.cs`

**Trigger**: Timer - Every 30 minutes
**Schedule**: `"0 */30 * * * *"`

**Process Flow**:
1. Check if enabled (`ENABLE_EMBEDDING_GENERATION`)
2. Query for pending embeddings (limit: `EMBEDDING_BATCH_SIZE`, default 50)
3. Filter out embeddings that exceeded max retries (default: 5)
4. Build batch:
   - Extract summaries into list
   - Create index map (index → EntityEmbedding)
5. Call OpenAI batch API
6. For each returned embedding:
   - Update EntityEmbedding document with vector
   - Set status = Generated
   - Set model name and dimensions
   - Reset retry count
   - Save to Cosmos DB
7. Handle failures:
   - Increment retry count
   - Set error message
   - If max retries reached: mark as Failed
   - Otherwise: keep as Pending for next run
8. Log statistics

**Configuration**:
- `ENABLE_EMBEDDING_GENERATION`: Enable/disable processing
- `EMBEDDING_BATCH_SIZE`: Max embeddings per run (default: 50)
- `EMBEDDING_MAX_RETRIES`: Max attempts before marking Failed (default: 5)

---

## Data Models

### UserProfile

**Location**: `EntityMatching.Core/Models/UserProfile.cs`
**Cosmos Container**: `profiles`
**Partition Key**: `/id`

**Key Fields**:
```csharp
public class UserProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }  // ← Used for change detection
    public string OwnedByUserId { get; set; }

    // 13+ preference categories
    public EntertainmentPreferences EntertainmentPreferences { get; set; }
    public AdventurePreferences AdventurePreferences { get; set; }
    public SensoryPreferences SensoryPreferences { get; set; }
    // ... etc
}
```

---

### EntityEmbedding

**Location**: `EntityMatching.Core/Models/EntityEmbedding.cs`
**Cosmos Container**: `embeddings`
**Partition Key**: `/profileId`

**Full Schema**:
```csharp
public class EntityEmbedding
{
    // Identity
    public string Id { get; set; }  // Format: "embedding_{profileId}"
    public string ProfileId { get; set; }

    // Summary Data
    public string? ProfileSummary { get; set; }  // Text summary
    public string SummaryHash { get; set; }  // SHA256 for change detection
    public SummaryMetadata? SummaryMetadata { get; set; }  // Word count, etc.

    // Embedding Data
    public float[]? Embedding { get; set; }  // Vector (1536 or 3072 floats)
    public string? EmbeddingModel { get; set; }  // "text-embedding-3-small"
    public int? Dimensions { get; set; }  // 1536 or 3072

    // Status & Timestamps
    public EmbeddingStatus Status { get; set; }  // Pending/Generated/Failed
    public DateTime GeneratedAt { get; set; }  // When summary was generated
    public DateTime ProfileLastModified { get; set; }  // Profile timestamp

    // Error Handling
    public int RetryCount { get; set; }  // Number of failed attempts
    public string? ErrorMessage { get; set; }  // Last error details
}

public enum EmbeddingStatus
{
    Pending = 0,    // Summary created, waiting for embedding
    Generated = 1,  // Embedding vector successfully created
    Failed = 2      // Failed after max retries
}
```

**Helper Methods**:
```csharp
// Generate document ID from profile ID
public static string GenerateId(string profileId)
    => $"embedding_{profileId}";

// Compute SHA256 hash of summary for change detection
public static string ComputeHash(string text)
    => SHA256(text);

// Check if summary needs regeneration
public bool NeedsRegeneration(DateTime profileLastModified)
    => profileLastModified > this.ProfileLastModified;
```

---

### ConversationContext

**Location**: `EntityMatching.Core/Models/Conversation/ConversationContext.cs`
**Cosmos Container**: `conversations` (multi-document architecture)
**Partition Key**: `/profileId`

**Architecture Note**: Conversations use a multi-document design to handle large conversation histories:
- **ConversationDocument**: Individual documents (1-N per profile), auto-split at 1.5MB
- **ConversationMetadata**: Single metadata document per profile for tracking
- **ConversationContext**: Aggregated view returned by `GetConversationHistoryAsync()`

**Schema** (Aggregated View):
```csharp
public class ConversationContext
{
    public string Id { get; set; }
    public string ProfileId { get; set; }
    public string UserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }

    // Conversation history (aggregated from multiple documents)
    public List<ConversationChunk> ConversationChunks { get; set; }

    // AI-extracted insights (aggregated from multiple documents)
    public List<ExtractedInsight> ExtractedInsights { get; set; }

    // Aggregates multiple ConversationDocument instances
    public static ConversationContext Aggregate(List<ConversationDocument> documents);
}

public class ExtractedInsight
{
    public string Category { get; set; }  // "hobby", "preference", etc.
    public string Insight { get; set; }  // "enjoys hiking"
    public float Confidence { get; set; }  // 0.0-1.0
    public string SourceChunk { get; set; }  // Original text
    public DateTime ExtractedAt { get; set; }
}
```

**Integration**:
- ProfileSummaryService calls `GetConversationHistoryAsync()` which transparently aggregates all conversation documents
- Supports unlimited conversation history (100-1000+ documents per profile)
- See `DATABASE_SCHEMA.md` section 3 for detailed multi-document architecture

---

## Processing Pipeline

### Timeline Example

**Day 1 - 10:30 AM**: User creates profile for "Sarah"
```
POST /api/profiles
→ Profile saved to Cosmos DB
→ LastModified: 2025-01-15T10:30:00Z
```

**Day 1 - 2:00 AM Next Day**: Nightly summary generation runs
```
GenerateProfileSummariesFunction triggered
→ Finds profile-sarah (lastModified: 10:30 AM)
→ No embedding exists yet
→ Loads full profile + conversation (none yet)
→ Generates summary (512 tokens)
→ Computes hash: "a1b2c3d4..."
→ Creates EntityEmbedding:
    - profileSummary: "Sarah is..."
    - summaryHash: "a1b2c3d4..."
    - status: Pending
    - embedding: null
→ Saves to Cosmos DB
```

**Day 1 - 2:30 AM**: First embedding processing run
```
ProcessPendingEmbeddingsFunction triggered
→ Queries for status=Pending, limit=50
→ Finds 1 embedding (profile-sarah)
→ Calls OpenAI batch API:
    POST /v1/embeddings
    { input: ["Sarah is..."], model: "text-embedding-3-small" }
→ Receives embedding: [0.0234, -0.0567, ..., 1536 floats]
→ Updates EntityEmbedding:
    - embedding: [0.0234, -0.0567, ...]
    - embeddingModel: "text-embedding-3-small"
    - dimensions: 1536
    - status: Generated
→ Saves to Cosmos DB
→ Logs: "Generated 1 embeddings, used 512 tokens"
```

**Total Time**: Profile created at 10:30 AM → Ready at 2:30 AM next day ≈ **16 hours**

**Best Case**: If profile created at 2:00 AM → Ready at 2:30 AM ≈ **30 minutes**

---

### Batch Processing Example

**100 Profiles Created**:
```
Nightly Run (2 AM):
  Batch 1 (10 profiles): Generate summaries → 10 ProfileEmbeddings (Pending)
  Batch 2 (10 profiles): Generate summaries → 10 ProfileEmbeddings (Pending)
  ...
  Batch 10 (10 profiles): Generate summaries → 10 ProfileEmbeddings (Pending)
  Total: 100 Pending embeddings

First 30-Min Run (2:30 AM):
  Query: Get 50 Pending embeddings
  OpenAI API: 1 batch call with 50 summaries
  Result: 50 Generated embeddings
  Remaining: 50 Pending

Second 30-Min Run (3:00 AM):
  Query: Get 50 Pending embeddings
  OpenAI API: 1 batch call with 50 summaries
  Result: 50 Generated embeddings
  Remaining: 0 Pending

Total API Calls: 2 (instead of 100 if done individually)
Cost Savings: 50x reduction in API overhead
```

---

## Error Handling & Retry Logic

### Multi-Layer Retry Strategy

#### Layer 1: OpenAIEmbeddingService (Immediate Retries)
```
Attempt 1: FAIL (network timeout)
  ↓ Wait 2 seconds
Attempt 2: FAIL (rate limit 429)
  ↓ Wait 4 seconds
Attempt 3: SUCCESS
  → Return embeddings
```

**Max Attempts**: 3
**Backoff**: Exponential (2^attempt seconds)
**Handles**: Network errors, rate limits, transient API errors

---

#### Layer 2: ProcessPendingEmbeddingsFunction (Cross-Run Retries)
```
Run 1 (2:30 AM):
  - Embedding A: SUCCESS → Generated
  - Embedding B: FAIL (OpenAI API down)
    → retryCount = 1
    → status = Pending
    → errorMessage = "OpenAI API returned 500"

Run 2 (3:00 AM):
  - Embedding B: FAIL again
    → retryCount = 2
    → status = Pending

Run 3 (3:30 AM):
  - Embedding B: SUCCESS
    → status = Generated
    → retryCount = 0 (reset on success)
```

**Max Attempts**: 5 (configurable via `EMBEDDING_MAX_RETRIES`)
**Interval**: 30 minutes (timer frequency)
**Final Action**: After 5 failures → status = Failed, stop retrying

---

### Failure Scenarios & Handling

| Scenario | Layer 1 (Service) | Layer 2 (Function) | Final State |
|----------|-------------------|-------------------|-------------|
| Network timeout | Retry 3x, then fail | Retry on next run | Eventually Generated or Failed |
| Rate limit (429) | Wait + retry 3x | If still failing, retry on next run | Eventually Generated |
| Invalid API key | Fail immediately | Mark as Failed after 5 runs | Failed |
| Malformed summary | Fail immediately | Mark as Failed after 5 runs | Failed |
| OpenAI quota exceeded | Fail on all attempts | Keep retrying until quota resets | Eventually Generated |
| Empty summary | Logged warning | Skip (not added to batch) | Pending |

---

### Monitoring Failed Embeddings

**Query Cosmos DB**:
```sql
SELECT * FROM c
WHERE c.status = 2  -- Failed
```

**Admin API**:
```
GET /api/super/embeddings?status=Failed
```

**Application Insights Query**:
```kusto
traces
| where customDimensions.Status == "Failed"
| where timestamp > ago(7d)
| summarize count() by tostring(customDimensions.ErrorMessage)
```

---

## Performance & Optimization

### Cost Optimization

#### Without Batch Processing:
```
1000 profiles × 1 API call each = 1000 API calls
Overhead per call: ~100ms
Total overhead: 100 seconds
```

#### With Batch Processing (50 per batch):
```
1000 profiles ÷ 50 per batch = 20 API calls
Overhead per call: ~100ms
Total overhead: 2 seconds

Cost Savings: 50x reduction in overhead
```

---

### RU (Request Unit) Optimization

#### Nightly Summary Generation:
```
Phase 1: Query all profiles (minimal projection)
  Query: SELECT c.id, c.lastModified FROM c
  Cost: ~3 RU per profile
  1000 profiles: 3000 RU

Phase 2: Load full profiles (only those needing processing)
  Cost: ~5 RU per profile
  Assume 10% need updates: 100 profiles × 5 RU = 500 RU

Phase 3: Upsert EntityEmbedding documents
  Cost: ~10 RU per document
  100 documents × 10 RU = 1000 RU

Total: ~4500 RU per night (serverless: ~$0.45)
```

#### Embedding Processing (every 30 min):
```
Phase 1: Query pending embeddings
  Query: SELECT * FROM c WHERE c.status = 0 LIMIT 50
  Cost: ~100 RU per query

Phase 2: Upsert updated embeddings
  Cost: ~15 RU per document (larger due to embedding array)
  50 documents × 15 RU = 750 RU

Total: ~850 RU per run × 48 runs/day = 40,800 RU/day (serverless: ~$4.08)
```

---

### Token Optimization

**Summary Length**:
- Average summary: 300-500 words
- Tokens: ~400-650 tokens per profile
- OpenAI cost (small model): ~$0.00001 per profile
- 1000 profiles: ~$0.01

**Optimization Strategies**:
1. **Summary Length Limits**: Cap summaries at 1000 words to control costs
2. **Hash-Based Skipping**: Skip unchanged profiles (saves 90%+ on updates)
3. **Batch Processing**: Amortize API overhead across 50 profiles

---

## Monitoring & Observability

### Application Insights Queries

#### Processing Statistics:
```kusto
traces
| where message contains "Pending embeddings processing completed"
| extend stats = extract("Total Pending=(\\d+), Generated=(\\d+), Failed=(\\d+)", 0, message)
| project timestamp, stats
| order by timestamp desc
```

#### Failure Analysis:
```kusto
traces
| where severityLevel >= 3  // Warning or Error
| where message contains "embedding"
| summarize count() by message
| order by count_ desc
```

#### Token Usage:
```kusto
customMetrics
| where name == "OpenAI.TokensUsed"
| summarize sum(value) by bin(timestamp, 1d)
```

#### Cost Tracking:
```kusto
let costPerToken = 0.00000002;  // $0.02 per 1M tokens
customMetrics
| where name == "OpenAI.TokensUsed"
| summarize totalTokens = sum(value)
| extend estimatedCost = totalTokens * costPerToken
```

---

### Key Metrics to Monitor

| Metric | Query | Alert Threshold |
|--------|-------|----------------|
| Pending Backlog | `SELECT COUNT(*) FROM c WHERE c.status = 0` | > 500 |
| Failed Embeddings | `SELECT COUNT(*) FROM c WHERE c.status = 2` | > 10 |
| Processing Time | Application Insights duration | > 5 minutes |
| Token Usage | Application Insights custom metric | > 1M tokens/day |
| API Errors | Application Insights failed requests | > 5% error rate |

---

### Health Checks

**Embedding System Health**:
```
GET /api/super/embeddings/health

Response:
{
  "status": "healthy",
  "totalProfiles": 1000,
  "totalEmbeddings": 995,
  "pendingCount": 5,
  "generatedCount": 987,
  "failedCount": 3,
  "coverage": 99.5,  // % of profiles with embeddings
  "lastProcessingRun": "2025-01-15T14:30:00Z",
  "nextScheduledRun": "2025-01-15T15:00:00Z"
}
```

---

## Summary

### Key Takeaways

1. **Automatic Pipeline**: Profiles → Summaries (nightly) → Embeddings (every 30 min)
2. **Two-Stage Processing**: Text generation separate from vector generation
3. **Batch Efficiency**: 50x reduction in API calls through batching
4. **Robust Retry**: Multi-layer retry strategy (3 immediate + 5 cross-run)
5. **Cost Optimized**: Hash-based change detection, serverless Cosmos DB
6. **Dual Model Support**: Small (cheap) and Large (accurate) models

### Processing Guarantees

- **Eventually Consistent**: All profiles will eventually have embeddings
- **Max Latency**: 24 hours from profile creation to embedding generation
- **Typical Latency**: 30 minutes to 2 hours
- **Failure Handling**: Up to 5 automatic retry attempts before manual intervention needed

### Scalability

- **Batch Size**: 50 embeddings per run (configurable)
- **Frequency**: Every 30 minutes = 48 runs/day
- **Max Throughput**: 50 × 48 = 2,400 embeddings/day
- **To Scale**: Reduce timer interval (e.g., every 15 min) or increase batch size (e.g., 100)

---

## Next Steps

For implementation details and API usage, see [EMBEDDING_USER_GUIDE.md](./EMBEDDING_USER_GUIDE.md).
