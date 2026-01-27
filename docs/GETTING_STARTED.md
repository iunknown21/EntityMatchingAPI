# Getting Started with EntityMatching API

This guide will help you start using the EntityMatching API in under 10 minutes.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Choose Your SDK](#choose-your-sdk)
4. [Your First Profile](#your-first-profile)
5. [Privacy-First Resume Upload](#privacy-first-resume-upload)
6. [Semantic Search](#semantic-search)
7. [Next Steps](#next-steps)

---

## Prerequisites

- **API Key**: Sign up at https://EntityMatching-apim.developer.azure-api.net
  - Free Tier: 5,000 requests/month, 100 requests/minute
  - Premium Tier: 100,000 requests/month, 1,000 requests/minute
  - Enterprise Tier: Unlimited requests, 10,000 requests/minute

- **OpenAI API Key** (optional, for client-side embeddings):
  - Get from: https://platform.openai.com/api-keys
  - Only needed if you want to generate embeddings client-side

---

## Quick Start

### Option 1: JavaScript/TypeScript

```bash
# Install the SDK
npm install @EntityMatching/sdk

# Create a simple script
touch quick-start.js
```

```javascript
// quick-start.js
const { EntityMatchingClient } = require('@EntityMatching/sdk');

const client = new EntityMatchingClient({
  apiKey: 'YOUR_API_KEY_HERE',
  baseUrl: 'https://EntityMatching-apim.azure-api.net/v1'
});

async function main() {
  // Create a profile
  const profile = await client.profiles.create({
    ownedByUserId: 'user-123',
    name: 'John Doe',
    bio: 'Software engineer passionate about AI',
    isSearchable: true
  });

  console.log('✅ Profile created:', profile.id);
}

main().catch(console.error);
```

Run it:
```bash
node quick-start.js
```

### Option 2: C#/.NET

```bash
# Create a new console app
dotnet new console -n EntityMatchingQuickStart
cd EntityMatchingQuickStart

# Install the SDK
dotnet add package EntityMatching.SDK

# Edit Program.cs
```

```csharp
using EntityMatching.SDK;
using EntityMatching.Shared.Models;

var client = new EntityMatchingClient(new EntityMatchingClientOptions
{
    ApiKey = "YOUR_API_KEY_HERE",
    BaseUrl = "https://EntityMatching-apim.azure-api.net/v1"
});

// Create a profile
var profile = await client.Profiles.CreateAsync(new Profile
{
    OwnedByUserId = "user-123",
    Name = "John Doe",
    Bio = "Software engineer passionate about AI",
    IsSearchable = true
});

Console.WriteLine($"✅ Profile created: {profile.Id}");
```

Run it:
```bash
dotnet run
```

### Option 3: cURL (No SDK)

```bash
# Create a profile
curl -X POST https://EntityMatching-apim.azure-api.net/v1/v1/profiles \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: YOUR_API_KEY" \
  -d '{
    "ownedByUserId": "user-123",
    "name": "John Doe",
    "bio": "Software engineer passionate about AI",
    "isSearchable": true
  }'
```

---

## Choose Your SDK

### JavaScript/TypeScript SDK

**Best for:**
- Web applications (React, Vue, Angular)
- Node.js backends
- Browser-based client-side embedding generation

**Installation:**
```bash
npm install @EntityMatching/sdk
```

**Documentation:** [JavaScript SDK Guide](./SDK_JAVASCRIPT.md)

### C#/.NET SDK

**Best for:**
- .NET applications (ASP.NET Core, Blazor, MAUI)
- Enterprise applications
- Windows desktop apps

**Installation:**
```bash
dotnet add package EntityMatching.SDK
```

**Documentation:** [C# SDK Guide](./SDK_CSHARP.md)

---

## Your First Profile

### 1. Create a Basic Profile

**JavaScript:**
```javascript
const profile = await client.profiles.create({
  ownedByUserId: 'user-123',
  name: 'Jane Smith',
  bio: 'Product manager who loves hiking',
  isSearchable: true,
  createdAt: new Date().toISOString(),
  lastModified: new Date().toISOString()
});
```

**C#:**
```csharp
var profile = await client.Profiles.CreateAsync(new Profile
{
    OwnedByUserId = "user-123",
    Name = "Jane Smith",
    Bio = "Product manager who loves hiking",
    IsSearchable = true,
    CreatedAt = DateTime.UtcNow,
    LastModified = DateTime.UtcNow
});
```

### 2. Add Preferences (Optional)

```javascript
const updatedProfile = await client.profiles.update(profile.id, {
  preferences: {
    entertainment: {
      movieGenres: ['Sci-Fi', 'Drama'],
      musicGenres: ['Rock', 'Jazz']
    },
    adventure: {
      riskTolerance: 7,
      noveltySeeking: 8
    }
  }
});
```

### 3. Retrieve Your Profiles

```javascript
const myProfiles = await client.profiles.list('user-123');
console.log(`You have ${myProfiles.length} profiles`);
```

---

## Privacy-First Resume Upload

**Why is this special?** Your resume text NEVER leaves your device. Only a mathematical representation (vector) is uploaded.

### JavaScript (Browser)

```javascript
// User enters resume text in a textarea
const resumeText = document.getElementById('resume').value;

// Generate embedding locally using OpenAI
// (Resume text is sent to OpenAI, NOT to EntityMatching servers)
const embedding = await client.generateEmbedding(resumeText);

// Upload ONLY the vector (not the text)
await client.embeddings.upload(profileId, {
  embedding: embedding,
  embeddingModel: 'text-embedding-3-small'
});

// ✅ Resume text stayed in browser
// ✅ Only numbers uploaded to server
// ✅ Even if server is hacked, attackers get meaningless vectors
```

### C#

```csharp
// User enters resume text
string resumeText = GetResumeTextFromUser();

// Generate embedding locally and upload vector only
// (This method handles both steps internally)
await client.UploadResumeAsync(profileId, resumeText);

// ✅ Resume text sent only to OpenAI for embedding
// ✅ Only vector uploaded to EntityMatching
// ✅ Zero PII stored on EntityMatching servers
```

**What gets uploaded:**
```json
{
  "embedding": [0.123, -0.456, 0.789, ..., 1536 numbers],
  "embeddingModel": "text-embedding-3-small"
}
```

**What NEVER gets uploaded:**
- Your resume text
- Name
- Email
- Phone number
- Address
- Any personal information

---

## Semantic Search

Search for profiles using natural language:

### JavaScript

```javascript
const results = await client.search.search({
  query: "Senior Python engineer with AWS experience and ML background",
  minSimilarity: 0.7,
  limit: 10,
  enforcePrivacy: true // Only returns profile IDs, no PII
});

console.log(`Found ${results.totalMatches} matches`);
results.matches.forEach(match => {
  console.log(`Profile ${match.profileId}: ${(match.similarityScore * 100).toFixed(1)}% match`);
});
```

### C#

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "Senior Python engineer with AWS experience and ML background",
    MinSimilarity = 0.7f,
    Limit = 10,
    EnforcePrivacy = true
});

Console.WriteLine($"Found {results.TotalMatches} matches");
foreach (var match in results.Matches)
{
    Console.WriteLine($"Profile {match.ProfileId}: {match.SimilarityScore * 100:F1}% match");
}
```

### Advanced Search with Filters

Combine semantic search with structured filters:

```javascript
const results = await client.search.search({
  query: "loves hiking and outdoor adventures",
  attributeFilters: {
    logicalOperator: 'AND',
    filters: [
      { fieldPath: 'preferences.adventure.riskTolerance', operator: 'GreaterThan', value: 6 },
      { fieldPath: 'preferences.nature.outdoorActivities', operator: 'Contains', value: 'Hiking' }
    ]
  },
  minSimilarity: 0.6,
  limit: 20
});
```

---

## Next Steps

### 1. Explore More Features

- **Conversational Profiling**: [Conversational API Guide](./API_CONVERSATIONAL.md)
- **Advanced Search**: [Search & Filtering Guide](./API_SEARCH.md)
- **Privacy Settings**: [Privacy Configuration Guide](./PRIVACY.md)

### 2. Try the Live Demo

Visit the interactive demo to see all features in action:
- **Demo URL**: https://privatematch-demo.azurestaticapps.net
- Features:
  - Privacy-first resume upload
  - Semantic profile search
  - Cost comparison calculator
  - Data breach simulation

### 3. Read the Architecture

Understand how the system works:
- [Architecture Documentation](./ARCHITECTURE.md)
- [Embedding Strategy](../EMBEDDING_ARCHITECTURE.md)
- [Database Schema](../DATABASE_SCHEMA.md)

### 4. Integration Examples

- [React Integration Example](./examples/react-integration.md)
- [Blazor Integration Example](./examples/blazor-integration.md)
- [Node.js Backend Example](./examples/nodejs-backend.md)

### 5. Production Checklist

Before going live:
- [ ] Upgrade to Premium or Enterprise tier
- [ ] Implement rate limiting on your side
- [ ] Add error handling and retries
- [ ] Set up monitoring and logging
- [ ] Review privacy settings
- [ ] Test with production-like data volume

---

## Support

- **Documentation**: https://github.com/iunknown21/EntityMatchingAPI/tree/master/docs
- **API Reference**: [CORE_PLATFORM_API.md](./CORE_PLATFORM_API.md)
- **Issues**: https://github.com/iunknown21/EntityMatchingAPI/issues
- **Email**: support@bystorm.com

---

## Pricing Tiers

| Tier | Monthly Requests | Rate Limit | Price | Approval |
|------|-----------------|------------|-------|----------|
| **Free** | 5,000 | 100/min | $0 | Auto |
| **Premium** | 100,000 | 1,000/min | $99/mo | Manual |
| **Enterprise** | Unlimited | 10,000/min | Custom | Manual |

**All tiers include:**
- Vector embedding storage
- Semantic search
- Attribute filtering
- Privacy-protected results
- 99.9%+ SLA (Premium+)

---

## FAQ

**Q: Do I need an OpenAI API key?**

A: Only if you want to generate embeddings client-side (privacy-first mode). For server-side generation, our API handles it.

**Q: Is my data secure?**

A: Yes. We store only vectors (mathematical representations), not your source text. Even in a data breach, attackers would only get meaningless numbers.

**Q: Can I use this for job matching?**

A: Absolutely! This API is domain-agnostic. Use it for:
- Job/talent matching
- Dating
- Travel companions
- Study groups
- Business networking
- Any matching use case

**Q: What's the search latency?**

A: Typically 50-200ms for semantic search with up to 10,000 profiles.

**Q: Can I filter by location?**

A: Yes! Add location to your profile and use attribute filters:
```javascript
attributeFilters: {
  filters: [
    { fieldPath: 'location.city', operator: 'Equals', value: 'San Francisco' }
  ]
}
```

---

🎉 **You're ready to start building!**

Begin with the [JavaScript SDK Guide](./SDK_JAVASCRIPT.md) or [C# SDK Guide](./SDK_CSHARP.md) for detailed examples.
