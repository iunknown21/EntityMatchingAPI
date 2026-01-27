# Profile and AI Matching API Extraction Plan

**Date:** 2025-12-26
**Project:** Date Night Planner → EntityMatchingAPI
**Revised for:** Manual Migration (Few Profiles)

---

## Executive Summary

This document provides a comprehensive analysis and migration plan for extracting the profile and AI matching logic from the Date Night Planner monolith into a standalone, domain-agnostic API service. The new API will power a zero-trust profile marketplace via embeddings and support multiple verticals (dating, jobs, travel, retail).

### Key Findings

- **Profile Infrastructure**: Highly sophisticated 8+ dimensional preference system with 50+ model files, conversation-based augmentation, and embedding preparation
- **AI Matching**: Groq-powered search, OpenAI compatibility, with placeholder embedding service ready for production implementation
- **Current State**: Already partially modular with clean service boundaries, but tightly coupled to Date Night domain
- **Extraction Complexity**: Medium (5-6 weeks with manual migration vs 8 weeks with automated migration)
- **Risk Level**: Low-Medium (manual migration significantly reduces risk)

### Timeline Summary

| Approach | Timeline | Complexity | Risk |
|----------|----------|------------|------|
| **Original (Automated Migration)** | 8 weeks | High | Medium |
| **Revised (Manual Migration)** | **5-6 weeks** | **Medium** | **Low-Medium** |

**Recommended Approach**: Manual migration with clean cutover - Build Profile API completely, test thoroughly, manually copy few profiles, deploy both services together.

---

## Revised Effort Estimate (Manual Migration)

### What Changes with Manual Migration

**Eliminated Complexity:**
- ❌ **Bulk migration tool** - No need for ProfileMigrationService, batch processing, retry logic (saved: 5 days)
- ❌ **Dual-write pattern** - No need to write to both databases simultaneously (saved: 2 days)
- ❌ **Migration reconciliation** - No need to sync/validate thousands of profiles (saved: 2 days)
- ❌ **Gradual rollout** - Can do clean cutover instead of 10%→50%→100% (saved: 2 days)
- ❌ **Migration-specific testing** - No need to test migration edge cases (saved: 3 days)

**Total time saved: ~14 days (nearly 3 weeks!)**

### Revised Timeline

| Phase | Original | Revised | Change |
|-------|----------|---------|--------|
| Phase 1: Foundation Setup | 1 week | 1 week | No change |
| Phase 2: Service Layer | 1 week | 1 week | No change |
| Phase 3: Azure Functions API | 1 week | 1 week | No change |
| Phase 4: Authentication | 1 week | 1 week | No change |
| ~~Phase 5: Data Migration~~ | ~~1 week~~ | ~~0.5 days~~ | **-4.5 days** |
| Phase 6: Date Night Integration | 1 week | 3 days | **-2 days** |
| Phase 7-8: Embedding Production | 2 weeks | 2 weeks | No change (optional) |
| **TOTAL to Production** | **8 weeks** | **5-6 weeks** | **-2-3 weeks saved** |

### Revised Migration Strategy

**New Approach: "Build Complete, Then Cutover"**

```
┌─────────────────────────────────────────────┐
│ Week 1-4: Build Profile API in Isolation    │
│ - No changes to Date Night                  │
│ - Test with mock data                       │
│ - Complete API ready to go                  │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ Week 5: Integration & Manual Migration      │
│ Day 1-2: Update Date Night client code      │
│ Day 3:   Test in development                │
│ Day 4:   Manual profile migration (1 hour)  │
│ Day 5:   Deploy both services               │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│ Week 6: Polish & Embeddings (Optional)      │
│ - Production monitoring                     │
│ - Implement embedding generation            │
│ - Prepare for marketplace features          │
└─────────────────────────────────────────────┘
```

### Manual Migration Process Options

#### Option 1: Manual Copy-Paste (30-60 minutes)
```
1. Open Date Night production database in Azure Portal
2. Open Profile API database in Azure Portal
3. For each profile document:
   - Copy JSON from DateNightPlannerDB.profiles
   - Paste into EntityMatchingDB.profiles
   - Remove Date Night-specific fields (RelationshipData, etc.)
4. Verify profile count matches
```

#### Option 2: Simple Migration Script (2 hours to write + 5 minutes to run)
```csharp
// DateNightProfileMigrator.cs (one-time use)
public class SimpleProfileMigrator
{
    public async Task MigrateAllProfilesAsync()
    {
        // 1. Read all profiles from Date Night DB
        var profiles = await _dateNightContainer.GetItemLinqQueryable<UserProfile>()
            .ToFeedIterator()
            .ReadNextAsync();

        Console.WriteLine($"Found {profiles.Count} profiles to migrate");

        // 2. Copy to Profile API DB
        foreach (var profile in profiles)
        {
            var cleaned = CleanProfile(profile);
            await _profileApiContainer.CreateItemAsync(cleaned);
            Console.WriteLine($"✓ Migrated: {profile.Name}");
        }

        Console.WriteLine("Migration complete!");
    }

    private Profile CleanProfile(UserProfile source)
    {
        // Remove Date Night-specific properties
        return new Profile
        {
            Id = source.Id,
            Name = source.Name,
            OwnedByUserId = source.OwnedByUserId,
            // ... copy domain-agnostic fields only
        };
    }
}
```

#### Option 3: Export/Import via JSON files (15 minutes)
```bash
# Export from Date Night
az cosmosdb query --query "SELECT * FROM c" > profiles_backup.json

# Clean up in text editor (remove Date Night fields)

# Import to Profile API
az cosmosdb import --file profiles_cleaned.json
```

### Benefits of Manual Migration

**Simpler Architecture:**
- No dual-write complexity to maintain
- No migration reconciliation jobs
- Cleaner code (no temporary migration logic)

**Lower Risk:**
- Test new API completely before touching production
- Single cutover point (easier to rollback)
- No gradual rollout confusion

**Faster Time to Market:**
- 5-6 weeks instead of 8 weeks to production
- Can start building marketplace features sooner
- Simpler deployment process

---

## 1. DETAILED INVENTORY

### 1.1 Profile Core Models (To Extract)

**Primary Profile Model** - `DateNightPlanner.Core\Models\UserProfile.cs`
- 397 lines, comprehensive partner profiling
- 8+ dimensional preference system
- Profile ownership and invite system
- Last modified tracking for embeddings
- **Status**: Move as-is with domain abstraction

**Preference Categories** (All extractable):
1. `EntertainmentPreferences.cs` - Music, movies, shows, books, games, aesthetics
2. `AdventurePreferences.cs` - Activity level, risk tolerance, novelty seeking
3. `LearningPreferences.cs` - Intellectual interests, cultural appreciation
4. `SensoryPreferences.cs` - Noise, lighting, crowds, touch, textures, temperatures
5. `SocialPreferences.cs` - Interaction style, group preferences, PDA
6. `StylePreferences.cs` - Fashion, aesthetics, ambiance
7. `NaturePreferences.cs` - Outdoor vs indoor, seasonal, weather
8. `GiftPreferences.cs` - Gift giving/receiving styles
9. `AccessibilityNeeds.cs` - Accommodations and accessibility requirements
10. `DietaryRestrictions.cs` - Food restrictions and allergies
11. `ActivityPreferences.cs` - Activity-specific preferences

**Supporting Models** (Extractable):
- `LoveLanguages.cs` - 5 love languages scoring
- `PersonalityClassifications.cs` - MBTI, Big Five, Enneagram
- `ProfileImage.cs` - Profile image metadata
- `ImportantDate.cs` - Date tracking
- `PreferencesAndInterests.cs` - Legacy preference container
- `ExperiencePreferences.cs` - Experience-level preferences
- `RelationshipData.cs` - **DATE NIGHT SPECIFIC** - needs abstraction or removal
- `PartnerInsights.cs` - **DATE NIGHT SPECIFIC** - needs abstraction or removal
- `RecognitionAndAppreciation.cs` - **DATE NIGHT SPECIFIC** - needs abstraction or removal

### 1.2 Embedding Infrastructure (To Extract)

**Models**:
- `EntityEmbedding.cs` - Vector storage model with summary, hash, status tracking
  - Partition key: `/profileId`
  - Embedding status: Pending, Generated, Failed
  - Summary hash for change detection
  - Metadata about summary composition

**Interfaces**:
- `IProfileSummaryService.cs` - Generate text summaries for embedding
- `IEmbeddingService.cs` - Generate vector embeddings
- `IEmbeddingStorageService.cs` - Manage embeddings in Cosmos DB

**Services**:
- `ProfileSummaryService.cs` - 450 lines, extracts profile to natural language
- `OpenAIEmbeddingService.cs` - OpenAI text-embedding-3-small implementation
- `EmbeddingStorageService.cs` - Cosmos DB operations for embeddings

**Status**: All embedding infrastructure is domain-agnostic and ready to extract

### 1.3 Conversational Profiling (To Extract)

**Models**:
- `ConversationContext.cs` - Conversation history + AI-extracted insights
  - `ConversationChunk` - User/AI message pairs
  - `ExtractedInsight` - Category, confidence, source tracking
  - Partition key: `/profileId`

**Interfaces**:
- `IConversationService.cs` - Process messages, extract insights, manage history

**Services**:
- `ConversationService.cs` - 353 lines, Groq-powered conversation processing
  - Model: `llama-3.3-70b-versatile`
  - Insight extraction with confidence scoring
  - Auto-creates Cosmos DB container
  - **Status**: Fully extractable, domain-agnostic

### 1.4 Profile Management Services (To Extract with Refactoring)

**Interfaces**:
- `IEntityService.cs` - CRUD operations for profiles
- `IProfileSharingService.cs` - Share profiles with privacy controls
- `IProfileInviteService.cs` - Invite-based profile creation

**Services**:
- `EntityService.cs` - 659 lines, Cosmos DB operations
  - Container: `profiles`, partition key: `/id`
  - In-memory fallback for development
  - Batch operations for performance
  - Auto-initialization
  - **Coupling**: References `DateNightUser` via `OwnedByUserId`

- `ProfileSharingService.cs` - 1008 lines, sophisticated sharing system
  - QR code generation
  - Privacy levels: Basic, Personality, Comprehensive
  - GDPR cascade deletion
  - Rate limiting (subscription-aware)
  - **Coupling**: Heavily coupled to Date Night subscription tiers

### 1.5 User Management (Hybrid - Partial Extraction)

**Model**:
- `DateNightUser.cs` - 1258 lines, comprehensive user account model
  - Subscription management (Stripe)
  - Usage tracking (searches, profiles, shares)
  - Trial management
  - Beta access, unlimited overrides
  - **Status**: **CANNOT fully extract** - contains Date Night-specific business logic
  - **Approach**: Create abstracted `MarketplaceUser` or `ProfileUser` model

**Interfaces**:
- `IUserService.cs` - User account management

**Services**:
- `UserService.cs` - User CRUD, subscription enforcement, usage tracking
  - **Status**: **STAYS in Date Night** - but new API needs lightweight user identity

### 1.6 AI and Matching Services (Partial Extraction)

**Date Discovery** - **STAYS in Date Night**:
- `SearchFirstEventService.cs` - Groq web search for date discovery
- **Reason**: Domain-specific (dates, events)

**Gift Recommendations** - **STAYS in Date Night**:
- `GiftRecommendationService.cs` - Groq web search for gifts
- **Reason**: Domain-specific use case

**Generic AI Services** - **EXTRACTABLE**:
- `OpenAIService.cs` - Generic AI provider abstraction
- `PromptManager.cs` - Prompt template management
- Pattern: Groq API integration with retry logic
- **Status**: Extract for reuse in profile matching

### 1.7 Infrastructure Dependencies

**Cosmos DB Containers** (To Replicate or Share):
1. `profiles` - Partition: `/id` - User profiles
2. `conversationContexts` - Partition: `/profileId` - Conversation history
3. `profile-embeddings` - Partition: `/profileId` - Vector embeddings
4. `profileInvites` - Partition: `/inviteToken` - Invite system
5. `profileShares` - Partition: `/shareToken` - Sharing system

**Configuration Requirements**:
- `CosmosDb:ConnectionString`
- `CosmosDb:DatabaseId`
- `CosmosDb:EntitiesContainerId`
- `ApiKeys:Groq`
- `ApiKeys:OpenAI` (optional)

**Utilities** (To Extract):
- `JsonHelper.cs` - Consistent JSON serialization
- **Status**: Essential for cross-solution compatibility

### 1.8 Azure Functions API Endpoints (To Replicate)

**Profile Management**:
- `ProfilesFunctions.cs` - 23KB, CRUD endpoints
  - GET `/profiles` - List profiles for user
  - GET `/profiles/{id}` - Get profile by ID
  - POST `/profiles` - Create profile
  - PUT `/profiles/{id}` - Update profile
  - DELETE `/profiles/{id}` - Delete profile

**Conversation**:
- `ConversationFunctions.cs` - 8KB
  - POST `/conversations/{profileId}/message` - Process message
  - GET `/conversations/{profileId}` - Get history
  - DELETE `/conversations/{profileId}` - Clear history

**Profile Sharing**:
- `ProfileSharingFunctions.cs` - 29KB
  - POST `/share/create` - Create share link
  - GET `/share/{shareId}` - View shared profile
  - POST `/share/import` - Import shared profile

**Embedding Generation**:
- `GenerateProfileSummariesFunction.cs` - 12KB
  - HTTP-triggered function to generate/update embeddings
  - **Status**: Extractable

### 1.9 Client-Side Services (To Update, Not Extract)

**Blazor Client Services** - `DateNightPlanner.Client\Services\`
- `EntityService.cs` - HTTP client for profile API
- `ConversationService.cs` - HTTP client for conversations
- `ProfileSharingService.cs` - HTTP client for sharing
- **Action**: Update to call new API instead of Date Night Functions

**Authentication**:
- Microsoft Entra External ID (CIAM)
- Authority: `https://datenightplanner.ciamlogin.com/...`
- ClientId: `a4c6ee8b-e89d-4ac8-88b5-51afffaf6b33`
- **Challenge**: New API needs its own auth or shared tenant

---

## 2. SEPARATION STRATEGY

### 2.1 Components That Can Move As-Is

**Core Profile Models** (90% ready):
- ✅ `UserProfile.cs` - Minor cleanup needed (remove Date Night-specific computed properties)
- ✅ All preference models (Entertainment, Adventure, Learning, Sensory, Social, Style, Nature, Gift)
- ✅ `ProfileImage.cs`
- ✅ `ImportantDate.cs`
- ✅ `LoveLanguages.cs`
- ✅ `PersonalityClassifications.cs`
- ✅ `AccessibilityNeeds.cs`
- ✅ `DietaryRestrictions.cs`

**Embedding Infrastructure** (100% ready):
- ✅ `EntityEmbedding.cs`
- ✅ `SummaryMetadata.cs`
- ✅ `IProfileSummaryService.cs` + implementation
- ✅ `IEmbeddingService.cs` + placeholder
- ✅ `IEmbeddingStorageService.cs` + implementation

**Conversation System** (100% ready):
- ✅ `ConversationContext.cs`
- ✅ `ConversationChunk.cs`
- ✅ `ExtractedInsight.cs`
- ✅ `IConversationService.cs` + implementation

**Utilities**:
- ✅ `JsonHelper.cs`

### 2.2 Components Needing Refactoring for Domain-Agnostic Use

**High Priority Refactoring**:

1. **UserProfile.cs** - Remove Date Night-specific properties
   ```csharp
   // REMOVE (Date Night specific):
   - RelationshipData (milestones, successful dates, traditions)
   - PartnerInsights (common interests, complementary traits)
   - RecognitionAndAppreciation (validation methods, achievements)
   - ProfileTypeValue enum (Me vs Partner) -> Generic "SelfProfile" vs "OtherProfile"
   - IsAutoSearchEnabled -> Domain-agnostic "AutoProcessingEnabled"

   // KEEP (Domain-agnostic):
   - All preference categories
   - Profile ownership (OwnedByUserId)
   - Invite system (CreatedViaInvite, EditToken)
   - LastModified timestamp
   ```

2. **EntityService.cs** - Decouple from DateNightUser
   ```csharp
   // Current: Uses OwnedByUserId to link to DateNightUser
   // New: Generic userId (string) without user object coupling

   // Change GetProfileAsync(string id, string userId) to validate ownership
   // without fetching full DateNightUser
   ```

3. **ProfileSharingService.cs** - Remove subscription tier logic
   ```csharp
   // REMOVE:
   - Subscription tier checks (requires Date Night business logic)
   - Rate limiting based on subscription
   - Upgrade prompts

   // REPLACE WITH:
   - Generic quota system (configurable limits per user)
   - External authorization checks (caller provides allowed=true/false)
   ```

4. **ConversationService.cs** - Already domain-agnostic
   - ✅ No changes needed

### 2.3 Components That Must Stay in Date Night

**User Account Management**:
- ❌ `DateNightUser.cs` - Too coupled to Date Night business logic
- ❌ `UserService.cs` - Subscription enforcement, usage tracking
- ❌ `SubscriptionService.cs` - Stripe integration
- ❌ `StripeService.cs` - Payment processing

**Domain-Specific Features**:
- ❌ `SearchFirstEventService.cs` - Date discovery
- ❌ `GiftRecommendationService.cs` - Gift recommendations
- ❌ `DateIdea.cs` - Event representation
- ❌ `GiftItem.cs` - Gift models
- ❌ All date-related Azure Functions

**Blazor Client**:
- ❌ Entire client app stays with Date Night
- Updates: HTTP client services point to new Profile API

### 2.4 Shared Dependencies Strategy

**Option A: Duplicate Core Models** (Recommended for Independence)
- Copy `UserProfile.cs` and preference models to new solution
- Refactor to remove Date Night specifics
- Accept some code duplication for deployment independence
- **Pros**: No shared library dependency, independent versioning
- **Cons**: Manual sync for bug fixes

**Option B: Shared Library Approach**
- Create `ProfileCore.Shared` NuGet package
- Both solutions reference shared package
- **Pros**: Single source of truth, no duplication
- **Cons**: Coordinated deployments, versioning complexity

**Recommendation**: **Option A** for MVP, consider Option B after stabilization

---

## 3. API SURFACE DESIGN

### 3.1 New API: EntityMatchingAPI (ai.bystorm.com)

**Base URL**: `https://ai.bystorm.com/api/v1`

### 3.2 Authentication Strategy

**Option 1: Shared Entra ID Tenant** (Recommended)
- Use existing Microsoft Entra External ID tenant
- Create new application registration for Profile API
- Date Night client can use same tenant, different scopes
- **Pros**: Seamless SSO, user identity continuity
- **Cons**: Tenant coupling

**Option 2: Service-to-Service Auth**
- Date Night Functions call Profile API with API key or certificate
- Profile API trusts Date Night's user claims
- **Pros**: Independent authentication
- **Cons**: Additional auth layer, complexity

**Recommendation**: Option 1 for MVP (shared tenant with scoped app registration)

### 3.3 HTTP Endpoints

#### Profile Management

```http
GET /api/v1/entities
Authorization: Bearer {token}
Response: 200 OK, Profile[]

GET /api/v1/entities/{profileId}
Authorization: Bearer {token}
Response: 200 OK, Profile

POST /api/v1/entities
Authorization: Bearer {token}
Content-Type: application/json
Body: CreateProfileRequest
Response: 201 Created, Profile

PUT /api/v1/entities/{profileId}
Authorization: Bearer {token}
Content-Type: application/json
Body: UpdateProfileRequest
Response: 200 OK, Profile

DELETE /api/v1/entities/{profileId}
Authorization: Bearer {token}
Response: 204 No Content
```

#### Conversation Augmentation

```http
POST /api/v1/entities/{profileId}/conversation
Authorization: Bearer {token}
Content-Type: application/json
Body: { "message": "They love hiking and photography" }
Response: 200 OK
{
  "aiResponse": "That's great! Do they prefer...",
  "newInsights": [
    { "category": "hobby", "insight": "enjoys hiking", "confidence": 0.9 }
  ]
}

GET /api/v1/entities/{profileId}/conversation/history
Authorization: Bearer {token}
Response: 200 OK, ConversationContext

GET /api/v1/entities/{profileId}/conversation/insights
Authorization: Bearer {token}
Response: 200 OK, string (formatted summary)

DELETE /api/v1/entities/{profileId}/conversation
Authorization: Bearer {token}
Response: 204 No Content
```

#### Embedding Management

```http
GET /api/v1/entities/{profileId}/embedding
Authorization: Bearer {token}
Response: 200 OK, EntityEmbedding

POST /api/v1/entities/{profileId}/embedding/generate
Authorization: Bearer {token}
Response: 202 Accepted
{
  "status": "pending",
  "embeddingId": "embedding_abc123"
}

GET /api/v1/entities/{profileId}/embedding/status
Authorization: Bearer {token}
Response: 200 OK
{
  "status": "generated|pending|failed",
  "error": null
}
```

#### Profile Sharing (Simplified)

```http
POST /api/v1/entities/{profileId}/share
Authorization: Bearer {token}
Content-Type: application/json
Body: {
  "privacyLevel": "comprehensive",
  "expirationHours": 24,
  "password": "optional",
  "maxViews": 10
}
Response: 201 Created
{
  "shareId": "abc123",
  "shareUrl": "https://ai.bystorm.com/share/abc123",
  "qrCode": "data:image/png;base64,..."
}

GET /api/v1/share/{shareId}
Query: ?password=optional
Response: 200 OK, SharedProfileData

POST /api/v1/entities/import
Authorization: Bearer {token}
Body: {
  "shareId": "abc123",
  "password": "optional",
  "customName": "Sarah"
}
Response: 201 Created, Profile
```

#### Profile Ownership (New Concept for Marketplace)

```http
POST /api/v1/entities/{profileId}/ownership/transfer
Authorization: Bearer {token}
Body: { "newOwnerId": "user-xyz" }
Response: 200 OK

POST /api/v1/entities/{profileId}/ownership/share
Authorization: Bearer {token}
Body: { "sharedWithUserId": "user-abc", "permissions": "read|write" }
Response: 200 OK
```

#### Search/Matching (Future - Embedding-based)

```http
POST /api/v1/entities/search
Authorization: Bearer {token}
Content-Type: application/json
Body: {
  "queryText": "Looking for outdoor enthusiasts who love photography",
  "filters": {
    "location": "Seattle",
    "ageRange": [25, 35]
  },
  "limit": 10
}
Response: 200 OK
{
  "results": [
    {
      "profileId": "abc123",
      "matchScore": 0.92,
      "profile": { ... }
    }
  ]
}
```

### 3.4 Request/Response Models

```csharp
// CreateProfileRequest
public class CreateProfileRequest
{
    public string Name { get; set; }
    public string? Bio { get; set; }
    public ProfileVisibility Visibility { get; set; } = ProfileVisibility.Private;
    public Dictionary<string, object>? Preferences { get; set; }
    // ... other profile fields
}

// UpdateProfileRequest
public class UpdateProfileRequest
{
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public Dictionary<string, object>? Preferences { get; set; }
    // Partial update support
}

// Profile (Response)
public class ProfileResponse
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public ProfileVisibility Visibility { get; set; }
    public object Preferences { get; set; }
    public EmbeddingStatus? EmbeddingStatus { get; set; }
}

// Error Response
public class ApiErrorResponse
{
    public string Error { get; set; }
    public string Message { get; set; }
    public string? Details { get; set; }
    public int StatusCode { get; set; }
}
```

---

## 4. DATA MIGRATION STRATEGY

### 4.1 Database Strategy: Separate from Start (Recommended)

**Approach**: Create new Cosmos DB database for Profile API from day 1

**Database**: `EntityMatchingDB` (or `MarketplaceProfilesDB`)

**Containers** (replicate structure):
1. `profiles` - Partition: `/id`
2. `conversations` - Partition: `/profileId`
3. `embeddings` - Partition: `/profileId`
4. `shares` - Partition: `/shareToken`
5. `users` - Partition: `/id` - Lightweight user identity only

### 4.2 Manual Migration Plan (Revised for Few Profiles)

**Phase 1: Build Profile API Completely** (Week 1-4)
```
Profile API Development
    ↓
Test with mock data in isolation
    ↓
No changes to Date Night yet
```

**Phase 2: Integration Preparation** (Week 5, Day 1-3)
```
Update Date Night EntityService to call ProfileAPI
    ↓
Test in development environment
    ↓
Verify all functionality works
```

**Phase 3: Manual Migration** (Week 5, Day 4 - 1 hour)
```
Option A: Copy-paste in Azure Portal (30-60 min)
Option B: Run simple migration script (5 min)
Option C: Export/import JSON files (15 min)
    ↓
Verify profile count matches
    ↓
Test profile access in new API
```

**Phase 4: Production Deployment** (Week 5, Day 4-5)
```
Deploy Profile API to production
    ↓
Deploy updated Date Night with ProfileApiClient
    ↓
Monitor and verify
```

### 4.3 Handling Existing Date Night User Data

**Challenge**: Few profiles in production, no advertising yet

**Solution**: Manual copy with optional cleanup script

**Recommended Migration Script** (Simple, 2 hours to write):

```csharp
// SimpleProfileMigrator.cs
public class SimpleProfileMigrator
{
    private readonly Container _sourceContainer; // Date Night DB
    private readonly Container _targetContainer; // Profile API DB
    private readonly ILogger _logger;

    public async Task<MigrationReport> MigrateAllAsync()
    {
        var report = new MigrationReport();

        // 1. Read all profiles
        var query = "SELECT * FROM c";
        var iterator = _sourceContainer.GetItemQueryIterator<UserProfile>(query);

        var profiles = new List<UserProfile>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            profiles.AddRange(response);
        }

        _logger.LogInformation($"Found {profiles.Count} profiles to migrate");

        // 2. Clean and copy each profile
        foreach (var profile in profiles)
        {
            try
            {
                var cleaned = CleanProfile(profile);
                await _targetContainer.CreateItemAsync(cleaned);

                report.SuccessCount++;
                _logger.LogInformation($"✓ Migrated: {profile.Name} ({profile.Id})");
            }
            catch (Exception ex)
            {
                report.FailedProfiles.Add((profile.Id, ex.Message));
                _logger.LogError(ex, $"✗ Failed to migrate: {profile.Name}");
            }
        }

        // 3. Verify
        var targetCount = await CountProfilesAsync(_targetContainer);
        report.SourceCount = profiles.Count;
        report.TargetCount = targetCount;

        return report;
    }

    private Profile CleanProfile(UserProfile source)
    {
        return new Profile
        {
            Id = source.Id,
            Name = source.Name,
            OwnedByUserId = source.OwnedByUserId,
            Bio = source.Bio,
            ProfileImage = source.ProfileImage,

            // Copy all preference categories
            EntertainmentPreferences = source.EntertainmentPreferences,
            AdventurePreferences = source.AdventurePreferences,
            LearningPreferences = source.LearningPreferences,
            SensoryPreferences = source.SensoryPreferences,
            SocialPreferences = source.SocialPreferences,
            StylePreferences = source.StylePreferences,
            NaturePreferences = source.NaturePreferences,
            GiftPreferences = source.GiftPreferences,
            AccessibilityNeeds = source.AccessibilityNeeds,
            DietaryRestrictions = source.DietaryRestrictions,

            LoveLanguages = source.LoveLanguages,
            PersonalityClassifications = source.PersonalityClassifications,
            ImportantDates = source.ImportantDates,

            CreatedAt = source.CreatedAt,
            LastModified = source.LastModified,

            // SKIP Date Night-specific fields:
            // - RelationshipData
            // - PartnerInsights
            // - RecognitionAndAppreciation
            // - IsAutoSearchEnabled
        };
    }
}

public class MigrationReport
{
    public int SourceCount { get; set; }
    public int TargetCount { get; set; }
    public int SuccessCount { get; set; }
    public List<(string ProfileId, string Error)> FailedProfiles { get; set; } = new();

    public bool IsComplete => SourceCount == TargetCount && FailedProfiles.Count == 0;
}
```

### 4.4 User Identity Mapping

**Solution: Use Existing Entra ID** (Recommended)
- Profile API reads same Entra ID claims
- `userId` = Entra ID subject claim (`sub`)
- No additional user mapping needed
- Seamless identity continuity

---

## 5. PROJECT STRUCTURE PROPOSAL

### 5.1 New Solution: EntityMatchingAPI

```
EntityMatchingAPI/
├── EntityMatching.Core/                 # Domain models and interfaces
│   ├── Models/
│   │   ├── Profile.cs                   # Clean profile model
│   │   ├── Preferences/
│   │   │   ├── EntertainmentPreferences.cs
│   │   │   ├── AdventurePreferences.cs
│   │   │   ├── LearningPreferences.cs
│   │   │   ├── SensoryPreferences.cs
│   │   │   ├── SocialPreferences.cs
│   │   │   ├── StylePreferences.cs
│   │   │   ├── NaturePreferences.cs
│   │   │   ├── GiftPreferences.cs
│   │   │   ├── AccessibilityNeeds.cs
│   │   │   ├── DietaryRestrictions.cs
│   │   │   └── ActivityPreferences.cs
│   │   ├── Personality/
│   │   │   ├── LoveLanguages.cs
│   │   │   └── PersonalityClassifications.cs
│   │   ├── Embedding/
│   │   │   ├── EntityEmbedding.cs
│   │   │   ├── SummaryMetadata.cs
│   │   ├── Conversation/
│   │   │   ├── ConversationContext.cs
│   │   │   ├── ConversationChunk.cs
│   │   │   ├── ExtractedInsight.cs
│   │   ├── Sharing/
│   │   │   ├── ProfileShare.cs
│   │   │   ├── SharedProfileData.cs
│   │   ├── User/
│   │   │   ├── MarketplaceUser.cs      # Lightweight user identity
│   │   └── Common/
│   │       ├── ProfileImage.cs
│   │       └── ImportantDate.cs
│   ├── Interfaces/
│   │   ├── IEntityService.cs
│   │   ├── IConversationService.cs
│   │   ├── IEmbeddingService.cs
│   │   ├── IProfileSummaryService.cs
│   │   ├── IEmbeddingStorageService.cs
│   │   └── IProfileSharingService.cs
│   ├── Utilities/
│   │   └── JsonHelper.cs
│   └── EntityMatching.Core.csproj
│
├── EntityMatching.Infrastructure/       # Service implementations
│   ├── Services/
│   │   ├── EntityService.cs            # Cosmos DB CRUD
│   │   ├── ConversationService.cs       # Groq-powered conversations
│   │   ├── ProfileSummaryService.cs     # Text summary generation
│   │   ├── OpenAIEmbeddingService.cs    # OpenAI embedding provider
│   │   ├── GroqEmbeddingService.cs      # Groq embedding (future)
│   │   ├── EmbeddingStorageService.cs   # Cosmos DB for vectors
│   │   ├── ProfileSharingService.cs     # Sharing logic (simplified)
│   │   ├── QrCodeService.cs             # QR generation
│   │   └── VectorSearchService.cs       # Embedding-based search (future)
│   ├── Repositories/
│   │   └── CosmosDbRepository.cs        # Generic Cosmos operations
│   └── EntityMatching.Infrastructure.csproj
│
├── EntityMatching.Functions/            # Azure Functions API
│   ├── ProfileFunctions.cs              # Profile CRUD endpoints
│   ├── ConversationFunctions.cs         # Conversation endpoints
│   ├── EmbeddingFunctions.cs            # Embedding generation
│   ├── SharingFunctions.cs              # Share/import endpoints
│   ├── SearchFunctions.cs               # Vector search (future)
│   ├── Common/
│   │   ├── BaseApiFunction.cs           # CORS, auth, error handling
│   │   ├── CorsMiddleware.cs
│   │   └── AuthorizationMiddleware.cs
│   ├── Program.cs                       # DI configuration
│   ├── host.json
│   ├── local.settings.json
│   └── EntityMatching.Functions.csproj
│
├── EntityMatching.Tests/                # Unit and integration tests
│   ├── Services/
│   │   ├── ProfileServiceTests.cs
│   │   ├── ConversationServiceTests.cs
│   │   └── EmbeddingTests.cs
│   ├── Integration/
│   │   ├── ProfileApiTests.cs
│   │   └── CosmosDbTests.cs
│   └── EntityMatching.Tests.csproj
│
└── EntityMatchingAPI.sln
```

### 5.2 Naming Conventions

**Namespace Root**: `EntityMatching`
- Core: `EntityMatching.Core.Models`
- Services: `EntityMatching.Infrastructure.Services`
- Functions: `EntityMatching.Functions`

**Database**: `EntityMatchingDB`

**Cosmos Containers**:
- `profiles` (consistent with Date Night)
- `conversations` (renamed from conversationContexts)
- `embeddings` (renamed from profile-embeddings)
- `shares` (consistent)
- `users` (lightweight identity)

**API Route Prefix**: `/api/v1`

### 5.3 Project Dependencies

```xml
<!-- EntityMatching.Core.csproj -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />

<!-- EntityMatching.Infrastructure.csproj -->
<ProjectReference Include="..\EntityMatching.Core\EntityMatching.Core.csproj" />
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.35.0" />

<!-- EntityMatching.Functions.csproj -->
<ProjectReference Include="..\EntityMatching.Core\EntityMatching.Core.csproj" />
<ProjectReference Include="..\EntityMatching.Infrastructure\EntityMatching.Infrastructure.csproj" />
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.20.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.1.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="2.15.2" />
```

---

## 6. STEP-BY-STEP MIGRATION PLAN

### Phase 1: Foundation Setup (Week 1)

**Milestone 1.1: Create New Solution Structure**
- [ ] Create `EntityMatchingAPI` solution
- [ ] Setup 3 projects: Core, Infrastructure, Functions
- [ ] Configure NuGet packages
- [ ] Setup Git repository

**Milestone 1.2: Copy Core Models**
- [ ] Copy `UserProfile.cs` → `Profile.cs` (refactor)
- [ ] Copy all preference models (11 files)
- [ ] Copy `EntityEmbedding.cs`, `SummaryMetadata.cs`
- [ ] Copy `ConversationContext.cs`, related models
- [ ] Copy `JsonHelper.cs`
- [ ] Remove Date Night-specific properties

**Milestone 1.3: Setup Cosmos DB**
- [ ] Create `EntityMatchingDB` database in Azure
- [ ] Configure connection strings
- [ ] Update container partition keys
- [ ] Test connection from local development

**Deliverable**: Compilable solution with clean domain models

---

### Phase 2: Service Layer (Week 2)

**Milestone 2.1: Implement Core Services**
- [ ] Copy & refactor `EntityService.cs`
  - Remove `DateNightUser` coupling
  - Generic `userId` parameter
- [ ] Copy `ConversationService.cs` (no changes needed)
- [ ] Copy `ProfileSummaryService.cs` (no changes needed)
- [ ] Copy `EmbeddingStorageService.cs` (update container names)

**Milestone 2.2: Simplify Profile Sharing**
- [ ] Copy `ProfileSharingService.cs`
- [ ] Remove subscription tier logic
- [ ] Implement generic quota system
- [ ] External authorization checks

**Milestone 2.3: Unit Tests**
- [ ] EntityService tests
- [ ] ConversationService tests
- [ ] EmbeddingSummaryService tests
- [ ] Mock Cosmos DB for tests

**Deliverable**: Working service layer with 80%+ test coverage

---

### Phase 3: Azure Functions API (Week 3)

**Milestone 3.1: Setup Functions Infrastructure**
- [ ] Create `Program.cs` with DI configuration
- [ ] Setup middleware: CORS, Auth, Exception handling
- [ ] Create `BaseApiFunction` base class
- [ ] Configure Application Insights

**Milestone 3.2: Implement Profile Endpoints**
- [ ] `GET /api/v1/entities` - List profiles
- [ ] `GET /api/v1/entities/{id}` - Get profile
- [ ] `POST /api/v1/entities` - Create profile
- [ ] `PUT /api/v1/entities/{id}` - Update profile
- [ ] `DELETE /api/v1/entities/{id}` - Delete profile

**Milestone 3.3: Implement Conversation Endpoints**
- [ ] `POST /api/v1/entities/{id}/conversation` - Send message
- [ ] `GET /api/v1/entities/{id}/conversation/history` - Get history
- [ ] `DELETE /api/v1/entities/{id}/conversation` - Clear history

**Milestone 3.4: Integration Tests**
- [ ] End-to-end API tests
- [ ] Authentication flow tests (mock)
- [ ] Error handling tests

**Deliverable**: Functional API with profile and conversation endpoints

---

### Phase 4: Authentication & Authorization (Week 4)

**Milestone 4.1: Setup Entra ID**
- [ ] Create new app registration in Entra ID tenant
- [ ] Configure API scopes: `Profile.Read`, `Profile.Write`, `Profile.Share`
- [ ] Update Date Night client registration with new scopes
- [ ] Test OAuth flow

**Milestone 4.2: Implement Authorization**
- [ ] JWT validation middleware
- [ ] User identity extraction
- [ ] Ownership verification (users can only access their profiles)
- [ ] Admin role support (optional)

**Milestone 4.3: Security Testing**
- [ ] Test unauthorized access (401)
- [ ] Test forbidden access (403)
- [ ] Test token expiration
- [ ] Penetration testing

**Deliverable**: Secure API with proper authentication

**Go/No-Go Decision Point**: End of Week 4
- **Go Criteria**: API functional, auth working, all tests passing
- **No-Go Criteria**: Major architectural issues, auth unsolvable, performance problems

---

### Phase 5: Manual Migration & Cutover (Week 5)

**Milestone 5.1: Update Date Night Client Services** (Day 1-2)
- [ ] Create `ProfileApiClient` in Date Night.Client
- [ ] Update `EntityService.cs` to call ProfileAPI
- [ ] Update `ConversationService.cs` to call ProfileAPI
- [ ] Update token acquisition to include new scopes

**Milestone 5.2: Integration Testing** (Day 3)
- [ ] Test profile CRUD in Date Night UI (dev environment)
- [ ] Test conversation in profile editor
- [ ] Test profile sharing flow
- [ ] Performance testing
- [ ] Error handling verification

**Milestone 5.3: Manual Profile Migration** (Day 4 morning - 1 hour)
- [ ] Choose migration method (copy-paste, script, or export/import)
- [ ] Execute migration
- [ ] Verify profile count matches
- [ ] Test profile access in new API
- [ ] Generate migration report

**Milestone 5.4: Production Deployment** (Day 4 afternoon)
- [ ] Deploy Profile API to Azure
- [ ] Deploy updated Date Night Functions
- [ ] Deploy updated Date Night Client
- [ ] Smoke test critical flows

**Milestone 5.5: Monitoring & Verification** (Day 5)
- [ ] Monitor API performance (Application Insights)
- [ ] Monitor error rates
- [ ] Verify all profiles accessible
- [ ] User acceptance testing

**Deliverable**: Date Night fully integrated with Profile API in production

---

### Phase 6: Embedding Production (Week 6 - Optional)

**Milestone 6.1: Choose Embedding Provider**
- [ ] Evaluate OpenAI vs Groq vs Custom
- [ ] Implement `IEmbeddingService` for chosen provider
- [ ] Test embedding generation quality
- [ ] Benchmark performance and cost

**Milestone 6.2: Batch Embedding Generation**
- [ ] Create Azure Function: `GenerateEmbeddingsTimer`
- [ ] Process profiles without embeddings
- [ ] Update existing embeddings when profiles change
- [ ] Monitor embedding generation queue

**Milestone 6.3: Vector Search Implementation**
- [ ] Implement similarity search algorithm
- [ ] Create `POST /api/v1/entities/search` endpoint
- [ ] Test search quality with real profiles
- [ ] Optimize search performance

**Deliverable**: Production-ready embedding and search system

---

### Phase 7: Marketplace Foundation (Future)

**Milestone 7.1: Profile Visibility & Privacy**
- [ ] Add `ProfileVisibility` enum (Private, Friends, Public, Marketplace)
- [ ] Implement visibility filtering in search
- [ ] Privacy controls UI (for clients)

**Milestone 7.2: Profile Discovery**
- [ ] Public profile directory endpoint
- [ ] Search by location, preferences
- [ ] Match percentage calculation

**Milestone 7.3: Zero-Trust Architecture**
- [ ] Encryption at rest for sensitive fields
- [ ] Encrypted profile shares
- [ ] Audit logging for profile access

**Deliverable**: Marketplace-ready profile discovery

---

## 7. RISK ASSESSMENT & MITIGATION

### 7.1 Technical Risks

**Risk 1: Authentication Token Compatibility**
- **Severity**: Medium
- **Probability**: Low (using shared tenant)
- **Impact**: Users can't access profiles after migration
- **Mitigation**:
  - Reuse existing Entra ID tenant
  - Thorough testing in development
  - Gradual rollout (optional with few users)
  - Keep Date Night's direct DB access as emergency fallback

**Risk 2: Breaking Date Night Functionality**
- **Severity**: High
- **Probability**: Low (comprehensive testing)
- **Impact**: Date Night stops working
- **Mitigation**:
  - Comprehensive integration tests
  - Test in development before production
  - Manual verification of all features
  - Rollback plan ready

**Risk 3: Performance Degradation**
- **Severity**: Medium
- **Probability**: Low
- **Impact**: Slower profile operations
- **Mitigation**:
  - Load testing before migration
  - Monitor API latency in production
  - Optimize Cosmos DB queries
  - Caching layer (Redis) if needed

**Risk 4: Manual Migration Errors**
- **Severity**: Low
- **Probability**: Low (few profiles)
- **Impact**: Missing or corrupted profiles
- **Mitigation**:
  - Backup before migration
  - Verification script to check profile count
  - Manual testing of each profile
  - Easy to re-run if issues found

**Risk 5: Embedding Generation Failures**
- **Severity**: Low
- **Probability**: Medium
- **Impact**: No semantic search for some profiles
- **Mitigation**:
  - Retry logic with exponential backoff
  - Dead letter queue for failed embeddings
  - Manual retry capability
  - Graceful degradation (return profiles without scores)

### 7.2 Problematic Dependencies

**Dependency 1: DateNightUser in EntityService**
- **Problem**: `EntityService.GetProfileAsync(id, userId)` validates ownership via `DateNightUser.OwnProfileId`
- **Solution**: Change to generic ownership check
  ```csharp
  // Before:
  var user = await _userService.GetUserAsync(userId);
  return profile.OwnedByUserId == user.Id;

  // After:
  return profile.OwnedByUserId == userId; // Direct string comparison
  ```

**Dependency 2: Subscription Tiers in ProfileSharingService**
- **Problem**: Rate limiting checks `user.SubscriptionTier`
- **Solution**: Remove subscription-based rate limiting for now, add generic quota system later
  ```csharp
  // Before:
  if (!user.CanShareProfile(profileType)) throw new InvalidOperationException();

  // After (MVP):
  // Simple fixed limit for all users
  var sharesThisMonth = await _sharingService.GetShareCountThisMonthAsync(userId);
  if (sharesThisMonth >= 10) throw new QuotaExceededException();
  ```

**Dependency 3: Groq API Key Configuration**
- **Problem**: Shared configuration between Date Night and Profile API
- **Solution**: Each API has its own config, but keys can be same value
  ```
  DateNight: ApiKeys__Groq=gsk_...
  ProfileAPI: ApiKeys__Groq=gsk_...
  # Both can use same key, but separately configured
  ```

### 7.3 Complexity Estimates

**Component Extraction Complexity** (Revised for Manual Migration):

| Component | Complexity | Effort (Days) | Risk |
|-----------|-----------|---------------|------|
| Core Models | Low | 2 | Low |
| EntityService | Medium | 3 | Low |
| ConversationService | Low | 1 | Low |
| EmbeddingServices | Low | 2 | Low |
| ProfileSharingService | Medium | 4 | Medium |
| Azure Functions API | Medium | 5 | Medium |
| Authentication Setup | Medium | 3 | Medium |
| ~~Data Migration~~ | ~~High~~ | ~~0.5~~ | ~~Low~~ |
| Date Night Integration | Medium | 3 | Medium |
| Testing & QA | Medium | 7 | Low |
| **TOTAL MVP** | - | **30 days** | - |

**Timeline Estimate**:
- **MVP (Read-only API)**: 3-4 weeks (1 developer)
- **Full Migration**: 5-6 weeks (1 developer)
- **Production Ready with Embeddings**: 6-7 weeks

### 7.4 Rollback Plan

**Pre-Migration State**:
- Date Night writes directly to `DateNightPlannerDB.profiles`
- No Profile API deployed

**Post-Migration Rollback**:

1. **If Profile API fails in production**:
   - Update Date Night configuration to use direct DB access
   - Deploy Date Night with old EntityService
   - Profile API can stay deployed but unused
   - **Recovery Time**: 30 minutes

2. **If data corruption detected**:
   - Restore `EntityMatchingDB` from backup
   - Re-run manual migration
   - **Recovery Time**: 2 hours

3. **If authentication fails**:
   - Temporarily disable auth in Profile API (dev/test only)
   - Investigate Entra ID configuration
   - Fix and redeploy
   - **Recovery Time**: 1-4 hours

**Recovery Time Objective (RTO)**: < 1 hour
**Recovery Point Objective (RPO)**: 0 (manual migration can be re-run)

---

## 8. EFFORT ESTIMATES

### 8.1 Development Timeline

**Minimum Viable Product (MVP)** - Working Profile API:
- **Week 1**: Foundation setup + core models
- **Week 2**: Service layer implementation
- **Week 3**: Azure Functions API
- **Week 4**: Authentication & authorization
- **Total**: 4 weeks, 1 developer

**Full Production Release**:
- **Week 5**: Date Night integration + manual migration + deployment
- **Total**: 5 weeks, 1 developer

**With Embeddings**:
- **Week 6**: Embedding production implementation
- **Total**: 6 weeks, 1 developer

**Future Enhancements** (Post-MVP):
- **Week 7-8**: Marketplace features (visibility, discovery)
- **Week 9-10**: Vector search optimization
- **Week 11-12**: Multi-tenant support for other verticals

### 8.2 Resource Requirements

**Development**:
- 1 Senior Full-Stack Developer (C#, Azure, Cosmos DB)
- 1 DevOps Engineer (part-time, Azure deployment - 1-2 days total)

**Infrastructure** (Initial):
- Azure Cosmos DB (Serverless): ~$50-100/month
- Azure Functions (Consumption Plan): ~$20-50/month
- Application Insights: ~$10/month
- **Total**: ~$80-160/month for MVP

**Third-Party Services**:
- Groq API: ~$50-200/month (depends on embedding usage)
- OpenAI (if used for embeddings): ~$100-500/month

### 8.3 Testing Estimates

**Unit Tests**: 3 days (parallel with development)
**Integration Tests**: 2 days
**Performance Testing**: 1 day
**Security Testing**: 1 day
**Manual Testing**: 2 days
**Total Testing**: **9 days** (included in phase timelines)

### 8.4 Documentation

**Technical Documentation**: 2 days
- API reference (OpenAPI/Swagger)
- Architecture diagrams
- Deployment guide
- Database schema

**Integration Guide for Clients**: 1 day
- Authentication flow
- Sample code (C#, JavaScript)
- Error handling
- Rate limits

**Total Documentation**: **3 days** (can be done in parallel)

---

## 9. SUCCESS METRICS

### 9.1 Technical Metrics

**API Performance**:
- Profile GET response time: < 200ms (p95)
- Profile CREATE response time: < 500ms (p95)
- Conversation processing: < 2s (p95)
- API availability: > 99.5%

**Data Quality**:
- Migration success rate: 100% (manual verification)
- Data validation pass rate: 100%
- Embedding generation success: > 95%

**Cost**:
- Cosmos DB consumption: < $200/month
- API hosting cost: < $100/month
- Total infrastructure: < $500/month

### 9.2 Business Metrics

**Adoption**:
- Date Night successfully using Profile API: 100% of requests
- Zero data loss incidents: 0
- User-reported issues: < 5 in first month

**Marketplace Readiness**:
- Profile model is domain-agnostic: 100%
- Embedding infrastructure ready: 100%
- Vector search implemented: Week 6+

### 9.3 Development Metrics

**Code Quality**:
- Unit test coverage: > 80%
- Integration test coverage: > 60%
- Zero critical security vulnerabilities
- Code review pass rate: 100%

**Documentation**:
- API documentation completeness: 100%
- Integration guide accuracy: 100%
- Architecture diagrams: Complete

---

## 10. CRITICAL FILES FOR EXTRACTION

### Top 10 Most Critical Files

1. **UserProfile.cs** (397 lines)
   - **Path**: `DateNightPlanner.Core\Models\UserProfile.cs`
   - **Action**: Copy → refactor → rename to `Profile.cs`
   - **Priority**: Highest

2. **EntityService.cs** (659 lines)
   - **Path**: `DateNightPlanner.Infrastructure\Services\EntityService.cs`
   - **Action**: Copy → decouple from DateNightUser
   - **Priority**: Highest

3. **ConversationService.cs** (353 lines)
   - **Path**: `DateNightPlanner.Infrastructure\Services\ConversationService.cs`
   - **Action**: Copy as-is (already domain-agnostic)
   - **Priority**: High

4. **ProfileSummaryService.cs** (450 lines)
   - **Path**: `DateNightPlanner.Infrastructure\Services\ProfileSummaryService.cs`
   - **Action**: Copy as-is
   - **Priority**: High

5. **ProfileSharingService.cs** (1008 lines)
   - **Path**: `DateNightPlanner.Infrastructure\Services\ProfileSharingService.cs`
   - **Action**: Copy → refactor (remove subscription logic)
   - **Priority**: High

6. **EntityEmbedding.cs** (159 lines)
   - **Path**: `DateNightPlanner.Core\Models\EntityEmbedding.cs`
   - **Action**: Copy as-is
   - **Priority**: Medium

7. **EmbeddingStorageService.cs** (196 lines)
   - **Path**: `DateNightPlanner.Infrastructure\Services\EmbeddingStorageService.cs`
   - **Action**: Copy → update container names
   - **Priority**: Medium

8. **JsonHelper.cs**
   - **Path**: `DateNightPlanner.Core\Utilities\JsonHelper.cs`
   - **Action**: Copy as-is (critical for compatibility)
   - **Priority**: High

9. **ProfilesFunctions.cs** (23KB)
   - **Path**: `DateNightPlanner.Functions\ProfilesFunctions.cs`
   - **Action**: Use as reference for new ProfileFunctions.cs
   - **Priority**: Medium

10. **Program.cs** (440 lines)
    - **Path**: `DateNightPlanner.Functions\Program.cs`
    - **Action**: Use as reference for DI configuration
    - **Priority**: Medium

### All Preference Models (Copy as-is)

11-21. Preference category models (11 files):
- `EntertainmentPreferences.cs`
- `AdventurePreferences.cs`
- `LearningPreferences.cs`
- `SensoryPreferences.cs`
- `SocialPreferences.cs`
- `StylePreferences.cs`
- `NaturePreferences.cs`
- `GiftPreferences.cs`
- `AccessibilityNeeds.cs`
- `DietaryRestrictions.cs`
- `ActivityPreferences.cs`

---

## 11. CONCLUSION AND RECOMMENDATIONS

### 11.1 Recommended Approach

**Strategy**: **Manual Migration with Clean Cutover**

**Rationale**:
1. With few profiles, manual migration is faster and simpler than automated
2. Can build Profile API completely before touching Date Night
3. Single deployment reduces complexity and risk
4. Easier to test and verify with small dataset
5. Saves 2-3 weeks of development time

**Key Decisions**:
1. ✅ **Separate Database from Start** - Clean separation, no schema coupling
2. ✅ **Shared Entra ID Tenant** - Simplifies authentication, seamless SSO
3. ✅ **Duplicate Core Models** - Deployment independence
4. ✅ **Manual Migration** - Fast, simple, low risk with few profiles
5. ✅ **Clean Cutover** - Build complete, test thoroughly, deploy once

### 11.2 Timeline Recommendation

**Option A: Comfortable Timeline (6 weeks)** - RECOMMENDED
- Week 1-4: Build Profile API completely
- Week 5: Integration + manual migration + deployment
- Week 6: Embedding implementation + monitoring
- **Benefits**: High quality, buffer for issues, production-ready with embeddings

**Option B: Aggressive Timeline (5 weeks)**
- Week 1-4: Build Profile API
- Week 5: Integration + migration + deployment (no embeddings yet)
- **Benefits**: Faster to production, embeddings added later
- **Risk**: Less buffer time

### 11.3 Critical Success Factors

1. **Thorough Testing**: Test Date Night + Profile API integration in development before production
2. **Manual Verification**: With few profiles, manually test each one after migration
3. **Performance Monitoring**: Watch API latency and error rates closely in first week
4. **Rollback Preparedness**: Keep Date Night's direct DB access code available for emergency
5. **Documentation**: Clear API docs for future clients beyond Date Night

### 11.4 Go/No-Go Decision Point

**When**: End of Week 4 (after authentication implementation)

**Go Criteria**:
- ✅ All API endpoints functional
- ✅ Authentication working with Entra ID
- ✅ All unit and integration tests passing
- ✅ Performance acceptable (< 500ms response times)
- ✅ No critical security vulnerabilities

**No-Go Criteria** (requires delay):
- ❌ Major architectural issues discovered
- ❌ Authentication unsolvable with current approach
- ❌ Performance unacceptable (> 2s response times)
- ❌ Critical bugs in core functionality

### 11.5 Future Opportunities

**After MVP Stabilization (Week 7+)**:
1. **Marketplace Launch** - Public profile discovery with embedding-based matching
2. **Multi-Vertical Support** - Job matching, travel companions, retail personalization
3. **Third-Party Integration** - API for external applications
4. **Advanced Search** - Hybrid search (keyword + semantic), faceted filtering
5. **Profile Analytics** - Insights dashboard for profile owners
6. **Profile Monetization** - Users get paid when businesses match their profile

### 11.6 Final Timeline Summary

| Milestone | Week | Deliverable |
|-----------|------|-------------|
| Foundation Setup | 1 | Compilable solution with clean models |
| Service Layer | 2 | Working services with 80%+ test coverage |
| Azure Functions API | 3 | Functional API endpoints with tests |
| Authentication | 4 | Secure API with Entra ID auth |
| **Go/No-Go Decision** | **End of 4** | **Proceed or pause** |
| Integration & Migration | 5 | Date Night using Profile API in production |
| Embeddings (Optional) | 6 | Vector search ready |
| **PRODUCTION READY** | **5-6 weeks** | **Complete EntityMatchingAPI** |

---

## Appendix A: Configuration Templates

### Profile API - local.settings.json

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
    "CosmosDb__SharesContainerId": "shares",

    "ApiKeys__Groq": "gsk_your-groq-api-key",
    "ApiKeys__OpenAI": "sk-your-openai-api-key-optional",

    "EntraId__TenantId": "your-tenant-id",
    "EntraId__ClientId": "your-profile-api-client-id",
    "EntraId__Instance": "https://login.microsoftonline.com/",

    "APPLICATIONINSIGHTS_CONNECTION_STRING": "your-app-insights-connection",

    "CORS_ALLOWED_ORIGINS": "https://localhost:5001,https://datenightplanner.com"
  }
}
```

### Date Night - Updated Configuration (for Profile API integration)

```json
{
  "ProfileApi__BaseUrl": "https://ai.bystorm.com",
  "ProfileApi__Scopes": "api://profile-api-client-id/Profile.Read api://profile-api-client-id/Profile.Write"
}
```

---

## Appendix B: Migration Checklist

### Pre-Migration Checklist

- [ ] Profile API deployed to Azure
- [ ] Profile API tested in development
- [ ] All API endpoints verified working
- [ ] Authentication tested with real Entra ID tokens
- [ ] Cosmos DB `EntityMatchingDB` created
- [ ] Backup of Date Night profiles created
- [ ] Date Night client code updated to call Profile API
- [ ] Date Night tested in development environment

### Migration Day Checklist

**Morning (Migration)**:
- [ ] Verify Profile API is healthy (health check endpoint)
- [ ] Run migration script or manual copy
- [ ] Verify profile count matches (source vs target)
- [ ] Test sample profiles in Profile API
- [ ] Generate migration report

**Afternoon (Deployment)**:
- [ ] Deploy Date Night Functions with ProfileApiClient
- [ ] Deploy Date Night Blazor client
- [ ] Smoke test: Create profile
- [ ] Smoke test: Update profile
- [ ] Smoke test: Conversation
- [ ] Smoke test: Profile sharing

**Evening (Monitoring)**:
- [ ] Monitor Application Insights for errors
- [ ] Check API response times
- [ ] Verify no 401/403 auth errors
- [ ] Test from real user accounts
- [ ] Verify all existing profiles accessible

### Post-Migration Checklist (Week 1)

- [ ] Daily monitoring of API performance
- [ ] Review error logs
- [ ] User feedback collection
- [ ] Performance optimization if needed
- [ ] Document lessons learned

---

**END OF EXTRACTION PLAN**

This plan provides a comprehensive, realistic roadmap for extracting profile and AI matching logic into a reusable API. The manual migration approach reduces complexity from 8 weeks to 5-6 weeks while maintaining high quality and low risk.

**Next Steps**: Review this plan, ask any questions, then begin Phase 1 (Foundation Setup) when ready.
