# Developer Onboarding Guide

Welcome to EntityMatching API! This guide will get you up to speed on the project architecture, development workflow, and key concepts.

## Table of Contents

1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [Local Development Setup](#local-development-setup)
4. [Key Concepts](#key-concepts)
5. [Project Structure](#project-structure)
6. [Development Workflow](#development-workflow)
7. [Testing](#testing)
8. [Deployment](#deployment)
9. [Common Scenarios](#common-scenarios)
10. [Resources](#resources)

---

## Project Overview

### What is EntityMatching API?

A **privacy-first profile matching API** that uses vector embeddings for semantic search without storing personal information.

### Key Features

1. **Privacy-First Architecture**
   - Client-side embedding generation
   - Only vectors stored (not resumes/profiles text)
   - GDPR-friendly (no PII storage)

2. **Semantic Search**
   - Natural language queries
   - Cosine similarity matching
   - Advanced attribute filtering

3. **Conversational Profiling**
   - AI-powered profile building through chat
   - Automatic insight extraction
   - Multi-document context support

4. **Production-Ready**
   - Azure Functions (serverless)
   - Cosmos DB (NoSQL)
   - APIM gateway (rate limiting, auth)
   - CI/CD with GitHub Actions

### Tech Stack

**Backend:**
- .NET 8 (C#)
- Azure Functions (isolated worker)
- Cosmos DB (NoSQL, vector search)
- OpenAI API (embeddings, chat)
- Groq API (fast LLM inference)

**Frontend/Demo:**
- Blazor WebAssembly
- Azure Static Web Apps

**Infrastructure:**
- Azure API Management (APIM)
- Azure Key Vault (secrets)
- Application Insights (monitoring)
- GitHub Actions (CI/CD)

**Client SDKs:**
- JavaScript/TypeScript (npm)
- C#/.NET (NuGet)

---

## System Architecture

### High-Level Overview

```
┌─────────────┐
│   Client    │ (Browser, mobile app, etc.)
└──────┬──────┘
       │
       │ HTTPS
       ▼
┌─────────────────┐
│  APIM Gateway   │ (Rate limiting, auth, routing)
│ api.bystorm.com │
└──────┬──────────┘
       │
       │ Internal
       ▼
┌─────────────────┐
│ Azure Functions │ (8 function classes, 40+ endpoints)
│  entityaiapi   │
└──────┬──────────┘
       │
       ├─────────────┐
       │             │
       ▼             ▼
┌──────────┐   ┌─────────┐
│ Cosmos DB│   │ OpenAI  │
│  Vectors │   │   API   │
└──────────┘   └─────────┘
```

### Request Flow Example: Search Profiles

1. **Client** sends request:
   ```
   POST https://EntityMatching-apim.azure-api.net/v1/search
   Header: Ocp-Apim-Subscription-Key: abc123
   Body: { "query": "Python engineer with AWS experience" }
   ```

2. **APIM Gateway**:
   - Validates subscription key
   - Checks rate limit (100/min for Free tier)
   - Routes to Azure Functions backend

3. **Azure Functions** (`SearchFunction.cs`):
   - Receives request
   - Generates query embedding via OpenAI
   - Queries Cosmos DB vector search
   - Filters results based on privacy settings
   - Returns matches

4. **Client** receives:
   ```json
   {
     "matches": [
       { "profileId": "abc", "similarityScore": 0.94 },
       { "profileId": "def", "similarityScore": 0.89 }
     ]
   }
   ```

### Privacy-First Upload Flow

```
┌─────────────┐
│   Browser   │
│             │
│  Resume     │ ──┐
│  Text       │   │ NEVER leaves client
│  (Private)  │   │
└─────────────┘   │
       │          │
       │ Call OpenAI API (client-side)
       ▼          │
┌─────────────┐   │
│   OpenAI    │   │
│   Embedding │   │
│   API       │◄──┘
└──────┬──────┘
       │
       │ Returns 1536-dim vector
       ▼
┌─────────────┐
│   Vector    │
│ [0.1, -0.5, │ ──┐
│  0.3, ...]  │   │ Upload to server
└─────────────┘   │
       │          │
       │ POST /embeddings/upload
       ▼          │
┌─────────────┐   │
│  Functions  │◄──┘
│   Backend   │
└──────┬──────┘
       │
       │ Store vector only
       ▼
┌─────────────┐
│  Cosmos DB  │
│   Vectors   │
└─────────────┘
```

**Key Point:** Resume text NEVER reaches our servers. Only mathematical vectors.

---

## Local Development Setup

### Prerequisites

1. **.NET 8 SDK**
   ```bash
   # Verify installation
   dotnet --version
   # Should show: 8.0.x
   ```

2. **Azure Functions Core Tools**
   ```bash
   # Verify installation
   func --version
   # Should show: 4.x
   ```

3. **Azure CLI** (for deployment)
   ```bash
   # Verify installation
   az --version
   ```

4. **Git**
   ```bash
   git --version
   ```

5. **IDE**: Visual Studio 2022, VS Code, or Rider

### Clone Repository

```bash
git clone https://github.com/iunknown21/EntityMatchingAPI.git
cd EntityMatchingAPI
```

### Configure Secrets

**Option 1: User Secrets (Recommended)**

```bash
cd EntityMatching.Functions

# Initialize user secrets
dotnet user-secrets init

# Add secrets
dotnet user-secrets set "CosmosDb:ConnectionString" "your-cosmos-connection-string"
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-key"
dotnet user-secrets set "ApiKeys:Groq" "your-groq-key"
```

**Option 2: local.settings.json**

Create `EntityMatching.Functions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDb__ConnectionString": "your-cosmos-connection-string",
    "CosmosDb__DatabaseId": "EntityMatchingDB",
    "CosmosDb__EntitiesContainerId": "profiles",
    "CosmosDb__ConversationsContainerId": "conversations",
    "CosmosDb__EmbeddingsContainerId": "embeddings",
    "OpenAI__ApiKey": "your-openai-key",
    "OpenAI__EmbeddingModel": "text-embedding-3-small",
    "OpenAI__EmbeddingDimensions": "1536",
    "ApiKeys__Groq": "your-groq-key",
    "EMBEDDING_INFRASTRUCTURE_ENABLED": "true",
    "ENABLE_EMBEDDING_GENERATION": "true"
  }
}
```

**⚠️ NEVER commit local.settings.json to Git!** (Already in `.gitignore`)

### Set Up Cosmos DB

**Create Cosmos DB account:**

```bash
# Login to Azure
az login

# Create Cosmos DB account (serverless)
az cosmosdb create \
  --name entitymatchingaidb \
  --resource-group entitymatchingai \
  --locations regionName=centralus \
  --capabilities EnableServerless

# Create database
az cosmosdb sql database create \
  --account-name entitymatchingaidb \
  --resource-group entitymatchingai \
  --name EntityMatchingDB

# Create containers
az cosmosdb sql container create \
  --account-name entitymatchingaidb \
  --resource-group entitymatchingai \
  --database-name EntityMatchingDB \
  --name profiles \
  --partition-key-path "/id"

az cosmosdb sql container create \
  --account-name entitymatchingaidb \
  --database-name EntityMatchingDB \
  --name conversations \
  --partition-key-path "/profileId"

az cosmosdb sql container create \
  --account-name entitymatchingaidb \
  --database-name EntityMatchingDB \
  --name embeddings \
  --partition-key-path "/profileId"

# Get connection string
az cosmosdb keys list \
  --name entitymatchingaidb \
  --resource-group entitymatchingai \
  --type connection-strings
```

Copy the **Primary SQL Connection String** to your secrets.

### Run Locally

**Terminal 1: Start Azure Functions**

```bash
cd EntityMatching.Functions
func start
```

Output:
```
Azure Functions Core Tools
Core Tools Version: 4.x

Functions:

  CreateProfile: [POST] http://localhost:7071/api/v1/entities
  GetProfiles: [GET] http://localhost:7071/api/v1/entities
  SearchProfiles: [POST] http://localhost:7071/api/v1/search
  ...
```

**Terminal 2: Test API**

```bash
# Create a profile
curl -X POST http://localhost:7071/api/v1/entities \
  -H "Content-Type: application/json" \
  -d '{
    "ownedByUserId": "test-user",
    "name": "Test User",
    "bio": "Software engineer",
    "isSearchable": true
  }'

# Response: { "id": "abc-123", ... }

# Get profiles
curl http://localhost:7071/api/v1/entities?userId=test-user
```

**Terminal 3: Run Blazor Demo (Optional)**

```bash
cd PrivateMatch.Demo
dotnet run
```

Open: http://localhost:5001

---

## Key Concepts

### 1. Vector Embeddings

**What are they?**

Mathematical representations of text as arrays of numbers (1536 dimensions for OpenAI's `text-embedding-3-small`).

**Example:**

```python
Text: "Senior Python engineer with AWS experience"
Embedding: [0.123, -0.456, 0.789, ..., 1536 more numbers]
```

**Why use them?**

- **Semantic similarity**: Similar text → similar vectors
- **Privacy**: Can't reverse engineer text from vector
- **Searchable**: Use cosine similarity for matching

### 2. Cosine Similarity

**How we compare vectors:**

```
similarity = cosine_similarity(vector1, vector2)
# Result: 0.0 (no match) to 1.0 (identical)
```

**Example:**

```
Query: "Python developer"
Profile A: "Senior Python engineer" → similarity: 0.92
Profile B: "JavaScript frontend dev" → similarity: 0.45

Profile A is a better match!
```

### 3. Privacy Tiers

**Who can see what:**

| Tier | Who Sees | What They See |
|------|----------|---------------|
| **Public** | Everyone | Name, bio, preferences, public fields |
| **MatchesOnly** | Matched users only | After matching, name + email revealed |
| **FriendsOnly** | Friends in network | Name, bio, contact info |
| **Private** | Nobody | Only profile ID, no PII |

**Default:** `MatchesOnly` (recommended)

### 4. Attribute Filters

**Complex filtering with semantic search:**

```json
{
  "query": "loves hiking",
  "attributeFilters": {
    "logicalOperator": "AND",
    "filters": [
      { "fieldPath": "preferences.adventure.riskTolerance", "operator": "GreaterThan", "value": 6 },
      { "fieldPath": "location.city", "operator": "Equals", "value": "Denver" }
    ]
  }
}
```

**Result:** Find profiles semantically similar to "loves hiking" AND with riskTolerance > 6 AND in Denver.

---

## Project Structure

```
EntityMatchingAPI/
├── EntityMatching.Core/              # Business logic
│   ├── Models/                        # Domain models (Profile, EmbeddingRecord, etc.)
│   ├── Services/                      # Core services
│   │   ├── EntityService.cs          # Profile CRUD operations
│   │   ├── ConversationService.cs     # Conversational profiling
│   │   ├── EmbeddingService.cs        # Embedding generation
│   │   └── SearchService.cs           # Vector search + filtering
│   └── Utilities/                     # Helpers (JsonHelper, etc.)
│
├── EntityMatching.Infrastructure/    # Data access layer
│   ├── Repositories/                  # Cosmos DB repositories
│   │   ├── ProfileRepository.cs
│   │   ├── ConversationRepository.cs
│   │   └── EmbeddingRepository.cs
│   └── Services/                      # External API clients
│       ├── OpenAIService.cs           # OpenAI API wrapper
│       └── GroqService.cs             # Groq API wrapper
│
├── EntityMatching.Functions/         # Azure Functions (API endpoints)
│   ├── ProfilesFunction.cs            # Profile CRUD endpoints
│   ├── ConversationFunction.cs        # Conversational endpoints
│   ├── EmbeddingsFunction.cs          # Embedding management
│   ├── SearchFunction.cs              # Search + matching
│   ├── MatchesFunction.cs             # Match requests
│   ├── ReputationFunction.cs          # Reputation system
│   ├── PrivacyFunction.cs             # Privacy settings
│   └── AdminFunction.cs               # Admin endpoints
│
├── EntityMatching.Shared/            # Shared models (DTOs, requests, responses)
│   └── Models/
│       ├── Profile.cs                 # Profile DTO
│       ├── SearchRequest.cs           # Search request DTO
│       ├── ConversationContext.cs     # Conversation context
│       └── ...
│
├── EntityMatching.Tests/             # Unit + integration tests
│   ├── Integration/                   # Integration tests (83 tests)
│   │   ├── ProfilesTests.cs
│   │   ├── ConversationTests.cs
│   │   ├── EmbeddingsTests.cs
│   │   └── SearchTests.cs
│   └── Unit/                          # Unit tests
│
├── EntityMatching.SDK/               # C# client SDK
│   ├── EntityMatchingClient.cs       # Main client
│   └── Endpoints/                     # Endpoint classes
│
├── EntityMatching.SDK.JS/            # JavaScript client SDK
│   └── src/
│       ├── client.ts                  # Main client
│       └── endpoints/                 # Endpoint classes
│
├── PrivateMatch.Demo/                 # Blazor demo website
│   ├── Pages/
│   │   ├── Index.razor                # Landing page
│   │   ├── UploadResume.razor         # Privacy-first upload demo
│   │   ├── SearchProfiles.razor       # Search demo
│   │   └── PrivacyProof.razor         # Privacy comparison
│   └── Services/
│
├── docs/                              # Documentation
│   ├── GETTING_STARTED.md             # Quick start guide
│   ├── API_DOCUMENTATION.md           # REST API reference
│   ├── SDK_JAVASCRIPT.md              # JavaScript SDK guide
│   ├── SDK_CSHARP.md                  # C# SDK guide
│   ├── DEMO_GUIDE.md                  # Demo website guide
│   ├── DEVELOPER_GUIDE_APIM.md        # APIM developer guide
│   ├── DEVELOPER_GUIDE_CICD.md        # CI/CD developer guide
│   └── DEVELOPER_ONBOARDING.md        # This file
│
├── .github/
│   └── workflows/                     # GitHub Actions CI/CD
│       ├── azure-functions-deploy.yml # Auto-deploy Functions
│       ├── build-and-test.yml         # PR validation
│       ├── publish-sdks.yml           # SDK publishing
│       └── deploy-demo.yml            # Demo deployment
│
├── apim-policies/                     # APIM policy XML files
│   ├── api-policy.xml                 # API-level policies
│   ├── free-tier-policy.xml           # Free tier rate limits
│   ├── premium-tier-policy.xml        # Premium tier rate limits
│   └── enterprise-tier-policy.xml     # Enterprise tier rate limits
│
└── README.md                          # Project README
```

---

## Development Workflow

### 1. Pick Up a Task

**Check issues:**
- GitHub → Issues tab
- Look for `good first issue` label for beginners
- Assign yourself to the issue

### 2. Create a Feature Branch

```bash
# Update master
git checkout master
git pull origin master

# Create feature branch
git checkout -b feature/your-feature-name

# Example:
git checkout -b feature/add-location-filter
```

### 3. Make Changes

**Example: Add a new endpoint**

**Step 1: Add function method**

Edit `EntityMatching.Functions/ProfilesFunction.cs`:

```csharp
[Function("GetPublicProfile")]
public async Task<HttpResponseData> GetPublicProfile(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/profiles/{profileId}/public")]
    HttpRequestData req,
    string profileId)
{
    try
    {
        var profile = await _profileService.GetProfileAsync(profileId);

        // Return only public fields
        var publicProfile = new
        {
            profile.Id,
            profile.Name,
            profile.Bio,
            // No email, phone, etc.
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(publicProfile);
        return response;
    }
    catch (Exception ex)
    {
        return req.CreateResponse(HttpStatusCode.NotFound);
    }
}
```

**Step 2: Test locally**

```bash
# Start Functions
cd EntityMatching.Functions
func start

# Test new endpoint
curl http://localhost:7071/api/v1/entities/abc123/public
```

**Step 3: Add integration test**

Edit `EntityMatching.Tests/Integration/ProfilesTests.cs`:

```csharp
[Fact]
public async Task GetPublicProfile_ReturnsOnlyPublicFields()
{
    // Arrange
    var profileId = await CreateTestProfile();

    // Act
    var response = await _httpClient.GetAsync($"/api/v1/entities/{profileId}/public");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();

    // Should include name and bio
    Assert.Contains("Test User", content);

    // Should NOT include email
    Assert.DoesNotContain("@", content);
}
```

**Step 4: Run tests**

```bash
cd EntityMatching.Tests
dotnet test

# Should show: Total tests: 84, Passed: 84
```

### 4. Commit Changes

```bash
# Stage changes
git add .

# Commit with descriptive message
git commit -m "Add GetPublicProfile endpoint

- Returns only public fields (name, bio)
- Excludes email, phone, and other PII
- Adds integration test for public profile
- Refs #42"

# Push to GitHub
git push origin feature/add-location-filter
```

### 5. Create Pull Request

1. GitHub → Pull requests → **New pull request**
2. Base: `master`, Compare: `feature/add-location-filter`
3. Fill in description:
   ```markdown
   ## Description
   Adds new endpoint to get public profile information without PII.

   ## Changes
   - New endpoint: GET /v1/profiles/{profileId}/public
   - Returns only public fields
   - Added integration test

   ## Testing
   - All 84 tests passing
   - Manually tested with cURL

   Closes #42
   ```
4. Click **Create pull request**

### 6. Code Review

**GitHub Actions automatically:**
- Builds code
- Runs all tests
- Reports results on PR

**Wait for:**
- ✅ All checks passed
- ✅ Code review approval (if required)

### 7. Merge

1. Click **Merge pull request**
2. Confirm merge
3. Delete branch (optional)

**GitHub Actions automatically deploys to production!**

### 8. Verify Deployment

```bash
# Wait 3-5 minutes for deployment

# Test production endpoint
curl https://EntityMatching-apim.azure-api.net/v1/profiles/abc123/public
```

---

## Testing

### Unit Tests

**Test individual methods in isolation:**

```csharp
public class ProfileServiceTests
{
    [Fact]
    public void CalculateSimilarity_ReturnsBetween0And1()
    {
        // Arrange
        var vector1 = new[] { 0.1, 0.2, 0.3 };
        var vector2 = new[] { 0.2, 0.3, 0.4 };

        // Act
        var similarity = EntityService.CalculateCosineSimilarity(vector1, vector2);

        // Assert
        Assert.InRange(similarity, 0.0, 1.0);
    }
}
```

### Integration Tests

**Test full API endpoints end-to-end:**

```csharp
public class ProfilesIntegrationTests
{
    [Fact]
    public async Task CreateProfile_ReturnsCreatedProfile()
    {
        // Arrange
        var profile = new Profile { Name = "Test", Bio = "Test bio" };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/entities", profile);

        // Assert
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Profile>();
        Assert.NotNull(created.Id);
    }
}
```

### Run All Tests

```bash
cd EntityMatching.Tests

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~ProfilesTests"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Test Settings

**Tests use:** `EntityMatching.Tests/testsettings.json`

```json
{
  "CosmosDb": {
    "ConnectionString": "your-test-cosmos-connection-string",
    "DatabaseId": "EntityMatchingDB-Test"
  },
  "OpenAI": {
    "ApiKey": "your-openai-key"
  }
}
```

**⚠️ Use separate Cosmos DB database for tests!**

---

## Deployment

### Manual Deployment

**Deploy Azure Functions:**

```bash
cd EntityMatching.Functions

# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./output

# Create zip
cd output
zip -r ../output.zip .
cd ..

# Login to Azure
az login

# Deploy
az functionapp deployment source config-zip \
  --resource-group entitymatchingai \
  --name entityaiapi \
  --src ./output.zip
```

### Automatic Deployment (CI/CD)

**Already configured!** Just push to master:

```bash
git push origin master
```

**GitHub Actions automatically:**
1. Builds code
2. Runs tests
3. Deploys to Azure (if tests pass)

**Monitor deployment:**
- GitHub → Actions tab
- See workflow run in real-time

### Deployment Checklist

Before deploying major changes:

- [ ] All tests passing locally
- [ ] Code reviewed and approved
- [ ] Database migrations applied (if needed)
- [ ] Secrets updated in Key Vault (if needed)
- [ ] APIM policies updated (if API changes)
- [ ] Documentation updated
- [ ] Changelog updated

---

## Common Scenarios

### Scenario 1: Add New Preference Field

**Goal:** Add `travelPreferences` to profile

**Steps:**

1. **Update model** (`EntityMatching.Shared/Models/Profile.cs`):
   ```csharp
   public class Profile
   {
       // ... existing fields
       public TravelPreferences? TravelPreferences { get; set; }
   }

   public class TravelPreferences
   {
       public List<string> PreferredDestinations { get; set; }
       public string TravelStyle { get; set; } // "Budget", "Luxury", "Adventure"
       public int TravelsPerYear { get; set; }
   }
   ```

2. **Update tests** (add sample data)

3. **Update documentation** (SDK guides, API docs)

4. **Deploy** (push to master)

**No backend code changes needed!** Cosmos DB is schema-less.

### Scenario 2: Add Custom Filtering

**Goal:** Filter by travel style

**Steps:**

1. **Use existing attribute filters:**
   ```bash
   curl -X POST https://api/v1/search \
     -H "Content-Type: application/json" \
     -d '{
       "query": "loves adventure travel",
       "attributeFilters": {
         "filters": [
           {
             "fieldPath": "travelPreferences.travelStyle",
             "operator": "Equals",
             "value": "Adventure"
           }
         ]
       }
     }'
   ```

**Already works!** No code changes needed.

### Scenario 3: Debug Production Issue

**Steps:**

1. **Check Application Insights:**
   - Azure Portal → Function App → Application Insights
   - Query logs for errors

2. **Enable detailed logging:**
   ```csharp
   _logger.LogInformation("Processing profile {ProfileId}", profileId);
   ```

3. **Test in production:**
   ```bash
   curl https://EntityMatching-apim.azure-api.net/v1/profiles \
     -H "Ocp-Apim-Trace: true"
   ```
   - Response includes trace URL with detailed execution

4. **Deploy hotfix** (see "Development Workflow")

---

## Resources

### Documentation

- **Getting Started**: [GETTING_STARTED.md](./GETTING_STARTED.md)
- **API Reference**: [CORE_PLATFORM_API.md](./CORE_PLATFORM_API.md)
- **JavaScript SDK**: [SDK_JAVASCRIPT.md](./SDK_JAVASCRIPT.md)
- **C# SDK**: [SDK_CSHARP.md](./SDK_CSHARP.md)
- **Demo Guide**: [DEMO_GUIDE.md](./DEMO_GUIDE.md)
- **APIM Guide**: [DEVELOPER_GUIDE_APIM.md](./DEVELOPER_GUIDE_APIM.md)
- **CI/CD Guide**: [DEVELOPER_GUIDE_CICD.md](./DEVELOPER_GUIDE_CICD.md)

### Azure Resources

- **Function App**: https://portal.azure.com/#resource/subscriptions/09f915e1-47f8-47c7-809d-cd0e924b928b/resourceGroups/entitymatchingai/providers/Microsoft.Web/sites/entityaiapi
- **APIM**: `EntityMatching-apim`
- **Cosmos DB**: `entitymatchingaidb`

### External Links

- **OpenAI API Docs**: https://platform.openai.com/docs/api-reference
- **Azure Functions Docs**: https://learn.microsoft.com/en-us/azure/azure-functions/
- **Cosmos DB Docs**: https://learn.microsoft.com/en-us/azure/cosmos-db/
- **GitHub Actions Docs**: https://docs.github.com/en/actions

### Support

- **Internal Questions**: Ask team in Slack/Teams
- **Bug Reports**: GitHub Issues
- **Feature Requests**: GitHub Issues with `enhancement` label

---

## Next Steps

Now that you're onboarded:

1. **Set up local environment** (see "Local Development Setup")
2. **Run the project locally** (`func start`)
3. **Run all tests** (`dotnet test`)
4. **Pick up a "good first issue"** from GitHub
5. **Create your first PR!**

Welcome to the team! 🎉
