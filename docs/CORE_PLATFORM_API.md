# Core Platform API Documentation

Complete REST API reference for the EntityMatchingAPI (Core Platform) - the universal entity matching platform.

## Base URL

```
Production: https://EntityMatching-apim.azure-api.net/api/v1
```

## Authentication

All requests require an API subscription key:

**Header:**
```
Ocp-Apim-Subscription-Key: YOUR_API_KEY_HERE
```

**Query Parameter (alternative):**
```
?subscription-key=YOUR_API_KEY_HERE
```

---

## Table of Contents

1. [Profile Management](#profile-management)
2. [Conversational Profiling](#conversational-profiling)
3. [Embedding Management](#embedding-management)
4. [Search & Matching](#search--matching)
5. [Mutual Matching](#mutual-matching)
6. [Match Requests](#match-requests)
7. [Reputation & Ratings](#reputation--ratings)
8. [Microservices](#microservices)
9. [Admin Endpoints](#admin-endpoints)
10. [Response Codes](#response-codes)
11. [Data Models](#data-models)

---

## Profile Management

### Create Profile

```http
POST /api/v1/entities
Content-Type: application/json

{
  "ownedByUserId": "user-123",
  "name": "John Doe",
  "entityType": 0,
  "description": "Software engineer passionate about AI",
  "externalId": "MLS-12345",
  "externalSource": "MLS",
  "isSearchable": true,
  "attributes": {
    "skills": ["Python", "AWS"],
    "yearsExperience": 5
  }
}
```

**Response:** `201 Created`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownedByUserId": "user-123",
  "name": "John Doe",
  "entityType": 0,
  "externalId": "MLS-12345",
  "externalSource": "MLS",
  "isSearchable": true,
  "createdAt": "2026-01-15T00:00:00Z",
  "lastModified": "2026-01-15T00:00:00Z"
}
```

---

### List Profiles

```http
GET /api/v1/entities?userId=user-123
```

**Response:** `200 OK`
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "ownedByUserId": "user-123",
    "name": "John Doe",
    "entityType": 0,
    "isSearchable": true
  }
]
```

---

### Get Profile

```http
GET /api/v1/entities/{profileId}
```

**Response:** `200 OK`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "ownedByUserId": "user-123",
  "name": "John Doe",
  "entityType": 0,
  "description": "Software engineer passionate about AI",
  "externalId": "MLS-12345",
  "externalSource": "MLS",
  "isSearchable": true,
  "attributes": {
    "skills": ["Python", "AWS"],
    "yearsExperience": 5
  },
  "createdAt": "2026-01-15T00:00:00Z",
  "lastModified": "2026-01-15T00:00:00Z"
}
```

---

### Update Profile

```http
PUT /api/v1/entities/{profileId}
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "description": "Updated description",
  "attributes": {
    "skills": ["Python", "AWS", "Machine Learning"]
  }
}
```

**Response:** `200 OK`

---

### Delete Profile

```http
DELETE /api/v1/entities/{profileId}
```

**Response:** `204 No Content`

---

### Get Profile Metadata

```http
GET /api/v1/entities/{profileId}/metadata
```

**Response:** `200 OK`
```json
{
  "profileId": "550e8400-e29b-41d4-a716-446655440000",
  "metadata": {
    "custom_field": "value"
  }
}
```

---

### Update Profile Metadata

```http
PUT /api/v1/entities/{profileId}/metadata
Content-Type: application/json

{
  "metadata": {
    "custom_field": "new_value"
  }
}
```

**Response:** `200 OK`

---

## Conversational Profiling

### Send Message

```http
POST /api/v1/entities/{profileId}/conversation
Content-Type: application/json

{
  "userId": "user-123",
  "message": "I love hiking and rock climbing. I go out almost every weekend!"
}
```

**Response:** `200 OK`
```json
{
  "message": "That's wonderful! Outdoor adventures are great. Do you prefer hiking in mountains or forests?",
  "insights": [
    {
      "category": "Hobbies",
      "content": "Enjoys hiking and rock climbing",
      "confidence": 0.95,
      "extractedAt": "2026-01-15T12:00:00Z"
    }
  ]
}
```

---

### Get Conversation

```http
GET /api/v1/entities/{profileId}/conversation
```

**Response:** `200 OK`
```json
{
  "profileId": "550e8400-e29b-41d4-a716-446655440000",
  "messages": [
    {
      "role": "user",
      "content": "I love hiking...",
      "timestamp": "2026-01-15T12:00:00Z"
    },
    {
      "role": "assistant",
      "content": "That's wonderful!...",
      "timestamp": "2026-01-15T12:00:01Z"
    }
  ],
  "insights": [
    {
      "category": "Hobbies",
      "content": "Enjoys hiking and rock climbing",
      "confidence": 0.95
    }
  ]
}
```

---

### Clear Conversation

```http
DELETE /api/v1/entities/{profileId}/conversation
```

**Response:** `204 No Content`

---

## Embedding Management

### Upload Embedding (Privacy-First)

Upload a pre-computed embedding vector. The client generates the embedding locally and uploads only the vector - no PII sent to server.

```http
POST /api/v1/entities/{profileId}/embeddings/upload
Content-Type: application/json

{
  "embedding": [0.123, -0.456, 0.789, ...],
  "embeddingModel": "text-embedding-3-small",
  "metadata": {
    "generatedAt": "2026-01-15T12:00:00Z",
    "clientVersion": "1.0.0"
  }
}
```

**Notes:**
- Embedding must be exactly 1536 dimensions
- Supported models: `text-embedding-3-small`, `text-embedding-3-large`

**Response:** `200 OK`
```json
{
  "profileId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Generated",
  "dimensions": 1536,
  "embeddingModel": "text-embedding-3-small",
  "generatedAt": "2026-01-15T12:00:00Z",
  "message": "Embedding uploaded successfully"
}
```

---

## Search & Matching

### Semantic Search

```http
POST /api/v1/entities/search
Content-Type: application/json

{
  "query": "Senior Python engineer with AWS experience and ML background",
  "minSimilarity": 0.7,
  "limit": 10,
  "enforcePrivacy": true,
  "requestingUserId": "user-123",
  "includeProfiles": false
}
```

**Response:** `200 OK`
```json
{
  "matches": [
    {
      "profileId": "550e8400-e29b-41d4-a716-446655440000",
      "similarity": 0.94
    },
    {
      "profileId": "660e8400-e29b-41d4-a716-446655440000",
      "similarity": 0.89
    }
  ],
  "metadata": {
    "searchDurationMs": 125,
    "totalCandidates": 1000
  }
}
```

---

### Advanced Search with Filters

```http
POST /api/v1/entities/search
Content-Type: application/json

{
  "query": "loves outdoor activities and adventure",
  "attributeFilters": {
    "logicalOperator": "And",
    "filters": [
      {
        "fieldPath": "attributes.riskTolerance",
        "operator": "GreaterThan",
        "value": 6
      },
      {
        "fieldPath": "attributes.skills",
        "operator": "Contains",
        "value": "Hiking"
      }
    ]
  },
  "minSimilarity": 0.6,
  "limit": 20,
  "enforcePrivacy": true
}
```

---

### Get Similar Profiles

```http
GET /api/v1/entities/{profileId}/similar?limit=10&minSimilarity=0.7
```

**Response:** `200 OK`
```json
{
  "matches": [
    {
      "profileId": "660e8400-e29b-41d4-a716-446655440000",
      "similarity": 0.92
    }
  ]
}
```

---

## Mutual Matching

Find entities that match each other bidirectionally (e.g., a job seeker who matches a job AND the job matches the job seeker).

### Find Mutual Matches

```http
POST /api/v1/entities/{entityId}/mutual-matches
Content-Type: application/json

{
  "minSimilarity": 0.8,
  "targetEntityType": 1,
  "limit": 50
}
```

**Parameters:**
- `minSimilarity`: Minimum score threshold (0-1). Both directions must exceed this.
- `targetEntityType`: Optional filter (0=Person, 1=Job, 2=Property, 3=Product, 4=Service, 5=Event)
- `limit`: Maximum results to return

**Response:** `200 OK`
```json
{
  "matches": [
    {
      "entityAId": "person-123",
      "entityBId": "job-456",
      "entityAType": 0,
      "entityBType": 1,
      "aToB_Score": 0.89,
      "bToA_Score": 0.92,
      "mutualScore": 0.905,
      "matchType": "Mutual",
      "detectedAt": "2026-01-15T12:00:00Z"
    }
  ],
  "totalMutualMatches": 1,
  "metadata": {
    "candidatesEvaluated": 47,
    "reverseLookups": 47,
    "searchDurationMs": 3420,
    "minSimilarity": 0.8
  }
}
```

---

## Match Requests

Manage connection requests between profiles (e.g., dating requests, job applications, property inquiries).

### Create Match Request

```http
POST /api/v1/matches
Content-Type: application/json

{
  "targetId": "profile-456",
  "requesterId": "profile-123",
  "message": "Hi! I think we'd be a great match based on our shared interests"
}
```

**Response:** `201 Created`
```json
{
  "id": "match-789",
  "targetId": "profile-456",
  "requesterId": "profile-123",
  "status": "Pending",
  "message": "Hi! I think we'd be a great match...",
  "createdAt": "2026-01-15T12:00:00Z",
  "lastStatusChangeAt": "2026-01-15T12:00:00Z"
}
```

---

### Get Match Request

```http
GET /api/v1/matches/{matchId}
```

**Response:** `200 OK`

---

### Update Match Status

```http
PATCH /api/v1/matches/{matchId}/status
Content-Type: application/json

{
  "newStatus": "Interested",
  "responseMessage": "Thanks for reaching out! I'd love to connect."
}
```

**Status Values:**
- `0` = Pending
- `1` = Viewed
- `2` = Interested
- `3` = Declined
- `4` = Connected
- `5` = Expired
- `6` = Withdrawn

**Response:** `200 OK`

---

### Get Incoming Match Requests

```http
GET /api/v1/entities/{profileId}/matches/incoming?includeResolved=false
```

**Response:** `200 OK`
```json
[
  {
    "id": "match-789",
    "requesterId": "profile-123",
    "status": "Pending",
    "message": "Hi! I think we'd be a great match...",
    "createdAt": "2026-01-15T12:00:00Z"
  }
]
```

---

### Get Outgoing Match Requests

```http
GET /api/v1/entities/{profileId}/matches/outgoing?includeResolved=false
```

**Response:** `200 OK`

---

## Reputation & Ratings

### Create Rating

```http
POST /api/v1/ratings
Content-Type: application/json

{
  "profileId": "profile-456",
  "ratedByProfileId": "profile-123",
  "overallRating": 4.5,
  "categoryRatings": {
    "communication": 4.5,
    "trustworthiness": 5.0,
    "compatibility": 4.0
  },
  "review": "Great experience working together!",
  "isPublic": true
}
```

**Response:** `201 Created`
```json
{
  "id": "rating-789",
  "profileId": "profile-456",
  "ratedByProfileId": "profile-123",
  "overallRating": 4.5,
  "categoryRatings": {
    "communication": 4.5,
    "trustworthiness": 5.0,
    "compatibility": 4.0
  },
  "review": "Great experience working together!",
  "isVerified": false,
  "isPublic": true,
  "createdAt": "2026-01-15T12:00:00Z"
}
```

---

### Get Rating

```http
GET /api/v1/ratings/{ratingId}
```

**Response:** `200 OK`

---

### Delete Rating

```http
DELETE /api/v1/ratings/{ratingId}
```

**Response:** `204 No Content`

---

### Get Profile Ratings

```http
GET /api/v1/entities/{profileId}/ratings?includePrivate=false
```

**Response:** `200 OK`
```json
[
  {
    "id": "rating-789",
    "ratedByProfileId": "profile-123",
    "overallRating": 4.5,
    "review": "Great experience!",
    "createdAt": "2026-01-15T12:00:00Z"
  }
]
```

---

### Get Profile Reputation

```http
GET /api/v1/entities/{profileId}/reputation?forceRecalculate=false
```

**Response:** `200 OK`
```json
{
  "profileId": "profile-456",
  "averageRating": 4.6,
  "totalRatings": 15,
  "categoryAverages": {
    "communication": 4.7,
    "trustworthiness": 4.8,
    "compatibility": 4.3
  },
  "lastCalculatedAt": "2026-01-15T12:00:00Z"
}
```

---

### Recalculate Reputation

Force recalculation of reputation scores.

```http
POST /api/v1/entities/{profileId}/reputation/recalculate
```

**Response:** `200 OK`

---

## Microservices

The platform has been refactored into a microservices architecture. Domain-specific enrichment has been extracted to specialized services:

### PropertyService

> **Note:** Property enrichment functionality has been extracted to **PropertyService**.

PropertyService enriches property addresses with comprehensive location intelligence data.

**Base URL:** `https://propertyservice-func.azurewebsites.net/api/v1`

**Features:**
- Geocoding with Google Maps API
- School ratings and nearby schools (GreatSchools)
- Demographics (US Census: income, age, education)
- Crime statistics and safety scores
- Walkability scores (Walk/Transit/Bike)
- Nearby places and amenities
- Grid-based caching (80% cost reduction)
- Batch processing for multiple properties
- Natural language property summaries

**Key Endpoints:**
- `POST /properties/enrich` - Enrich single property
- `POST /properties/enrich/batch` - Batch enrichment
- `GET /properties/{id}` - Retrieve enriched property

**Integration:** PropertyService stores enriched properties in EntityMatchingAPI as `Entity` objects (EntityType=5) for semantic search capabilities.

**Documentation:** [PropertyService API Docs](https://github.com/iunknown21/PropertyService/blob/master/docs/PROPERTY_SERVICE_API.md)

---

### CareerService

> **Note:** Career and major enrichment functionality is provided by **CareerService**.

CareerService enriches careers using O*NET-SOC codes and majors using CIP codes with comprehensive occupational and educational data.

**Base URL:** `https://careerservice-func.azurewebsites.net/api/v1`

**Features:**
- Career enrichment using O*NET Web Services
- Major enrichment using CIP codes
- RIASEC interest scores (Holland Codes)
- Detailed tasks, skills, knowledge, abilities
- Salary data and job growth projections (BLS)
- Education and experience requirements
- Major-to-career pathway recommendations

**Key Endpoints:**
- `POST /careers/enrich?onetCode={code}` - Enrich career by O*NET code
- `POST /majors/enrich?cipCode={code}` - Enrich major by CIP code
- `GET /careers/{onetCode}` - Retrieve enriched career
- `GET /majors/{cipCode}` - Retrieve enriched major

**Integration:** CareerService stores enriched careers (EntityType=7) and majors (EntityType=6) in EntityMatchingAPI for semantic search and matching.

**Documentation:** [CareerService API Docs](https://github.com/iunknown21/CareerService/blob/master/docs/CAREER_SERVICE_API.md)

---

**See also:** [Microservices Architecture Guide](./MICROSERVICES_ARCHITECTURE.md) for complete integration patterns and workflows.

---

## Admin Endpoints

Administrative endpoints for system management.

### Process Pending Embeddings

Manually trigger embedding processing (normally runs every 30 minutes).

```http
POST /api/admin/embeddings/process
```

**Response:** `200 OK`
```json
{
  "processed": 25,
  "succeeded": 24,
  "failed": 1,
  "durationMs": 15000
}
```

---

### Get Embedding Status

```http
GET /api/admin/embeddings/status
```

**Response:** `200 OK`
```json
{
  "pending": 10,
  "generated": 500,
  "failed": 2
}
```

---

### Get Embedding for Profile

```http
GET /api/admin/embeddings/{profileId}
```

**Response:** `200 OK`

---

### Regenerate Embedding

Force regeneration of a profile's embedding.

```http
POST /api/admin/embeddings/{profileId}/regenerate
```

**Response:** `200 OK`
```json
{
  "message": "Embedding queued for regeneration",
  "profileId": "profile-123"
}
```

---

### Retry Failed Embeddings

Reset all failed embeddings to pending status.

```http
POST /api/admin/embeddings/retry-failed
```

**Response:** `200 OK`

---

## Response Codes

| Code | Description |
|------|-------------|
| `200 OK` | Request succeeded |
| `201 Created` | Resource created successfully |
| `204 No Content` | Request succeeded, no content to return |
| `400 Bad Request` | Invalid request format or parameters |
| `401 Unauthorized` | Missing or invalid API key |
| `404 Not Found` | Resource not found |
| `429 Too Many Requests` | Rate limit exceeded |
| `500 Internal Server Error` | Server error |

---

## Data Models

### Entity (Base)

All entities (Person, Job, Property, etc.) inherit from this base model.

```json
{
  "id": "guid",
  "entityType": "number (0-5)",
  "externalId": "string (optional - external system ID like MLS ID)",
  "externalSource": "string (optional - source system name like 'MLS')",
  "name": "string",
  "description": "string",
  "attributes": { "key": "value" },
  "metadata": { "key": "value" },
  "ownedByUserId": "string",
  "isSearchable": "boolean",
  "privacySettings": { "fieldOverrides": {} },
  "createdAt": "datetime",
  "lastModified": "datetime"
}
```

**Entity Types:**
- `0` = Person
- `1` = Job
- `2` = Property
- `3` = Product
- `4` = Service
- `5` = Event

---

### PropertyEntity

Strongly-typed entity for real estate properties.

```json
{
  "id": "guid",
  "entityType": 2,
  "externalId": "MLS-12345678",
  "externalSource": "NTREIS",
  "name": "Beautiful 3BR Home",
  "address": "123 Main St",
  "city": "Dallas",
  "state": "TX",
  "postalCode": "75201",
  "propertyType": "House",
  "bedrooms": 3,
  "bathrooms": 2.5,
  "squareFeet": 2200,
  "lotSize": 8500,
  "yearBuilt": 2015,
  "price": 450000,
  "listingType": "Sale",
  "petsAllowed": true,
  "petDeposit": 500,
  "parkingSpaces": 2,
  "amenities": ["Pool", "Garage", "Central AC"],
  "nearbyFeatures": ["Schools", "Parks", "Shopping"],
  "schoolDistrict": "Dallas ISD",
  "schoolRating": 8.5,
  "furnished": false,
  "hoaFees": 150,
  "propertyTax": 8500,
  "availableDate": "2026-02-01"
}
```

---

### EnrichedProperty

Location intelligence data returned from property enrichment.

```json
{
  "id": "string",
  "address": "string",
  "location": {
    "latitude": "number",
    "longitude": "number",
    "formattedAddress": "string",
    "city": "string",
    "state": "string",
    "zipCode": "string",
    "neighborhood": "string"
  },
  "areaData": {
    "schools": [{ "name": "string", "level": "string", "rating": "number", "distance": "number" }],
    "demographics": { "medianIncome": "number", "medianAge": "number" },
    "crimeStats": { "rating": "string", "vsNationalAverage": "number" },
    "neighborhoodCharacter": { "name": "string", "description": "string", "vibeTags": ["string"] }
  },
  "metrics": {
    "walkScore": "number",
    "transitScore": "number",
    "bikeScore": "number",
    "nearbyPlaces": [{ "name": "string", "type": "string", "distance": "number" }]
  },
  "naturalLanguageSummary": "string",
  "status": "Pending | Enriching | Completed | PartiallyCompleted | Failed",
  "enrichedAt": "datetime"
}
```

---

### MatchRequest

```json
{
  "id": "string",
  "targetId": "string",
  "requesterId": "string",
  "status": "number (0-6)",
  "message": "string (optional)",
  "responseMessage": "string (optional)",
  "createdAt": "datetime",
  "viewedAt": "datetime (optional)",
  "lastStatusChangeAt": "datetime",
  "expiresAt": "datetime (optional)",
  "metadata": { "key": "value" }
}
```

**Match Status:**
- `0` = Pending
- `1` = Viewed
- `2` = Interested
- `3` = Declined
- `4` = Connected
- `5` = Expired
- `6` = Withdrawn

---

### EntityRating

```json
{
  "id": "string",
  "profileId": "string",
  "ratedByProfileId": "string",
  "overallRating": "number",
  "categoryRatings": { "category": "number" },
  "review": "string (optional)",
  "isVerified": "boolean",
  "isPublic": "boolean",
  "createdAt": "datetime",
  "lastModified": "datetime",
  "metadata": { "key": "value" }
}
```

---

### SearchRequest

```json
{
  "query": "string",
  "attributeFilters": {
    "logicalOperator": "And | Or",
    "filters": [
      {
        "fieldPath": "string",
        "operator": "Equals | Contains | GreaterThan | ...",
        "value": "any"
      }
    ],
    "subGroups": [{ "...nested filter groups..." }]
  },
  "minSimilarity": "number (0.0-1.0)",
  "limit": "number",
  "enforcePrivacy": "boolean",
  "requestingUserId": "string",
  "includeProfiles": "boolean"
}
```

---

### Filter Operators

- `Equals` - Exact match
- `NotEquals` - Not equal
- `Contains` - Array/string contains value
- `NotContains` - Array/string doesn't contain value
- `GreaterThan` - Numeric greater than
- `LessThan` - Numeric less than
- `GreaterThanOrEqual` - Numeric >=
- `LessThanOrEqual` - Numeric <=
- `InRange` - Value within range
- `IsTrue` - Boolean is true
- `IsFalse` - Boolean is false
- `Exists` - Field exists
- `NotExists` - Field doesn't exist

---

## Errors

### Error Response Format

```json
{
  "error": {
    "code": "InvalidRequest",
    "message": "The query parameter is required",
    "details": [
      {
        "field": "query",
        "issue": "Field is required"
      }
    ]
  },
  "timestamp": "2026-01-15T12:00:00Z"
}
```

### Common Error Codes

| Code | Description |
|------|-------------|
| `InvalidRequest` | Request format or parameters invalid |
| `UnauthorizedAccess` | API key missing or invalid |
| `ResourceNotFound` | Requested resource doesn't exist |
| `RateLimitExceeded` | Too many requests |
| `InsufficientPermissions` | User lacks required permissions |
| `ValidationError` | Data validation failed |

---

## SDKs

**JavaScript/TypeScript:**
```bash
npm install @EntityMatching/sdk
```
[Documentation](./SDK_JAVASCRIPT.md)

**C#/.NET:**
```bash
dotnet add package EntityMatching.SDK
```
[Documentation](./SDK_CSHARP.md)

---

## Support

- **Documentation**: https://github.com/iunknown21/EntityMatchingAPI/tree/master/docs
- **Issues**: https://github.com/iunknown21/EntityMatchingAPI/issues
