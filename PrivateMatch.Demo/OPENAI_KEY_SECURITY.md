# OpenAI API Key Security in Client-Side Apps

## The Problem

Blazor WebAssembly runs entirely in the browser. **Any secrets in the app are visible to users.**

### What Users Can See:

1. **Network Traffic:**
   ```
   POST https://api.openai.com/v1/embeddings
   Authorization: Bearer sk-proj-YOUR_KEY_HERE  ← Visible in DevTools
   ```

2. **Downloaded Configuration:**
   ```
   https://localhost:5001/appsettings.json
   {
     "OpenAI": {
       "ApiKey": "sk-proj-YOUR_KEY_HERE"  ← Publicly downloadable
     }
   }
   ```

3. **Source Code:**
   - All C# code compiled to WebAssembly (can be decompiled)
   - All JavaScript visible in browser

**Bottom line: Client-side keys are never truly secure.**

---

## Current Setup: Demo/Testing Only

**⚠️ The current configuration is for LOCAL TESTING ONLY.**

**Why it's acceptable for testing:**
- You're testing locally (http://localhost:5001)
- Not deployed publicly
- Temporary API key with low usage limits

**Why it's NOT acceptable for production:**
- Anyone can extract and abuse your key
- You pay for all usage (even abuse)
- No way to prevent extraction

---

## Production Solutions

### Option 1: Backend Proxy (RECOMMENDED)

**Best balance of security and privacy.**

**How it works:**
```
User → PrivateMatch Demo → YOUR API → OpenAI → Return Embedding
        (browser)           (proxy)      ↑
                                         Key stays here
```

**Implementation:**

1. **Add proxy endpoint to Azure Functions:**

   ```csharp
   // ProfileMatching.Functions/EmbeddingsFunction.cs
   [Function("ProxyOpenAIEmbedding")]
   public async Task<HttpResponseData> ProxyOpenAIEmbedding(
       [HttpTrigger(AuthorizationLevel.Anonymous, "post",
           Route = "v1/proxy/embeddings")] HttpRequestData req)
   {
       // Read text from request
       var request = await req.ReadFromJsonAsync<ProxyEmbeddingRequest>();

       // Call OpenAI using server-side key
       var embedding = await _openAIService.GenerateEmbeddingAsync(request.Text);

       // Return embedding (text is NOT stored)
       var response = req.CreateResponse(HttpStatusCode.OK);
       await response.WriteAsJsonAsync(new { embedding });
       return response;
   }
   ```

2. **Update Blazor demo to use proxy:**

   ```csharp
   // PrivateMatch.Demo/Services/EmbeddingService.cs
   public async Task<float[]> GenerateEmbeddingAsync(string text)
   {
       // Call YOUR API (not OpenAI directly)
       var response = await _httpClient.PostAsJsonAsync(
           "https://profilematching-apim.azure-api.net/v1/proxy/embeddings",
           new { text }
       );

       var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
       return result.Embedding;
   }
   ```

3. **Remove OpenAI key from client:**

   ```json
   // PrivateMatch.Demo/wwwroot/appsettings.json
   {
     // No OpenAI key needed - backend handles it!
   }
   ```

**Benefits:**
- ✅ OpenAI key stays on server (secure)
- ✅ You control rate limiting
- ✅ Monitor all usage
- ✅ Text doesn't persist (just proxied)
- ⚠️ Text briefly passes through your servers (in memory only)

**Tradeoff:**
- Resume text reaches your backend (temporarily)
- Not "pure" client-side privacy
- But realistically, this is the best option for production

---

### Option 2: Server-Side Generation Only

**Most secure, least private.**

**How it works:**
```
User uploads resume text → YOUR API stores text → Generates embedding
```

**Implementation:**

Use existing endpoint:
```bash
POST /api/v1/profiles/{id}/embeddings/generate
```

This generates embeddings server-side (no client-side OpenAI key needed).

**Benefits:**
- ✅ Maximum security
- ✅ No key exposure risk
- ✅ Full control

**Downsides:**
- ❌ Resume text stored on your servers (defeats privacy-first)
- ❌ You pay for all embeddings

---

### Option 3: User-Provided Keys

**Let users bring their own OpenAI keys.**

**Implementation:**

```razor
@* PrivateMatch.Demo/Pages/Settings.razor *@
<h3>OpenAI API Key</h3>
<input type="password" @bind="userApiKey" placeholder="sk-proj-..." />
<button @onclick="SaveKey">Save Key</button>

@code {
    private string userApiKey = "";

    private void SaveKey()
    {
        // Store in browser localStorage
        localStorage.SetItem("openai_key", userApiKey);
    }
}
```

**Benefits:**
- ✅ You don't expose your key
- ✅ Users pay for their own usage
- ✅ True client-side privacy

**Downsides:**
- ❌ Poor UX (requires OpenAI account)
- ❌ Users' keys still visible in their browser
- ❌ Only works for technical users

---

## Immediate Action Items

### For Current Testing (Right Now):

1. **Set strict OpenAI usage limits:**
   - Go to: https://platform.openai.com/account/limits
   - Set monthly cap: $5-10
   - Set up email alerts at 80%
   - Enable billing alerts

2. **Create dedicated demo key:**
   ```bash
   # Create new key just for demo
   # Name it: "PrivateMatch Demo - LOCAL TESTING ONLY"
   # Delete when done testing
   ```

3. **Monitor usage daily:**
   - https://platform.openai.com/usage
   - Watch for unexpected spikes

4. **Rotate key after testing:**
   - Delete the demo key after you're done
   - Create new key for next testing session

### Before Public Deployment:

**DO NOT deploy the demo publicly with client-side OpenAI key!**

**Instead, choose one of:**
1. ✅ Implement backend proxy (Option 1) - **RECOMMENDED**
2. ✅ Use server-side generation only (Option 2)
3. ✅ Require user-provided keys (Option 3)

---

## Cost Analysis

### Client-Side Key (Current - Risky):

**If key is discovered:**
- Malicious user makes 1,000 requests/hour
- Cost: $0.0001/1K tokens × 1K tokens × 1,000 = **$0.10/hour**
- Over 24 hours: **$2.40/day**
- Over 30 days: **$72/month**

**With $10 monthly limit:**
- Attacker can burn through limit in ~4 days
- You get email alert
- Key gets rate-limited by OpenAI

### Backend Proxy (Recommended):

**With rate limiting:**
- Your APIM rate limit: 100 req/min (Free tier)
- Max cost: $0.0001 × 1K × 100 × 60 × 24 = **$8.64/day max**
- But you control who can access (subscription keys)
- Can implement additional rate limits

---

## Example: Backend Proxy Implementation

**Complete implementation for Option 1:**

### 1. Add to Azure Functions

```csharp
// ProfileMatching.Functions/ProxyFunction.cs
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using ProfileMatching.Infrastructure.Services;

namespace ProfileMatching.Functions;

public class ProxyFunction
{
    private readonly IOpenAIService _openAIService;

    public ProxyFunction(IOpenAIService openAIService)
    {
        _openAIService = openAIService;
    }

    [Function("ProxyOpenAIEmbedding")]
    public async Task<HttpResponseData> ProxyOpenAIEmbedding(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "v1/proxy/embeddings")] HttpRequestData req)
    {
        try
        {
            // Parse request
            var request = await req.ReadFromJsonAsync<ProxyEmbeddingRequest>();

            if (string.IsNullOrEmpty(request?.Text))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Text is required");
                return badRequest;
            }

            // Generate embedding using server-side OpenAI key
            var embedding = await _openAIService.GenerateEmbeddingAsync(
                request.Text,
                "text-embedding-3-small"
            );

            // Return embedding only (text is NOT stored)
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ProxyEmbeddingResponse
            {
                Embedding = embedding,
                Model = "text-embedding-3-small",
                Dimensions = embedding.Length
            });

            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}

public record ProxyEmbeddingRequest(string Text);
public record ProxyEmbeddingResponse(float[] Embedding, string Model, int Dimensions);
```

### 2. Update Blazor Demo

```csharp
// PrivateMatch.Demo/Services/EmbeddingService.cs
public class EmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public EmbeddingService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _baseUrl = config["ProfileMatching:BaseUrl"] ??
            "https://profilematching-apim.azure-api.net/v1";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        // Call YOUR proxy endpoint (not OpenAI directly)
        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/proxy/embeddings",
            new { text }
        );

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        return result.Embedding;
    }
}
```

### 3. Update Program.cs

```csharp
// PrivateMatch.Demo/Program.cs
builder.Services.AddScoped<EmbeddingService>();

// Remove OpenAIKey from ProfileMatchingClient config
builder.Services.AddScoped(sp => new ProfileMatchingClient(new ProfileMatchingClientOptions
{
    BaseUrl = "https://profilematching-apim.azure-api.net/v1",
    ApiKey = "", // Demo tier
    // OpenAIKey removed - using backend proxy now
}));
```

### 4. Deploy

```bash
# Deploy updated Functions
cd ProfileMatching.Functions
func azure functionapp publish profileaiapi

# Deploy updated demo
cd PrivateMatch.Demo
dotnet publish -c Release
# (Follow Static Web Apps deployment steps)
```

**Result:**
- ✅ OpenAI key secure on server
- ✅ Demo works exactly the same
- ✅ No key exposure risk

---

## Summary

| Approach | Security | Privacy | UX | Production Ready? |
|----------|----------|---------|----|--------------------|
| **Client-side key** | ❌ Low | ✅ High | ✅ Easy | ❌ NO |
| **Backend proxy** | ✅ High | ⚠️ Medium | ✅ Easy | ✅ YES |
| **Server-side only** | ✅ High | ❌ Low | ✅ Easy | ✅ YES |
| **User keys** | ✅ High | ✅ High | ❌ Hard | ⚠️ Maybe |

**Recommendation:** Use **backend proxy** for production (Option 1).

---

## Questions?

See main documentation:
- [Developer Onboarding](../docs/DEVELOPER_ONBOARDING.md)
- [APIM Guide](../docs/DEVELOPER_GUIDE_APIM.md)
- [Demo Testing Guide](./TESTING.md)
