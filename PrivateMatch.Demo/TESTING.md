# Testing the PrivateMatch Demo Locally

Quick guide to test the demo against your deployed Azure API.

## Prerequisites

- ✅ Azure Functions published to Azure (profileaiapi)
- ✅ APIM configured (profilematching-apim)
- ⚠️ OpenAI API key (for client-side embedding generation)

---

## Step 1: Add Your OpenAI API Key

**Edit:** `PrivateMatch.Demo/wwwroot/appsettings.json`

```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-YOUR_OPENAI_KEY_HERE"
  }
}
```

**Get an OpenAI key:**
1. Go to: https://platform.openai.com/api-keys
2. Click **Create new secret key**
3. Copy the key (starts with `sk-proj-...`)
4. Paste into appsettings.json

**⚠️ This file is gitignored** - your key won't be committed.

---

## Step 2: Run the Demo

**Option 1: Command Line**

```bash
cd PrivateMatch.Demo
dotnet run
```

**Option 2: Visual Studio**

1. Right-click `PrivateMatch.Demo` project
2. **Set as Startup Project**
3. Press **F5** or click **Run**

**Wait for:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Application started. Press Ctrl+C to shut down.
```

---

## Step 3: Open Browser

Navigate to: **https://localhost:5001**

You should see the PrivateMatch demo landing page.

---

## Step 4: Test the Features

### Test 1: Upload Resume (Privacy-First)

1. Click **Upload Resume** in navigation
2. Paste this sample resume:
   ```
   Senior Software Engineer

   EXPERIENCE:
   - 10 years of Python development
   - Expert in AWS (Lambda, S3, DynamoDB)
   - Built ML pipelines processing 100M+ events/day

   SKILLS:
   - Python, TypeScript, Go
   - AWS, Azure, GCP
   - TensorFlow, PyTorch
   ```

3. Click **Generate Embedding Locally**
   - Wait 2-3 seconds
   - You'll see the 1536-dimensional vector appear

4. Click **Upload Vector Only**
   - Uploads to your deployed API
   - Success message should appear

**What just happened:**
- ✅ Resume text stayed in your browser
- ✅ OpenAI generated embedding (client-side)
- ✅ Only vector uploaded to Azure
- ✅ Your deployed API stored the vector in Cosmos DB

**Verify in logs:**
```bash
# Check Azure Functions logs
az functionapp log tail --name profileaiapi --resource-group profilesai
```

### Test 2: Search Profiles

1. Click **Search Profiles** in navigation
2. Enter query:
   ```
   Senior Python engineer with AWS and ML experience
   ```

3. Click **Search**
   - Should find the profile you just uploaded
   - Shows similarity score (should be high, like 94%)

**What just happened:**
- ✅ Query sent to your deployed API
- ✅ API generated query embedding via OpenAI
- ✅ Cosmos DB vector search found matches
- ✅ Results returned with similarity scores

### Test 3: Privacy Proof

1. Click **Privacy & Cost Proof** in navigation
2. Explore:
   - Side-by-side comparison
   - Cost calculator (interactive slider)
   - **Data breach simulation** (click the button!)

**What the breach simulation shows:**
- ❌ Traditional platform: Full PII exposed
- ✅ PrivateMatch: Only meaningless numbers

---

## Troubleshooting

### Issue: "OpenAI API key not configured"

**Fix:** Add your OpenAI key to `wwwroot/appsettings.json` (see Step 1)

### Issue: "Failed to upload embedding"

**Possible causes:**

1. **Azure Functions not running**
   ```bash
   # Check if Functions are running
   curl https://profileaiapi.azurewebsites.net/api/v1/profiles
   ```

2. **APIM gateway down**
   ```bash
   # Test APIM gateway
   curl https://profilematching-apim.azure-api.net/v1/profiles
   ```

3. **CORS issue**
   - Check browser console (F12)
   - APIM policy should allow `http://localhost:5001`

**Fix:** Check APIM CORS policy includes localhost:
```xml
<cors allow-credentials="true">
    <allowed-origins>
        <origin>http://localhost:5001</origin>
        <origin>https://localhost:5001</origin>
    </allowed-origins>
</cors>
```

### Issue: Search returns no results

**Possible causes:**

1. **No profiles uploaded yet**
   - Upload a resume first (see Test 1)

2. **Query too specific**
   - Try broader query: "software engineer"

3. **Minimum similarity too high**
   - Lower the similarity threshold slider

### Issue: Slow embedding generation

**This is normal!** OpenAI API typically takes 1-3 seconds to generate embeddings.

---

## Verify End-to-End Flow

**Complete flow from browser to Azure:**

```
Browser (localhost:5001)
    │
    │ 1. User enters resume text
    ▼
OpenAI API (client-side)
    │
    │ 2. Generate embedding
    ▼
Browser JavaScript
    │
    │ 3. Upload vector only
    ▼
APIM Gateway (profilematching-apim.azure-api.net)
    │
    │ 4. Validate subscription, rate limit
    ▼
Azure Functions (profileaiapi.azurewebsites.net)
    │
    │ 5. Process request
    ▼
Cosmos DB (profilesaidb)
    │
    │ 6. Store vector
    ✅ Done!
```

**Verify each step:**

1. ✅ Browser console shows API calls (F12 → Network tab)
2. ✅ APIM gateway responds (check APIM analytics in Azure Portal)
3. ✅ Azure Functions processes (check Application Insights)
4. ✅ Cosmos DB stores data (check Data Explorer in Azure Portal)

---

## Next Steps

### Deploy Demo to Production

Once local testing works:

1. **Create Azure Static Web App**
   ```bash
   az staticwebapp create \
     --name privatematch-demo \
     --resource-group profilesai \
     --location centralus \
     --sku Free
   ```

2. **Get deployment token**
   ```bash
   az staticwebapp secrets list \
     --name privatematch-demo \
     --resource-group profilesai \
     --query "properties.apiKey" -o tsv
   ```

3. **Add to GitHub secrets**
   - Name: `AZURE_STATIC_WEB_APPS_API_TOKEN`
   - Value: (deployment token from step 2)

4. **Push to GitHub**
   ```bash
   git push origin master
   ```

   GitHub Actions automatically deploys the demo!

### Production OpenAI Key

For production deployment, DON'T use appsettings.json in wwwroot (it's exposed to clients).

**Instead:**
- Store OpenAI key in Azure Key Vault
- Retrieve on server-side (if you have a backend)
- **OR** accept that client-side keys are visible (for Blazor WASM, this is unavoidable)

**Security note:** Blazor WebAssembly runs entirely in the browser, so any configuration in `wwwroot/appsettings.json` is visible to users. For production:
- Use OpenAI API key with strict rate limits
- Monitor usage in OpenAI dashboard
- Consider moving embedding generation to server-side

---

## Support

**Issues?**
- Check browser console (F12 → Console tab)
- Check Network tab for API calls
- Check Azure Function logs
- See main docs: [DEMO_GUIDE.md](../docs/DEMO_GUIDE.md)
