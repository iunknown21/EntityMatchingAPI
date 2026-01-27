# EntityMatching SDK (C#/.NET)

Privacy-first client SDK for [EntityMatchingAPI](https://api.bystorm.com) - Zero PII storage with client-side embedding generation.

## Features

- **🔒 Privacy-First**: Generate embeddings locally, upload only vectors (never text)
- **🎯 Semantic Search**: Find profiles using natural language queries
- **🔍 Attribute Filtering**: Combine semantic search with structured filters
- **💬 Conversational Profiling**: Build profiles through AI conversations
- **📊 Full .NET 8 Support**: Modern C# with nullable reference types

## Installation

```bash
dotnet add package EntityMatching.SDK
```

## Quick Start

### Initialize the Client

```csharp
using EntityMatching.SDK;

var client = new EntityMatchingClient(new EntityMatchingClientOptions
{
    ApiKey = "your-api-key",              // From https://api.bystorm.com
    OpenAIKey = "your-openai-key",        // Optional, for client-side embedding
    BaseUrl = "https://api.bystorm.com"   // Optional, defaults to this
});
```

### Privacy-First Resume Upload

The killer feature: upload resumes without sending text to the server.

```csharp
var resumeText = @"
    Senior Software Engineer with 10 years experience in Python and AWS.
    Built machine learning pipelines processing 100M+ events/day.
";

// Generate embedding locally, upload only the vector
await client.UploadResumeAsync(profileId, resumeText);

// ✅ Resume text NEVER left your device!
// ✅ Only 1536 numbers were sent to the server
// ✅ Even if hacked, attackers get meaningless data
```

### Search Profiles

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "Senior Python engineer, AWS experience",
    Limit = 10,
    MinSimilarity = 0.7
});

foreach (var match in results.Matches)
{
    Console.WriteLine($"Profile {match.ProfileId}: {match.SimilarityScore * 100}% match");
}
```

### Advanced Search with Filters

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "loves hiking and outdoor adventures",
    AttributeFilters = new AttributeFilters
    {
        LogicalOperator = LogicalOperator.And,
        Filters = new List<AttributeFilter>
        {
            new() { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue },
            new() { FieldPath = "naturePreferences.petTypes", Operator = FilterOperator.Contains, Value = "Dog" },
            new() { FieldPath = "adventurePreferences.riskTolerance", Operator = FilterOperator.GreaterThan, Value = 6 }
        }
    },
    Limit = 5,
    MinSimilarity = 0.6
});
```

### Conversational Profiling

```csharp
// Send a message to build a profile
var response = await client.Conversations.SendMessageAsync(profileId, new ConversationMessage
{
    UserId = "user-123",
    Message = "They love hiking on weekends and enjoy trying new restaurants"
});

Console.WriteLine($"AI Response: {response.AIResponse}");
Console.WriteLine($"Extracted Insights: {response.NewInsights.Count}");
```

## API Reference

### EntityMatchingClient

Main client class for interacting with EntityMatchingAPI.

#### Constructor Options

```csharp
public class EntityMatchingClientOptions
{
    public required string ApiKey { get; set; }        // Required: API key from portal
    public string BaseUrl { get; set; }                // Optional: API base URL
    public string? OpenAIKey { get; set; }             // Optional: For client-side embedding
}
```

#### Properties

- `Profiles` - Profile CRUD operations
- `Embeddings` - Privacy-first vector upload
- `Conversations` - Conversational profiling
- `Search` - Semantic search with filters

#### Methods

##### `UploadResumeAsync(Guid profileId, string resumeText): Task`

Upload a resume with privacy-first approach. Generates embedding locally and uploads only the vector.

**Parameters:**
- `profileId` (Guid) - Profile ID to associate resume with
- `resumeText` (string) - Resume text (stays local, never sent to server)

**Example:**
```csharp
await client.UploadResumeAsync(profileId, resumeText);
```

##### `GenerateEmbeddingAsync(string text): Task<float[]>`

Generate embedding for any text (not just resumes).

**Returns:** 1536-dimensional embedding vector

### Profiles Endpoint

```csharp
// List all profiles for a user
var profiles = await client.Profiles.ListAsync("user-123");

// Get single profile
var profile = await client.Profiles.GetAsync(profileId);

// Create profile
var newProfile = await client.Profiles.CreateAsync(new Profile
{
    OwnedByUserId = "user-123",
    Name = "John Doe",
    Bio = "Software Engineer",
    IsSearchable = true
});

// Update profile
await client.Profiles.UpdateAsync(profileId, updatedProfile);

// Delete profile
await client.Profiles.DeleteAsync(profileId);
```

### Embeddings Endpoint

```csharp
// Upload pre-computed embedding
await client.Embeddings.UploadAsync(profileId, new UploadEmbeddingRequest
{
    Embedding = new float[] { 0.123f, -0.456f, /* ... 1536 numbers */ },
    EmbeddingModel = "text-embedding-3-small"
});
```

### Conversations Endpoint

```csharp
// Send message
var response = await client.Conversations.SendMessageAsync(profileId, new ConversationMessage
{
    UserId = "user-123",
    Message = "They love hiking and outdoor activities"
});

// Get conversation history
var history = await client.Conversations.GetHistoryAsync(profileId);

// Delete conversation
await client.Conversations.DeleteAsync(profileId);
```

### Search Endpoint

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "Senior Python engineer",
    AttributeFilters = new AttributeFilters { /* ... */ },
    Limit = 10,
    MinSimilarity = 0.7,
    EnforcePrivacy = true
});
```

## Privacy Guarantees

### What Gets Stored?

**Traditional Job Boards:**
- ❌ Full resume text (can be stolen)
- ❌ Personal information (name, email, address)
- ❌ GDPR compliance burden

**EntityMatching:**
- ✅ Only 1536-dimensional vector
- ✅ No personal information in vector
- ✅ Impossible to reconstruct original text
- ✅ Minimal GDPR requirements

### Cost Savings

Storing vectors instead of text:
- **87% smaller storage** (1536 floats vs. full documents)
- **92% lower costs** ($208/month vs. $2,623/month for 1000 profiles)

## Examples

See the `Examples/` directory for complete examples:

- `UploadResumeExample.cs` - Privacy-first resume upload
- `SearchProfilesExample.cs` - Semantic search with filters

## Requirements

- .NET 8.0 or later
- Azure.AI.OpenAI 1.0.0-beta.12 or later (for client-side embedding)

## License

MIT

## Support

- **API Documentation**: https://api.bystorm.com/docs
- **GitHub Issues**: https://github.com/iunknown21/EntityMatchingAPI/issues
- **Email**: admin@bystorm.com
