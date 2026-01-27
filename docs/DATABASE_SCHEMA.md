# EntityMatchingAPI - Database Schema Documentation

## Overview

EntityMatchingAPI uses **Azure Cosmos DB** with the SQL API for all data storage. The database is designed for scalability, performance, and domain-agnostic flexibility.

### Database Configuration
- **Database Name**: `EntityMatchingDb` (configurable)
- **API**: Cosmos DB SQL API
- **Consistency Level**: Session (default)
- **Provisioning Model**: Serverless (default) or Provisioned Throughput

### Container Summary

| Container | Partition Key | Primary Use Case | Typical Size |
|-----------|---------------|------------------|--------------|
| `profiles` | `/id` | Store user/entity profiles with preferences | 1-10 KB per document |
| `embeddings` | `/profileId` | Store AI-generated embeddings for vector search | 10-20 KB per document |
| `conversations` | `/profileId` | Store conversation history (multi-doc: 1-N per profile) | Up to 1.5 MB per document |
| `matches` | `/targetId` | Track match requests between profiles | 1-2 KB per document |
| `ratings` | `/profileId` | Store individual ratings given to profiles | 1-2 KB per document |
| `reputations` | `/profileId` | Aggregated reputation scores per profile | 2-5 KB per document |

---

## Container Details

### 1. profiles Container

**Purpose**: Store comprehensive profiles with multi-dimensional preferences

**Partition Key**: `/id`
- Ensures even distribution across physical partitions
- Point reads are highly efficient (1 RU for documents < 1KB)

**Schema**: [`Profile.cs`](EntityMatching.Shared/Models/Profile.cs)

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "Sarah Anderson",
  "bio": "Outdoor enthusiast who loves hiking and photography",
  "birthday": "1990-05-15T00:00:00Z",
  "contactInformation": "Seattle, WA",
  "profileImages": [
    {
      "imageUrl": "https://storage.example.com/profile123.jpg",
      "isDefault": true,
      "uploadedAt": "2025-01-01T10:00:00Z"
    }
  ],
  "importantDates": [
    {
      "name": "Anniversary",
      "date": "2020-07-20T00:00:00Z",
      "importance": "High"
    }
  ],
  "entertainmentPreferences": {
    "favoriteMovieGenres": ["Adventure", "Documentary"],
    "favoriteMusicGenres": ["Indie", "Folk"],
    "preferredVenueTypes": ["Intimate", "Outdoor"]
  },
  "adventurePreferences": {
    "activityLevel": 8,
    "riskTolerance": 7,
    "noveltyPreference": 9,
    "enjoysSpontaneity": true
  },
  "sensoryPreferences": {
    "noiseToleranceLevel": 3,
    "lightingPreference": "Natural",
    "crowdSensitivity": 2
  },
  "dietaryRestrictions": {
    "allergies": ["peanuts"],
    "restrictions": ["vegetarian"]
  },
  "accessibilityNeeds": {
    "requiresWheelchairAccess": false,
    "hasLimitedMobility": false
  },
  "privacySettings": {
    "profileVisibility": "Public",
    "searchable": true
  },
  "isSearchable": true,
  "sensitiveDataConsent": true,
  "ownedByUserId": "user-123",
  "createdAt": "2025-01-01T10:00:00Z",
  "lastModified": "2025-01-04T15:30:00Z"
}
```

**Key Features**:
- **11+ Preference Categories**: Entertainment, Style, Nature, Social, Sensory, Adventure, Learning, Gift, Accessibility, Dietary, Activity
- **Privacy Controls**: Field-level visibility settings, searchability toggles
- **Ownership Model**: Supports shared profiles (invite system via `invites` collection)
- **Domain-Agnostic**: Works for dating, jobs, travel, retail, etc.

**Common Queries**:

```sql
-- Get single profile by ID (most efficient - point read)
SELECT * FROM c WHERE c.id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

-- Get all profiles owned by a user
SELECT * FROM c WHERE c.ownedByUserId = "user-123"

-- Get searchable profiles only
SELECT * FROM c WHERE c.isSearchable = true

-- Get profiles modified after a certain date (for embedding sync)
SELECT c.id, c.lastModified FROM c
WHERE c.lastModified > "2025-01-01T00:00:00Z"
```

**Index Recommendations**:
```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    { "path": "/*" }
  ],
  "excludedPaths": [
    { "path": "/profileImages/*" },
    { "path": "/entertainmentPreferences/*" },
    { "path": "/adventurePreferences/*" }
  ]
}
```

**Performance Tips**:
- Use `SELECT c.id, c.lastModified` for lightweight queries (reduces RU cost)
- Exclude large nested objects from indexes if not queried directly
- Point reads (by `id`) are ~1 RU for small documents

---

### 2. embeddings Container

**Purpose**: Store AI-generated text embeddings for semantic vector search

**Partition Key**: `/profileId`
- Efficient lookup of embeddings by profile
- Natural distribution (one embedding per profile)

**Schema**: [`EntityEmbedding.cs`](EntityMatching.Core/Models/Embedding/EntityEmbedding.cs)

```json
{
  "id": "embedding_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "profileId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "profileSummary": "Sarah is an outdoor enthusiast who loves hiking and photography. ENTERTAINMENT: Favorite movie genres include Adventure and Documentary. Enjoys Indie and Folk music. ADVENTURE: High activity level (8/10) with strong risk tolerance (7/10)...",
  "summaryHash": "a1b2c3d4e5f67890abcdef1234567890abcdef==",
  "embedding": [0.0234, -0.0567, 0.0891, ..., 0.0123],
  "embeddingModel": "text-embedding-3-small",
  "dimensions": 1536,
  "status": 1,
  "generatedAt": "2025-01-04T02:30:00Z",
  "profileLastModified": "2025-01-04T15:30:00Z",
  "retryCount": 0,
  "errorMessage": null,
  "summaryMetadata": {
    "hasConversationData": true,
    "conversationChunksCount": 12,
    "extractedInsightsCount": 8,
    "preferenceCategories": ["entertainment", "adventure", "sensory"],
    "hasPersonalityData": true,
    "summaryWordCount": 342
  }
}
```

**Status Values**:
- `0` = **Pending**: Summary generated, waiting for embedding
- `1` = **Generated**: Embedding successfully created
- `2` = **Failed**: Failed after max retries

**Common Queries**:

```sql
-- Get embedding for a specific profile (point read)
SELECT * FROM c WHERE c.profileId = "profile-123"

-- Get all pending embeddings (for scheduled processing)
SELECT * FROM c WHERE c.status = 0 LIMIT 50

-- Get failed embeddings for investigation
SELECT * FROM c WHERE c.status = 2

-- Count embeddings by status
SELECT c.status, COUNT(1) as count FROM c GROUP BY c.status

-- Find profiles that need regeneration (profile modified after embedding)
SELECT * FROM c WHERE c.profileLastModified < @profileModifiedDate
```

**Index Recommendations**:
```json
{
  "indexingMode": "consistent",
  "includedPaths": [
    { "path": "/profileId/?" },
    { "path": "/status/?" },
    { "path": "/profileLastModified/?" }
  ],
  "excludedPaths": [
    { "path": "/embedding/*" },
    { "path": "/profileSummary/?" }
  ],
  "compositeIndexes": [
    [
      { "path": "/status", "order": "ascending" },
      { "path": "/generatedAt", "order": "descending" }
    ]
  ]
}
```

**Performance Tips**:
- **Exclude embedding vector from index**: The `embedding` array (1536 floats) is large and should never be indexed
- **Use composite index**: For queries like "get pending embeddings sorted by date"
- **Batch processing**: Query 50 pending embeddings at a time to optimize RU usage

**RU Cost Estimates**:
- Point read (by `profileId`): ~1 RU
- Query 50 pending: ~100 RU
- Upsert with 1536-dimension vector: ~15 RU

---

### 3. conversations Container

**Purpose**: Store conversation history and AI-extracted insights for profiles using multi-document architecture to avoid 2MB Cosmos DB limit

**Partition Key**: `/profileId`
- Multiple conversation documents per profile (automatically split when approaching 1.5MB)
- One metadata document per profile for efficient tracking
- Efficient retrieval when building profile summaries

**Architecture**: Multi-Document Design
- **ConversationDocument**: Individual conversation segments (1-N per profile)
- **ConversationMetadata**: Tracks active document and statistics (1 per profile)
- **Automatic Splitting**: New documents created at 1.5MB threshold (75% of 2MB limit)

**Schemas**:
- [`ConversationDocument.cs`](EntityMatching.Core/Models/Conversation/ConversationDocument.cs)
- [`ConversationMetadata.cs`](EntityMatching.Core/Models/Conversation/ConversationMetadata.cs)
- [`ConversationContext.cs`](EntityMatching.Core/Models/Conversation/ConversationContext.cs) (aggregated view for API responses)

**ConversationDocument Example**:
```json
{
  "id": "8f3a4b2c-9d1e-4f5a-b6c7-8d9e0f1a2b3c",
  "profileId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userId": "user-123",
  "sequenceNumber": 0,
  "isActive": true,
  "conversationChunks": [
    {
      "text": "I love hiking on weekends, especially mountain trails",
      "timestamp": "2025-01-01T10:15:00Z",
      "speaker": "user"
    },
    {
      "text": "That's great! Do you prefer day hikes or multi-day backpacking trips?",
      "timestamp": "2025-01-01T10:15:30Z",
      "speaker": "ai"
    }
  ],
  "extractedInsights": [
    {
      "category": "hobby",
      "insight": "enjoys hiking, especially mountain trails",
      "confidence": 0.95,
      "sourceChunk": "I love hiking on weekends...",
      "extractedAt": "2025-01-01T10:16:05Z"
    }
  ],
  "createdAt": "2025-01-01T10:00:00Z",
  "lastUpdated": "2025-01-01T10:16:10Z",
  "estimatedSizeBytes": 1245678,
  "chunkCount": 2,
  "insightCount": 1
}
```

**ConversationMetadata Example**:
```json
{
  "id": "convmeta_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "profileId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userId": "user-123",
  "activeDocumentId": "8f3a4b2c-9d1e-4f5a-b6c7-8d9e0f1a2b3c",
  "activeSequenceNumber": 0,
  "totalDocuments": 1,
  "totalChunks": 2,
  "totalInsights": 1,
  "createdAt": "2025-01-01T10:00:00Z",
  "lastUpdated": "2025-01-01T10:16:10Z"
}
```

**Key Features**:
- **Multi-Document Architecture**: Unlimited conversation history by splitting across multiple documents
- **Automatic Sharding**: Documents split at 1.5MB threshold (75% of 2MB Cosmos DB limit)
- **Metadata Tracking**: Fast access to active document without querying all documents
- **Conversation Chunks**: Timestamped messages with speaker attribution
- **Extracted Insights**: AI-identified preferences, hobbies, restrictions
- **Confidence Scores**: Track reliability of extracted insights (0.0-1.0)
- **Source Tracking**: Link insights back to original conversation text

**Common Queries**:

```sql
-- Get conversation metadata for a profile (point read - fastest)
SELECT * FROM c WHERE c.id = "convmeta_profile-123"

-- Get all conversation documents for a profile (aggregated by service)
SELECT * FROM c
WHERE c.profileId = "profile-123" AND c.id != "convmeta_profile-123"
ORDER BY c.sequenceNumber ASC

-- Get active conversation document only
SELECT * FROM c
WHERE c.profileId = "profile-123" AND c.isActive = true

-- Get all conversations for a user (cross-partition)
SELECT * FROM c WHERE c.userId = "user-123"

-- Get conversations with recent activity
SELECT * FROM c WHERE c.lastUpdated > "2025-01-01T00:00:00Z"
```

**Index Recommendations**:
```json
{
  "indexingMode": "consistent",
  "includedPaths": [
    { "path": "/profileId/?" },
    { "path": "/userId/?" },
    { "path": "/sequenceNumber/?" },
    { "path": "/isActive/?" },
    { "path": "/lastUpdated/?" }
  ],
  "excludedPaths": [
    { "path": "/conversationChunks/*" },
    { "path": "/extractedInsights/*/sourceChunk/?" }
  ],
  "compositeIndexes": [
    [
      { "path": "/profileId", "order": "ascending" },
      { "path": "/sequenceNumber", "order": "ascending" }
    ]
  ]
}
```

**Performance Tips**:
- **Metadata First**: Query metadata (1 RU) to get active document ID, then point-read active document
- **Efficient Aggregation**: Service layer handles aggregating multiple documents transparently
- **Automatic Splitting**: Documents automatically split at 1.5MB (max 500 chunks per document as fallback)
- **Scalability**: Supports 100-1000+ conversation documents per profile
- **Query Optimization**: Use `sequenceNumber` for efficient ordering when retrieving all documents

**RU Cost Estimates**:
- Point read metadata: 1 RU
- Get all documents for profile (10 docs): 10-15 RU
- Add new message: 17-22 RU (metadata + active document read/write)
- Delete all conversations: 5-10 RU × number of documents

---

### 4. matches Container

**Purpose**: Track match/connection requests between profiles

**Partition Key**: `/targetId`
- Optimized for "incoming requests" queries
- Efficient lookup of all match requests for a profile

**Schema**: [`MatchRequest.cs`](EntityMatching.Core/Models/Matching/MatchRequest.cs)

```json
{
  "id": "match-a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "targetId": "profile-456",
  "requesterId": "profile-123",
  "status": 1,
  "message": "Hi! I think we'd be a great match based on our shared interests in hiking",
  "responseMessage": null,
  "createdAt": "2025-01-03T14:20:00Z",
  "viewedAt": "2025-01-03T16:45:00Z",
  "lastStatusChangeAt": "2025-01-03T16:45:00Z",
  "expiresAt": "2025-02-03T14:20:00Z",
  "metadata": {
    "source": "search_results",
    "similarity_score": 0.87,
    "context": "shared_hiking_interest"
  }
}
```

**Status Values**:
- `0` = **Pending**: Request sent, awaiting response
- `1` = **Viewed**: Target viewed the request
- `2` = **Interested**: Target indicated interest
- `3` = **Declined**: Target declined
- `4` = **Connected**: Mutual match established
- `5` = **Expired**: Timed out
- `6` = **Withdrawn**: Requester withdrew

**Status Lifecycle**:
```
Pending → Viewed → Interested → Connected
   ↓         ↓          ↓
Withdrawn  Declined   Declined
   ↓         ↓          ↓
Expired   Expired    Expired
```

**Common Queries**:

```sql
-- Get all incoming requests for a profile (MOST COMMON - partition key query)
SELECT * FROM c WHERE c.targetId = "profile-456"

-- Get pending incoming requests
SELECT * FROM c
WHERE c.targetId = "profile-456" AND c.status = 0
ORDER BY c.createdAt DESC

-- Get all outgoing requests from a profile (cross-partition query)
SELECT * FROM c WHERE c.requesterId = "profile-123"

-- Get mutual matches (connected status)
SELECT * FROM c WHERE c.status = 4

-- Find expired requests that need status update
SELECT * FROM c
WHERE c.expiresAt < @currentDate AND c.status < 3
```

**Index Recommendations**:
```json
{
  "indexingMode": "consistent",
  "includedPaths": [
    { "path": "/targetId/?" },
    { "path": "/requesterId/?" },
    { "path": "/status/?" },
    { "path": "/createdAt/?" },
    { "path": "/expiresAt/?" }
  ],
  "compositeIndexes": [
    [
      { "path": "/targetId", "order": "ascending" },
      { "path": "/status", "order": "ascending" },
      { "path": "/createdAt", "order": "descending" }
    ]
  ]
}
```

**Performance Tips**:
- **Partition key is critical**: Querying by `targetId` is most efficient (3-5 RU)
- **Cross-partition queries are expensive**: Querying by `requesterId` scans all partitions (~10-50 RU depending on data size)
- **Use composite index**: For "get pending requests sorted by date"
- **Implement pagination**: Use continuation tokens for large result sets

**RU Cost Estimates**:
- Get incoming requests (by `targetId`): 3-5 RU
- Get outgoing requests (by `requesterId`): 10-50 RU (cross-partition)
- Create match request: 5 RU
- Update status: 5-10 RU

---

### 5. ratings Container

**Purpose**: Store individual ratings given by one profile to another

**Partition Key**: `/profileId`
- Optimized for "get all ratings for a profile" queries
- Natural distribution (many raters per profile)

**Schema**: [`EntityRating.cs`](EntityMatching.Core/Models/Reputation/EntityRating.cs)

```json
{
  "id": "rating-a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "profileId": "profile-456",
  "ratedByProfileId": "profile-123",
  "overallRating": 4.5,
  "categoryRatings": {
    "communication": 5.0,
    "trustworthiness": 4.5,
    "compatibility": 4.0
  },
  "review": "Great experience! Very professional and friendly.",
  "isVerified": true,
  "isPublic": true,
  "createdAt": "2025-01-03T18:30:00Z",
  "lastModified": "2025-01-03T18:30:00Z",
  "metadata": {
    "interaction_date": "2025-01-02",
    "context": "first_meeting",
    "verified_via": "mutual_match"
  }
}
```

**Key Features**:
- **Overall Rating**: Single numeric score (application-defined scale, e.g., 1-5 stars)
- **Category Ratings**: Optional breakdown by attribute (communication, skills, quality, etc.)
- **Verified Ratings**: Higher weight if both profiles matched/connected
- **Public/Private**: Control whether rating is visible to others

**Common Queries**:

```sql
-- Get all ratings for a profile (partition key query)
SELECT * FROM c WHERE c.profileId = "profile-456"

-- Get verified ratings only
SELECT * FROM c
WHERE c.profileId = "profile-456" AND c.isVerified = true

-- Get public ratings for display
SELECT * FROM c
WHERE c.profileId = "profile-456" AND c.isPublic = true
ORDER BY c.createdAt DESC

-- Get ratings given by a specific profile (cross-partition)
SELECT * FROM c WHERE c.ratedByProfileId = "profile-123"

-- Calculate average rating (aggregation)
SELECT AVG(c.overallRating) as avgRating FROM c
WHERE c.profileId = "profile-456"
```

**Index Recommendations**:
```json
{
  "indexingMode": "consistent",
  "includedPaths": [
    { "path": "/profileId/?" },
    { "path": "/ratedByProfileId/?" },
    { "path": "/overallRating/?" },
    { "path": "/isVerified/?" },
    { "path": "/isPublic/?" },
    { "path": "/createdAt/?" }
  ],
  "compositeIndexes": [
    [
      { "path": "/profileId", "order": "ascending" },
      { "path": "/isPublic", "order": "ascending" },
      { "path": "/createdAt", "order": "descending" }
    ]
  ]
}
```

**Performance Tips**:
- Use partition key (`profileId`) for efficient queries
- Calculate aggregations (averages) in application code or use Azure Functions
- Consider caching reputation scores in the `reputations` container

---

### 6. reputations Container

**Purpose**: Store aggregated reputation scores calculated from ratings

**Partition Key**: `/profileId`
- One reputation record per profile
- Efficient point reads and updates

**Schema**: [`EntityReputation.cs`](EntityMatching.Core/Models/Reputation/EntityReputation.cs)

```json
{
  "id": "reputation-profile-456",
  "profileId": "profile-456",
  "overallScore": 4.5,
  "totalRatings": 23,
  "verifiedRatings": 15,
  "verifiedScore": 4.7,
  "categoryScores": [
    {
      "category": "communication",
      "score": 4.8,
      "count": 20
    },
    {
      "category": "trustworthiness",
      "score": 4.6,
      "count": 18
    },
    {
      "category": "compatibility",
      "score": 4.3,
      "count": 15
    }
  ],
  "confidenceScore": 0.92,
  "lastCalculated": "2025-01-04T12:00:00Z",
  "metadata": {
    "calculation_method": "weighted_average",
    "min_ratings_threshold": 3
  }
}
```

**Key Features**:
- **Pre-calculated Scores**: Avoid expensive aggregations at query time
- **Confidence Score**: Indicates reliability based on number of ratings
- **Category Breakdown**: Detailed reputation by attribute
- **Verified vs All**: Separate scores for verified vs all ratings

**Common Queries**:

```sql
-- Get reputation for a profile (point read)
SELECT * FROM c WHERE c.profileId = "profile-456"

-- Get high-reputation profiles (cross-partition)
SELECT * FROM c
WHERE c.overallScore >= 4.5 AND c.totalRatings >= 10

-- Get profiles with high confidence
SELECT * FROM c WHERE c.confidenceScore >= 0.8
```

**Index Recommendations**:
```json
{
  "indexingMode": "consistent",
  "includedPaths": [
    { "path": "/profileId/?" },
    { "path": "/overallScore/?" },
    { "path": "/confidenceScore/?" },
    { "path": "/totalRatings/?" }
  ]
}
```

**Performance Tips**:
- **Update via scheduled job**: Recalculate reputations nightly or after new ratings
- **Cache in application**: Reputation changes infrequently, ideal for caching
- **Point reads are fast**: ~1 RU per lookup

**Calculation Formula Example**:
```
overallScore = SUM(rating.overallRating) / totalRatings
confidenceScore = MIN(totalRatings / TARGET_RATINGS, 1.0)
  where TARGET_RATINGS = 25 (configurable)
```

---

## Query Patterns & Best Practices

### Efficient Query Patterns

#### 1. Point Reads (Best Performance)
```sql
-- Get profile by ID (1 RU)
SELECT * FROM c WHERE c.id = "profile-123"

-- Get embedding by profileId (1 RU)
SELECT * FROM c WHERE c.profileId = "profile-123"
```

#### 2. Partition Key Queries (Good Performance)
```sql
-- Get incoming match requests (3-5 RU)
SELECT * FROM c WHERE c.targetId = "profile-456"

-- Get all ratings for a profile (5-10 RU)
SELECT * FROM c WHERE c.profileId = "profile-456"
```

#### 3. Cross-Partition Queries (Higher Cost)
```sql
-- Get all searchable profiles (50-500 RU depending on size)
SELECT * FROM c WHERE c.isSearchable = true

-- Get all pending embeddings (100 RU)
SELECT * FROM c WHERE c.status = 0
```

#### 4. Aggregations (Expensive - Avoid if Possible)
```sql
-- Count by status (pre-calculate and cache instead)
SELECT c.status, COUNT(1) FROM c GROUP BY c.status

-- Calculate average rating (use reputations container instead)
SELECT AVG(c.overallRating) FROM c WHERE c.profileId = "profile-456"
```

### Performance Optimization Strategies

#### 1. Use Minimal Projections
```sql
-- BAD: Fetches entire document
SELECT * FROM c WHERE c.lastModified > @date

-- GOOD: Fetches only needed fields (reduces RU cost 50-80%)
SELECT c.id, c.lastModified FROM c WHERE c.lastModified > @date
```

#### 2. Implement Pagination
```csharp
var query = container.GetItemQueryIterator<Profile>(
    queryDefinition,
    continuationToken: continuationToken,
    requestOptions: new QueryRequestOptions { MaxItemCount = 50 }
);
```

#### 3. Batch Operations
```csharp
// Process embeddings in batches of 50
var batch = await container.GetItemQueryIterator<EntityEmbedding>(
    "SELECT TOP 50 * FROM c WHERE c.status = 0"
).ReadNextAsync();
```

#### 4. Use Composite Indexes for Sorting
```json
// Enable efficient ORDER BY on multiple fields
{
  "compositeIndexes": [
    [
      { "path": "/status", "order": "ascending" },
      { "path": "/createdAt", "order": "descending" }
    ]
  ]
}
```

#### 5. Exclude Large Fields from Indexes
```json
{
  "excludedPaths": [
    { "path": "/embedding/*" },        // Large array
    { "path": "/profileSummary/?" },   // Large text
    { "path": "/conversationChunks/*" } // Large nested array
  ]
}
```

---

## Cost Optimization

### RU (Request Unit) Cost Examples

| Operation | Container | RU Cost | Notes |
|-----------|-----------|---------|-------|
| Point read (by ID) | Any | 1 RU | Most efficient |
| Query by partition key | Any | 3-5 RU | Very efficient |
| Cross-partition query | Any | 10-500 RU | Depends on data size |
| Create profile | profiles | 5-10 RU | ~5 KB document |
| Upsert embedding | embeddings | 15 RU | Large array (1536 floats) |
| Create match request | matches | 5 RU | ~1 KB document |
| Query 50 pending | embeddings | 100 RU | Batch query |
| Aggregation (COUNT) | Any | 50-200 RU | Scans all matching docs |

### Monthly Cost Estimates (Serverless)

**Serverless Pricing**: $0.25 per million RUs + $0.25 per GB stored

**Example Workload**:
- 1,000 active profiles
- 10,000 profile reads/day
- 100 new profiles/day
- 1,000 match requests/day
- 48 embedding batches/day (every 30 min)

**Estimated RU Usage**:
```
Profile reads:     10,000 × 1 RU = 10,000 RU/day
New profiles:      100 × 10 RU = 1,000 RU/day
Match requests:    1,000 × 5 RU = 5,000 RU/day
Embedding batches: 48 × 850 RU = 40,800 RU/day
------------------------
Total:             56,800 RU/day × 30 = 1.7M RU/month

Cost: 1.7M RU × $0.25 = $0.43/month (RU cost)
Storage: 1.5 GB × $0.25 = $0.38/month
------------------------
Total: ~$0.81/month
```

### Cost Optimization Tips

1. **Use Point Reads**: Query by `id` or partition key whenever possible
2. **Minimize Projections**: Select only needed fields (50-80% RU savings)
3. **Batch Operations**: Process embeddings in batches (50x reduction)
4. **Cache Frequently Read Data**: Use Redis/in-memory cache for profiles
5. **Exclude Large Fields from Indexes**: Embedding arrays, conversation chunks
6. **Pre-calculate Aggregations**: Use reputations container instead of AVG() queries

---

## Scaling Considerations

### When to Scale Up

**Move from Serverless to Provisioned Throughput when**:
- Consistent traffic > 5,000 RU/second
- Monthly RU usage > 72M RU (provisioned becomes cheaper)
- Predictable workload patterns
- Need guaranteed performance SLAs

### Partition Key Design Review

**Current Design**:
- ✅ `profiles`: `/id` - Even distribution, efficient point reads
- ✅ `embeddings`: `/profileId` - Natural distribution (1:1 with profiles)
- ✅ `conversations`: `/profileId` - Natural distribution (1:1 with profiles)
- ✅ `matches`: `/targetId` - Optimized for incoming requests
- ✅ `ratings`: `/profileId` - Natural distribution (many ratings per profile)
- ✅ `reputations`: `/profileId` - Natural distribution (1:1 with profiles)

**Potential Hot Partition Issues**:
- If one profile receives thousands of match requests: Consider sharding by `targetId_timestamp`
- If one profile has thousands of ratings: Monitor partition metrics, may need synthetic partition key

### Geographic Distribution

**Multi-region setup** (for global applications):
1. Enable multi-region writes for low-latency updates
2. Configure consistency level: Session or Bounded Staleness
3. Add read regions near user populations
4. Estimate 2x cost for each additional write region

---

## Backup and Disaster Recovery

### Automatic Backups
- **Continuous backups**: Enabled by default (30-day retention)
- **Point-in-time restore**: Restore to any second within retention window
- **Geo-redundant**: Backups replicated to paired region

### Disaster Recovery Plan
1. **Enable multi-region writes**: Automatic failover
2. **Document critical queries**: For post-recovery validation
3. **Test restore procedures**: Quarterly DR drills
4. **Monitor RU consumption**: Set alerts for unusual spikes

---

## Security Best Practices

### 1. Network Security
- Enable **Firewall rules** to restrict IP access
- Use **Private Endpoints** for VNet-isolated access
- Disable **Public Network Access** if possible

### 2. Authentication
- Use **Managed Identity** for Functions to access Cosmos DB
- Rotate **Connection Strings** every 90 days
- Never store keys in code (use Key Vault)

### 3. Data Encryption
- **Encryption at rest**: Enabled by default (Microsoft-managed keys)
- **Encryption in transit**: TLS 1.2+ enforced
- Consider **Customer-managed keys** for compliance

### 4. Privacy Compliance
- Implement **field-level visibility** (Privacy.FieldVisibilitySettings)
- Support **GDPR right to erasure**: Hard-delete profile + related data
- Log all **data access** for audit trails

---

## Monitoring & Alerts

### Key Metrics to Monitor

| Metric | Threshold | Alert Action |
|--------|-----------|--------------|
| RU consumption | > 10,000 RU/s sustained | Review query patterns, consider provisioned throughput |
| Pending embeddings count | > 500 | Investigate embedding generation failures |
| Failed embeddings count | > 50 | Check OpenAI API status, review error logs |
| Average query latency | > 100ms | Optimize queries, check for hot partitions |
| Storage growth | > 80% of quota | Review data retention policies |
| 429 Rate limit errors | > 1% of requests | Increase provisioned RU/s or implement retry logic |

### Application Insights Queries

```kusto
// RU consumption over time
customMetrics
| where name == "CosmosDb.RequestCharge"
| summarize sum(value) by bin(timestamp, 1h)

// Slow queries (> 100ms)
dependencies
| where type == "Azure DocumentDB"
| where duration > 100
| summarize count() by operation_Name

// 429 Rate limit errors
traces
| where message contains "429"
| summarize count() by bin(timestamp, 5m)
```

---

## Migration and Data Management

### Initial Data Import
1. Use **Azure Data Factory** for bulk imports (> 10,000 records)
2. Use **Cosmos DB Bulk Executor** library for medium imports
3. Use **direct API calls** for small imports (< 1,000 records)

### Schema Evolution
- Cosmos DB is schema-less: Add new fields without migration
- Use **versioning** for breaking changes: Add `schemaVersion` field
- Implement **data migration functions** for gradual schema updates

### Data Retention Policies
- **Profiles**: Retain indefinitely (user-managed deletion)
- **Embeddings**: Delete when profile deleted (cascade delete)
- **Conversations**: Archive after 1 year of inactivity
- **Matches**: Archive declined/expired matches after 6 months
- **Ratings**: Retain indefinitely (immutable after creation)
- **Reputations**: Recalculate when ratings change

---

## Summary

This database schema provides:
- ✅ **Scalability**: Serverless with auto-scaling to provisioned throughput
- ✅ **Performance**: Optimized partition keys and indexing strategies
- ✅ **Flexibility**: Domain-agnostic models support multiple use cases
- ✅ **Cost Efficiency**: Minimal RU usage through query optimization
- ✅ **Privacy**: Field-level visibility and GDPR compliance
- ✅ **Reliability**: Automatic backups and multi-region support

For implementation details, see:
- [EMBEDDING_ARCHITECTURE.md](./EMBEDDING_ARCHITECTURE.md) - Embedding generation pipeline
- [EXECUTIVE_SUMMARY.md](./EXECUTIVE_SUMMARY.md) - System overview and API capabilities
