# C# / .NET SDK Guide

Complete guide to using the EntityMatching C#/.NET SDK.

## Table of Contents

1. [Installation](#installation)
2. [Configuration](#configuration)
3. [Profile Management](#profile-management)
4. [Privacy-First Resume Upload](#privacy-first-resume-upload)
5. [Conversational Profiling](#conversational-profiling)
6. [Semantic Search](#semantic-search)
7. [Advanced Search with Filters](#advanced-search-with-filters)
8. [Error Handling](#error-handling)
9. [Async/Await Patterns](#asyncawait-patterns)
10. [Examples](#examples)

---

## Installation

### Package Manager Console

```powershell
Install-Package EntityMatching.SDK
```

### .NET CLI

```bash
dotnet add package EntityMatching.SDK
```

### PackageReference

```xml
<PackageReference Include="EntityMatching.SDK" Version="1.0.0" />
```

**Requirements:**
- .NET 8.0 or later
- C# 10.0 or later

---

## Configuration

### Basic Setup

```csharp
using EntityMatching.SDK;

var client = new EntityMatchingClient(new EntityMatchingClientOptions
{
    ApiKey = "your-api-key-here",
    BaseUrl = "https://EntityMatching-apim.azure-api.net/v1"
});
```

### With OpenAI for Client-Side Embeddings

```csharp
var client = new EntityMatchingClient(new EntityMatchingClientOptions
{
    ApiKey = "your-EntityMatching-api-key",
    BaseUrl = "https://EntityMatching-apim.azure-api.net/v1",
    OpenAIKey = "your-openai-api-key" // For client-side embedding generation
});
```

### Configuration from appsettings.json

**appsettings.json:**
```json
{
  "EntityMatching": {
    "ApiKey": "your-api-key-here",
    "BaseUrl": "https://EntityMatching-apim.azure-api.net/v1",
    "OpenAIKey": "your-openai-key-here"
  }
}
```

**Startup.cs or Program.cs:**
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register EntityMatchingClient
builder.Services.AddScoped<EntityMatchingClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new EntityMatchingClient(new EntityMatchingClientOptions
    {
        ApiKey = config["EntityMatching:ApiKey"],
        BaseUrl = config["EntityMatching:BaseUrl"],
        OpenAIKey = config["EntityMatching:OpenAIKey"]
    });
});
```

### User Secrets (Development)

```bash
dotnet user-secrets init
dotnet user-secrets set "EntityMatching:ApiKey" "your-api-key"
dotnet user-secrets set "EntityMatching:OpenAIKey" "your-openai-key"
```

---

## Profile Management

### Create a Profile

```csharp
using EntityMatching.Shared.Models;

var profile = await client.Profiles.CreateAsync(new Profile
{
    OwnedByUserId = "user-123",
    Name = "Alice Johnson",
    Bio = "Software engineer passionate about AI and machine learning",
    IsSearchable = true,
    CreatedAt = DateTime.UtcNow,
    LastModified = DateTime.UtcNow
});

Console.WriteLine($"Profile created: {profile.Id}");
```

### Get All Profiles for a User

```csharp
var profiles = await client.Profiles.ListAsync("user-123");

foreach (var profile in profiles)
{
    Console.WriteLine($"- {profile.Name} ({profile.Id})");
}
```

### Get a Specific Profile

```csharp
var profile = await client.Profiles.GetAsync(Guid.Parse("profile-id-here"));

Console.WriteLine($"Name: {profile.Name}");
Console.WriteLine($"Bio: {profile.Bio}");
```

### Update a Profile

```csharp
var updated = await client.Profiles.UpdateAsync(profileId, new Profile
{
    Bio = "Updated bio with new information",
    LastModified = DateTime.UtcNow,
    Preferences = new PreferencesAndInterests
    {
        Entertainment = new EntertainmentPreferences
        {
            MovieGenres = new List<string> { "Sci-Fi", "Drama", "Thriller" },
            MusicGenres = new List<string> { "Rock", "Jazz", "Electronic" }
        },
        Adventure = new AdventurePreferences
        {
            RiskTolerance = 7,
            NoveltySeeking = 8
        }
    }
});
```

### Delete a Profile

```csharp
await client.Profiles.DeleteAsync(profileId);
Console.WriteLine("Profile deleted");
```

### Get Similar Profiles

```csharp
var similarProfiles = await client.Profiles.GetSimilarAsync(profileId, limit: 10);

foreach (var match in similarProfiles)
{
    Console.WriteLine($"{match.ProfileId}: {match.SimilarityScore * 100:F1}% similar");
}
```

---

## Privacy-First Resume Upload

### Simple Upload

```csharp
// User provides resume text
string resumeText = GetResumeTextFromUser();

// Create profile
var profile = await client.Profiles.CreateAsync(new Profile
{
    OwnedByUserId = "user-123",
    Name = "Anonymous User",
    IsSearchable = true
});

// Upload resume (privacy-first)
// This generates embedding with OpenAI and uploads only the vector
await client.UploadResumeAsync(profile.Id, resumeText);

Console.WriteLine("✅ Resume uploaded as vector embedding");
Console.WriteLine("📄 Original text: NOT stored on server");
Console.WriteLine("🔒 Privacy: Maximum");
```

### ASP.NET Core API Endpoint

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly EntityMatchingClient _client;

    public ResumeController(EntityMatchingClient client)
    {
        _client = client;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadResume([FromBody] ResumeUploadRequest request)
    {
        try
        {
            // Create profile
            var profile = await _client.Profiles.CreateAsync(new Profile
            {
                OwnedByUserId = request.UserId,
                Name = request.Name,
                IsSearchable = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            });

            // Upload resume (privacy-first)
            await _client.UploadResumeAsync(profile.Id, request.ResumeText);

            return Ok(new
            {
                ProfileId = profile.Id,
                Message = "Resume uploaded successfully (as vector only)",
                PrivacyNote = "Your resume text was not stored"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public class ResumeUploadRequest
{
    public string UserId { get; set; }
    public string Name { get; set; }
    public string ResumeText { get; set; }
}
```

### Blazor Component Example

```razor
@page "/upload-resume"
@inject EntityMatchingClient Client

<h3>Privacy-First Resume Upload</h3>

<div class="form-group">
    <label>Resume Text:</label>
    <textarea @bind="resumeText" rows="10" class="form-control"></textarea>
</div>

<button class="btn btn-primary" @onclick="UploadResume" disabled="@isUploading">
    @(isUploading ? "Uploading..." : "Upload Resume")
</button>

@if (!string.IsNullOrEmpty(statusMessage))
{
    <div class="alert @alertClass mt-3">@statusMessage</div>
}

@code {
    private string resumeText = "";
    private bool isUploading = false;
    private string statusMessage = "";
    private string alertClass = "";

    private async Task UploadResume()
    {
        isUploading = true;
        statusMessage = "";

        try
        {
            // Create profile
            var profile = await Client.Profiles.CreateAsync(new Profile
            {
                OwnedByUserId = "demo-user",
                Name = "Demo User",
                IsSearchable = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            });

            // Upload resume (privacy-first)
            await Client.UploadResumeAsync(profile.Id, resumeText);

            statusMessage = $"✅ Success! Profile created: {profile.Id}. Your resume text was NOT stored on our servers.";
            alertClass = "alert-success";
            resumeText = ""; // Clear the form
        }
        catch (Exception ex)
        {
            statusMessage = $"❌ Error: {ex.Message}";
            alertClass = "alert-danger";
        }
        finally
        {
            isUploading = false;
        }
    }
}
```

---

## Conversational Profiling

Build profiles through natural conversations:

### Start a Conversation

```csharp
using EntityMatching.Core.Models.Conversation;

var response = await client.Conversations.SendMessageAsync(
    profileId,
    "user-123",
    "I really enjoy hiking and rock climbing. I go out almost every weekend!"
);

Console.WriteLine($"AI Response: {response.Message}");

// AI extracts insights automatically
foreach (var insight in response.Insights)
{
    Console.WriteLine($"- {insight.Category}: {insight.Content} ({insight.Confidence})");
}
```

### Multi-Turn Conversation

```csharp
async Task BuildProfileThroughChat(Guid profileId, string userId)
{
    var questions = new[]
    {
        "What do you like to do in your free time?",
        "What kind of movies do you enjoy?",
        "Are you more of an introvert or extrovert?",
        "What are your career goals?"
    };

    foreach (var question in questions)
    {
        Console.WriteLine($"\nQ: {question}");
        var userAnswer = Console.ReadLine(); // Get user input

        var response = await client.Conversations.SendMessageAsync(
            profileId,
            userId,
            userAnswer
        );

        Console.WriteLine($"AI: {response.Message}");

        // Insights are automatically added to profile
        if (response.Insights.Any())
        {
            Console.WriteLine("📝 Extracted insights:");
            foreach (var insight in response.Insights)
            {
                Console.WriteLine($"  - {insight.Content} ({insight.Confidence})");
            }
        }
    }
}
```

### Get Conversation History

```csharp
var conversation = await client.Conversations.GetAsync(profileId);

Console.WriteLine($"Messages: {conversation.Messages.Count}");
Console.WriteLine($"Insights extracted: {conversation.Insights.Count}");

foreach (var msg in conversation.Messages)
{
    Console.WriteLine($"{msg.Role}: {msg.Content}");
}
```

### Clear Conversation

```csharp
await client.Conversations.DeleteAsync(profileId);
Console.WriteLine("Conversation cleared");
```

---

## Semantic Search

### Basic Search

```csharp
using EntityMatching.Core.Models.Search;

var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "Full-stack developer with React and Node.js experience",
    Limit = 10,
    MinSimilarity = 0.7f
});

Console.WriteLine($"Found {results.TotalMatches} matches in {results.Metadata.SearchDurationMs}ms");

for (int i = 0; i < results.Matches.Count; i++)
{
    var match = results.Matches[i];
    Console.WriteLine($"{i + 1}. Profile {match.ProfileId}: {match.SimilarityScore * 100:F1}% match");
}
```

### Privacy-Protected Search (Recruiter View)

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "Senior software engineer, Python, AWS, 10+ years",
    RequestingUserId = "company-recruiter-456",
    EnforcePrivacy = true, // Only returns profile IDs
    Limit = 20,
    MinSimilarity = 0.75f
});

Console.WriteLine("🔒 Privacy mode: Only profile IDs returned");
foreach (var match in results.Matches)
{
    Console.WriteLine($"Profile #{match.ProfileId.ToString().Substring(0, 8)}: {match.SimilarityScore * 100:F1}% match");
    // Name, email, phone are NOT included
}
```

---

## Advanced Search with Filters

### Combine Semantic + Structured Search

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    // Semantic part
    Query = "loves outdoor activities and adventure sports",

    // Structured filters
    AttributeFilters = new FilterGroup
    {
        LogicalOperator = LogicalOperator.And,
        Filters = new List<AttributeFilter>
        {
            new()
            {
                FieldPath = "preferences.adventure.riskTolerance",
                Operator = FilterOperator.GreaterThan,
                Value = 6
            },
            new()
            {
                FieldPath = "preferences.nature.outdoorActivities",
                Operator = FilterOperator.Contains,
                Value = "Hiking"
            },
            new()
            {
                FieldPath = "location.city",
                Operator = FilterOperator.Equals,
                Value = "Denver"
            }
        }
    },

    MinSimilarity = 0.6f,
    Limit = 15
});
```

### Complex Filter Groups (OR Logic)

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "experienced software engineer",

    AttributeFilters = new FilterGroup
    {
        LogicalOperator = LogicalOperator.Or,
        Filters = new List<AttributeFilter>
        {
            new() { FieldPath = "skills", Operator = FilterOperator.Contains, Value = "Python" },
            new() { FieldPath = "skills", Operator = FilterOperator.Contains, Value = "JavaScript" },
            new() { FieldPath = "skills", Operator = FilterOperator.Contains, Value = "Java" }
        }
    },

    MinSimilarity = 0.7f,
    Limit = 20
});
```

### Nested Filter Groups

```csharp
var results = await client.Search.SearchAsync(new SearchRequest
{
    Query = "looking for travel companions",

    AttributeFilters = new FilterGroup
    {
        LogicalOperator = LogicalOperator.And,
        Filters = new List<AttributeFilter>
        {
            new()
            {
                FieldPath = "location.country",
                Operator = FilterOperator.Equals,
                Value = "USA"
            }
        },
        SubGroups = new List<FilterGroup>
        {
            new FilterGroup
            {
                LogicalOperator = LogicalOperator.Or,
                Filters = new List<AttributeFilter>
                {
                    new()
                    {
                        FieldPath = "preferences.adventure.riskTolerance",
                        Operator = FilterOperator.GreaterThan,
                        Value = 7
                    },
                    new()
                    {
                        FieldPath = "preferences.adventure.noveltySeeking",
                        Operator = FilterOperator.GreaterThan,
                        Value = 8
                    }
                }
            }
        }
    }
});
```

---

## Error Handling

### Basic Try-Catch

```csharp
try
{
    var profile = await client.Profiles.GetAsync(Guid.Parse("invalid-id"));
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    Console.WriteLine("Profile not found");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    Console.WriteLine("Invalid API key");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    Console.WriteLine("Rate limit exceeded");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Retry with Polly

```bash
dotnet add package Polly
```

```csharp
using Polly;
using Polly.Retry;

var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
        }
    );

// Usage
var profile = await retryPolicy.ExecuteAsync(async () =>
    await client.Profiles.GetAsync(profileId)
);
```

---

## Async/Await Patterns

### Sequential Operations

```csharp
// Create profile, then upload resume
var profile = await client.Profiles.CreateAsync(newProfile);
await client.UploadResumeAsync(profile.Id, resumeText);
```

### Parallel Operations

```csharp
// Fetch multiple profiles in parallel
var profileIds = new[] { id1, id2, id3, id4 };

var tasks = profileIds.Select(id => client.Profiles.GetAsync(id));
var profiles = await Task.WhenAll(tasks);

foreach (var profile in profiles)
{
    Console.WriteLine($"Loaded: {profile.Name}");
}
```

### Cancellation Support

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var results = await client.Search.SearchAsync(searchRequest, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Search timeout after 30 seconds");
}
```

---

## Examples

### Example 1: Job Board Service

```csharp
public class JobMatchingService
{
    private readonly EntityMatchingClient _client;

    public JobMatchingService(EntityMatchingClient client)
    {
        _client = client;
    }

    public async Task<Guid> CreateCandidateProfileAsync(string userId, string resume)
    {
        // Create profile
        var profile = await _client.Profiles.CreateAsync(new Profile
        {
            OwnedByUserId = userId,
            Name = "Anonymous Candidate",
            IsSearchable = true,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        });

        // Upload resume (privacy-first)
        await _client.UploadResumeAsync(profile.Id, resume);

        return profile.Id;
    }

    public async Task<List<SearchMatch>> FindCandidatesAsync(
        string jobDescription,
        List<string> requiredSkills)
    {
        var results = await _client.Search.SearchAsync(new SearchRequest
        {
            Query = jobDescription,
            AttributeFilters = new FilterGroup
            {
                LogicalOperator = LogicalOperator.And,
                Filters = requiredSkills.Select(skill => new AttributeFilter
                {
                    FieldPath = "skills",
                    Operator = FilterOperator.Contains,
                    Value = skill
                }).ToList()
            },
            MinSimilarity = 0.75f,
            EnforcePrivacy = true,
            Limit = 50
        });

        return results.Matches;
    }
}

// Usage
var service = new JobMatchingService(client);

// Candidate uploads resume
var profileId = await service.CreateCandidateProfileAsync(
    "user-123",
    resumeText
);

// Company searches
var candidates = await service.FindCandidatesAsync(
    "We're looking for a senior Python engineer with AWS and ML experience",
    new List<string> { "Python", "AWS", "Machine Learning" }
);
```

### Example 2: Background Service

```csharp
using Microsoft.Extensions.Hosting;

public class ProfileIndexingService : BackgroundService
{
    private readonly EntityMatchingClient _client;
    private readonly ILogger<ProfileIndexingService> _logger;

    public ProfileIndexingService(
        EntityMatchingClient client,
        ILogger<ProfileIndexingService> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process new profiles that need indexing
                await IndexPendingProfilesAsync(stoppingToken);

                // Wait 5 minutes before next run
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in profile indexing service");
            }
        }
    }

    private async Task IndexPendingProfilesAsync(CancellationToken ct)
    {
        // Your logic to find and index profiles
        _logger.LogInformation("Checking for profiles to index...");
    }
}

// Register in Program.cs
builder.Services.AddHostedService<ProfileIndexingService>();
```

### Example 3: Minimal API

```csharp
using EntityMatching.SDK;
using EntityMatching.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<EntityMatchingClient>(sp =>
    new EntityMatchingClient(new EntityMatchingClientOptions
    {
        ApiKey = builder.Configuration["EntityMatching:ApiKey"],
        BaseUrl = builder.Configuration["EntityMatching:BaseUrl"],
        OpenAIKey = builder.Configuration["EntityMatching:OpenAIKey"]
    })
);

var app = builder.Build();

app.MapPost("/api/profiles", async (Profile profile, EntityMatchingClient client) =>
{
    var created = await client.Profiles.CreateAsync(profile);
    return Results.Created($"/api/profiles/{created.Id}", created);
});

app.MapGet("/api/profiles/{id:guid}", async (Guid id, EntityMatchingClient client) =>
{
    try
    {
        var profile = await client.Profiles.GetAsync(id);
        return Results.Ok(profile);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
});

app.MapPost("/api/search", async (SearchRequest request, EntityMatchingClient client) =>
{
    var results = await client.Search.SearchAsync(request);
    return Results.Ok(results);
});

app.Run();
```

---

## Best Practices

1. **Use Dependency Injection**
   - Register `EntityMatchingClient` as scoped or singleton
   - Don't create new instances for each request

2. **Handle API Keys Securely**
   - Store in User Secrets (development)
   - Store in Azure Key Vault (production)
   - Never commit keys to source control

3. **Implement Retry Logic**
   - Use Polly for resilient HTTP calls
   - Implement exponential backoff
   - Handle transient failures gracefully

4. **Privacy First**
   - Always use `EnforcePrivacy = true` for public searches
   - Generate embeddings client-side when possible
   - Validate privacy settings before queries

5. **Performance**
   - Use async/await consistently
   - Implement caching where appropriate
   - Batch operations when possible
   - Use `Task.WhenAll` for parallel operations

6. **Error Handling**
   - Catch specific HTTP status codes
   - Provide user-friendly error messages
   - Log errors with structured logging

---

## API Reference

See [CORE_PLATFORM_API.md](./CORE_PLATFORM_API.md) for complete API reference.

## Support

- **GitHub**: https://github.com/iunknown21/EntityMatchingAPI/issues
- **Email**: support@bystorm.com
