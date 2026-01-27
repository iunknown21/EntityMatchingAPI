# EntityMatchingAPI

**A domain-agnostic profile and AI matching API** extracted from Date Night Planner to power a zero-trust profile marketplace.

## 🎯 Vision

Enable users to create comprehensive profiles that can be matched across multiple verticals (dating, jobs, travel, retail, etc.) using vector embeddings and AI-powered semantic search - without exposing personal data.

## 🏗️ Architecture

```
┌─────────────────────┐
│  api.bystorm.com    │  ← Custom Domain
└──────────┬──────────┘
           │
           ▼
┌──────────────────────────────────┐
│  Azure API Management (APIM)     │  ← API Gateway
│  • Authentication                │
│  • Rate Limiting                 │
│  • Analytics                     │
│  • Caching                       │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│  Azure Functions (.NET 8)        │  ← API Backend
│  • ProfileFunctions              │
│  • ConversationFunctions         │
└──────────┬───────────────────────┘
           │
           ▼
┌──────────────────────────────────┐
│  Cosmos DB (Serverless)          │  ← Data Storage
│  • profiles                      │
│  • conversations                 │
│  • embeddings                    │
└──────────────────────────────────┘
```

## ✨ Features

### Profile Management
- **Comprehensive Profiling**: 8+ dimensional preference system
  - Entertainment (movies, music, books, games)
  - Adventure (risk tolerance, novelty seeking)
  - Learning (intellectual interests, subjects)
  - Sensory (sound, smell, texture, light, temperature)
  - Social (interaction style, conversation preferences)
  - Style (fashion, aesthetics, ambiance)
  - Nature (animals, weather, seasons)
  - Gift preferences

- **Personality Data**: Love languages, MBTI, Big Five, Enneagram
- **Accessibility**: Comprehensive accommodation tracking
- **Dietary**: Allergies and restrictions with severity levels

### Conversational Profiling
- **AI-Powered**: Natural language conversations to build profiles
- **Insight Extraction**: Automatic extraction of preferences from chat
- **Confidence Scoring**: AI assigns confidence levels to insights (0.0-1.0)
- **Category Classification**: Hobbies, preferences, restrictions, personality

### Vector Embeddings & Intelligent Search
- **Profile Summaries**: Generate comprehensive text summaries
- **Embedding Storage**: Store 1536-dim vectors for semantic search (OpenAI text-embedding-3-small)
- **Change Detection**: Track profile modifications via hash comparison
- **Batch Processing**: Ready for large-scale embedding generation
- **Hybrid Search**: Combine semantic similarity with structured attribute filtering
- **Privacy-First**: Return only profile IDs + similarity scores (no PII exposure)
- **Client-Side Embeddings ⭐ NEW**: Upload pre-computed vectors without sending source text (zero PII storage)

### Attribute-Based Filtering
- **13 Filter Operators**: Equals, NotEquals, Contains, GreaterThan, LessThan, InRange, IsTrue, IsFalse, Exists, NotExists, and more
- **Complex Logic**: AND/OR operators with nested filter groups
- **Field-Level Privacy**: Public, Private, and FriendsOnly visibility settings
- **Privacy Enforcement**: Fail-closed security - private fields not searchable by unauthorized users
- **Matched Attributes**: Results include only the fields that matched (transparency without exposure)

## 🚀 API Endpoints

### Profile Management

```http
GET    /api/v1/entities?userId={userId}
GET    /api/v1/entities/{id}
POST   /api/v1/entities
PUT    /api/v1/entities/{id}
DELETE /api/v1/entities/{id}
```

### Conversational Profiling

```http
POST   /api/v1/entities/{profileId}/conversation
GET    /api/v1/entities/{profileId}/conversation
DELETE /api/v1/entities/{profileId}/conversation
```

### Similarity Search & Attribute Filtering

```http
POST   /api/v1/entities/search                    # Hybrid search with filters
GET    /api/v1/entities/{id}/similar              # Find similar profiles
POST   /api/admin/embeddings/process              # Generate embeddings
GET    /api/admin/embeddings/status               # Check embedding status
```

### Privacy-First Vector Upload ⭐ NEW

```http
POST   /api/v1/entities/{profileId}/embeddings/upload   # Upload client-generated embedding
```

## 🔧 Technology Stack

- **.NET 8.0** (C#, Isolated Worker Model)
- **Azure Functions** (Serverless compute)
- **Azure Cosmos DB** (Serverless NoSQL)
- **Azure API Management** (API Gateway)
- **Groq AI** (`llama-3.3-70b-versatile` for conversational profiling)
- **OpenAI** (`text-embedding-3-small` for vector embeddings - 1536 dimensions)

## 📦 Project Structure

```
EntityMatchingAPI/
├── EntityMatching.Core/          # Domain models & interfaces
│   ├── Models/                    # 30+ model files
│   ├── Interfaces/                # Service interfaces
│   └── Utilities/                 # JsonHelper
│
├── EntityMatching.Infrastructure/ # Service implementations
│   └── Services/                  # 5 core services
│
├── EntityMatching.Functions/     # Azure Functions API
│   ├── ProfileFunctions.cs        # Profile CRUD
│   ├── ConversationFunctions.cs   # Conversation endpoints
│   └── Common/BaseApiFunction.cs  # CORS & base functionality
│
└── EntityMatching.Tests/         # Unit & integration tests
```

## 🏃 Quick Start

### Prerequisites

- .NET 8.0 SDK
- Azure Account
- Groq API Key (for conversational profiling)
- OpenAI API Key (for vector embeddings)

### Local Development

1. **Clone and Build**
   ```bash
   cd EntityMatchingAPI
   dotnet build
   ```

2. **Configure Settings**

   Edit `EntityMatching.Functions/local.settings.json`:
   ```json
   {
     "CosmosDb__ConnectionString": "your-cosmos-connection",
     "CosmosDb__DatabaseId": "EntityMatchingDB",
     "ApiKeys__Groq": "your-groq-api-key",
     "OpenAI__ApiKey": "your-openai-api-key",
     "OpenAI__EmbeddingModel": "text-embedding-3-small",
     "OpenAI__EmbeddingDimensions": "1536"
   }
   ```

3. **Run Locally**
   ```bash
   cd EntityMatching.Functions
   func start
   ```

4. **Test API**
   ```bash
   # List profiles
   curl http://localhost:7071/api/v1/entities?userId=test-user

   # Create profile
   curl -X POST http://localhost:7071/api/v1/entities \
     -H "Content-Type: application/json" \
     -d '{"name":"John Doe","ownedByUserId":"user-123"}'
   ```

## ☁️ Azure Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for complete deployment guide including:
- Azure Functions deployment
- API Management setup
- Custom domain configuration
- Subscription tiers & API keys
- Monitoring & analytics

**Quick Deploy**:
```bash
# Deploy Functions
cd EntityMatching.Functions
dotnet publish -c Release
func azure functionapp publish EntityMatchingapi

# Setup APIM (see DEPLOYMENT.md for full guide)
az apim create --name EntityMatchingapim --resource-group EntityMatchingRG
```

## 📊 API Usage Examples

### Create a Profile

```http
POST /api/v1/entities
Content-Type: application/json
Ocp-Apim-Subscription-Key: YOUR_KEY

{
  "name": "Sarah Johnson",
  "bio": "Adventure seeker and book lover",
  "ownedByUserId": "user-456",
  "entertainmentPreferences": {
    "favoriteMovieGenres": ["Sci-Fi", "Documentary"],
    "favoriteMusicGenres": ["Indie Rock", "Jazz"],
    "favoriteBookGenres": ["Fantasy", "Philosophy"]
  },
  "adventurePreferences": {
    "riskTolerance": 7,
    "noveltyPreference": 8,
    "enjoysSpontaneity": true
  },
  "loveLanguages": {
    "qualityTime": 9,
    "wordsOfAffirmation": 7,
    "actsOfService": 5
  }
}
```

### Start a Conversation

```http
POST /api/v1/entities/{profileId}/conversation
Content-Type: application/json
Ocp-Apim-Subscription-Key: YOUR_KEY

{
  "userId": "user-456",
  "message": "They love hiking on weekends and enjoy trying new restaurants"
}
```

**Response**:
```json
{
  "aiResponse": "That's wonderful! Do they prefer challenging mountain hikes or scenic nature trails? And what types of cuisine do they enjoy most?",
  "newInsights": [
    {
      "category": "hobby",
      "insight": "enjoys hiking on weekends",
      "confidence": 0.95
    },
    {
      "category": "preference",
      "insight": "likes trying new restaurants",
      "confidence": 0.92
    }
  ]
}
```

### Hybrid Search (Semantic + Attribute Filtering)

**Example: Find outdoor enthusiasts with specific attributes**

```http
POST /api/v1/entities/search
Content-Type: application/json
Ocp-Apim-Subscription-Key: YOUR_KEY

{
  "query": "loves hiking and outdoor adventures in nature",
  "attributeFilters": {
    "logicalOperator": "And",
    "filters": [
      {
        "fieldPath": "naturePreferences.hasPets",
        "operator": "IsTrue"
      },
      {
        "fieldPath": "naturePreferences.petTypes",
        "operator": "Contains",
        "value": "Dog"
      },
      {
        "fieldPath": "learningPreferences.subjectsOfInterest",
        "operator": "Contains",
        "value": "English"
      },
      {
        "fieldPath": "preferences.favoriteCuisines",
        "operator": "Contains",
        "value": "American"
      }
    ]
  },
  "requestingUserId": null,
  "enforcePrivacy": true,
  "limit": 10,
  "minSimilarity": 0.5
}
```

**Response (Privacy-Protected)**:
```json
{
  "matches": [
    {
      "profileId": "abc-123",
      "similarityScore": 0.87,
      "matchedAttributes": {
        "naturePreferences.hasPets": true,
        "naturePreferences.petTypes": ["Dog", "Cat"],
        "learningPreferences.subjectsOfInterest": ["English Literature", "History"],
        "preferences.favoriteCuisines": ["American", "Italian"]
      },
      "profileName": "Outdoor Male 1",
      "embeddingDimensions": 1536
    }
  ],
  "totalMatches": 1,
  "metadata": {
    "searchedAt": "2025-12-31T...",
    "totalEmbeddingsSearched": 150,
    "minSimilarity": 0.5,
    "requestedLimit": 10,
    "searchDurationMs": 234
  }
}
```

**Note**: Only profile IDs and matched attributes are returned - never full profiles. Clients fetch full profiles separately via `/api/v1/entities/{id}` to maintain privacy.

### Privacy-First Vector Upload (Client-Side Embedding Generation)

**Example: Upload resume embedding without sending resume text**

```javascript
// Step 1: User provides resume text (stays on their device)
const resumeText = `
Senior Software Engineer with 10 years experience in Python and AWS.
Built machine learning pipelines processing 100M+ events/day.
Expert in distributed systems, microservices, and cloud architecture.
`;

// Step 2: Generate embedding locally using OpenAI SDK
const openai = new OpenAI({ apiKey: userOpenAIKey });
const embeddingResponse = await openai.embeddings.create({
  model: "text-embedding-3-small",
  input: resumeText,
});

// Step 3: Extract the vector (1536 floats)
const vector = embeddingResponse.data[0].embedding;

// Step 4: Upload ONLY the vector (never the text!)
const response = await fetch(
  `https://api.bystorm.com/v1/profiles/${profileId}/embeddings/upload`,
  {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Ocp-Apim-Subscription-Key': 'YOUR_KEY'
    },
    body: JSON.stringify({
      Embedding: vector,  // [0.123, -0.456, ..., 1536 numbers]
      EmbeddingModel: "text-embedding-3-small"
    })
  }
);

// Server response
const result = await response.json();
// {
//   "ProfileId": "profile-123",
//   "Status": "Generated",
//   "Dimensions": 1536,
//   "Message": "Embedding uploaded successfully"
// }

// CRITICAL: Delete the original resume text after upload
// (it's no longer needed - the vector contains the semantic information)
resumeText = null;
```

**Privacy Benefits:**
- ✅ Server **never sees** your resume text
- ✅ Only the vector is stored (meaningless numbers without context)
- ✅ Even if database is breached, attackers get no personal information
- ✅ GDPR compliant - no PII means no data protection requirements
- ✅ User controls when/how embedding is generated

**Use Cases:**
- Job seekers upload resume embeddings without exposing current employer
- Dating profiles without revealing personal details
- Healthcare providers matching patients without storing medical records
- Any scenario requiring semantic matching without PII exposure

### Field-Level Privacy Control

```http
PUT /api/v1/entities/{id}
Content-Type: application/json

{
  "name": "John Doe",
  "birthday": "1990-01-01",
  "isSearchable": true,
  "privacySettings": {
    "defaultVisibility": "Private",
    "fieldVisibilityMap": {
      "name": "Public",
      "bio": "Public",
      "birthday": "Private",
      "contactInformation": "Private",
      "naturePreferences.hasPets": "Public",
      "naturePreferences.petTypes": "Public"
    }
  }
}
```

**Privacy Levels**:
- **Public**: Searchable by all users (including anonymous)
- **Private**: Only owner can search/see (fail-closed security)
- **FriendsOnly**: Only approved friends can search (future feature)

## 🔐 Authentication

### For Local Development
- Authorization Level: Anonymous (for testing)

### For Production (via APIM)
- **API Keys**: Subscription-based keys
- **OAuth 2.0**: Microsoft Entra ID integration (planned)
- **Rate Limiting**: 100 requests/minute, monthly quotas

## 📈 Monitoring

- **Application Insights**: Performance & error tracking
- **APIM Analytics**: API usage, response times, geographic distribution
- **Cosmos DB Metrics**: Request units, latency, storage

## 🤝 Integration Guide

### Date Night Planner Integration

```csharp
// Before (direct database access):
var profile = await _cosmosClient.ReadItemAsync<UserProfile>(id);

// After (via EntityMatchingAPI):
var client = new HttpClient();
client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
var response = await client.GetAsync($"https://api.bystorm.com/v1/profiles/{id}");
var profile = await response.Content.ReadFromJsonAsync<Profile>();
```

## 📝 Roadmap

- [x] Extract profile models from Date Night
- [x] Implement core services (Profile, Conversation, Embedding)
- [x] Create Azure Functions API
- [x] Build status: ✅ 0 errors, 0 warnings
- [x] Implement OpenAI vector embedding generation (text-embedding-3-small, 1536 dimensions)
- [x] **Vector similarity search with attribute filtering**
- [x] **Privacy-first architecture (IDs-only responses)**
- [x] **Field-level privacy controls (Public/Private/FriendsOnly)**
- [x] **13 attribute filter operators with complex AND/OR logic**
- [x] **Comprehensive test suite (unit + integration)**
- [x] **Privacy-first vector upload ⭐ NEW** (client-side embedding generation, zero PII storage)
- [x] **Multi-document conversations** (auto-sharding at 1.5MB, unlimited history)
- [ ] Deploy to Azure with APIM
- [ ] Add authentication (Microsoft Entra ID)
- [ ] Marketplace portal for businesses
- [ ] Multi-tenant support
- [ ] Friendship system for FriendsOnly privacy level

## 🌟 Use Cases

This API is domain-agnostic and can power:

- 💕 **Dating Apps**: Match romantic partners
- 💼 **Job Matching**: Connect candidates with companies
- ✈️ **Travel Companions**: Find compatible travel partners
- 🛍️ **Retail Personalization**: Recommend products based on preferences
- 🏥 **Healthcare**: Match patients with providers
- 🎓 **Education**: Connect students with mentors

## 📄 License

Proprietary - ByStorm

## 🤝 Contributing

This is a private project. For questions or access, contact admin@bystorm.com.

## 📞 Support

- **Documentation**: See [DEPLOYMENT.md](DEPLOYMENT.md)
- **Migration Plan**: See [PROFILE_API_EXTRACTION_PLAN.md](../datenightai/PROFILE_API_EXTRACTION_PLAN.md)
- **Issues**: Contact development team

---

**Built with ❤️ for the zero-trust profile marketplace vision**
