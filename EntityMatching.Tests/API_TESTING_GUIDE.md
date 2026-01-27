# API-Level Integration Testing Guide

This guide explains the two-tier testing approach for the Profile Matching API.

## Testing Approaches

### Service-Level Tests (ProfileMatchingWorkflowIntegrationTests)
```
Test → Services directly → Cosmos DB / OpenAI
```

**When to use:**
- During development (TDD)
- Testing service logic and algorithms
- Debugging embedding generation
- Fast iteration

**Requirements:**
- OpenAI API key in `testsettings.json`
- Cosmos DB connection string

**Run:**
```bash
dotnet test --filter FullyQualifiedName~ProfileMatchingWorkflowIntegrationTests
```

---

### API-Level Tests (ProfileMatchingApiTests)
```
Test → HTTP → Azure Functions → Services → Cosmos DB / OpenAI
```

**When to use:**
- Before commits
- Testing complete HTTP stack
- Validating deployed environments
- CI/CD pipelines

**Requirements:**
- Azure Functions app running (local or Azure)
- Functions app has its own config (no OpenAI key needed in tests)

**Run:**
```bash
# Local Functions
dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests

# Against Azure
$env:API_BASE_URL = "https://your-app.azurewebsites.net"
$env:API_FUNCTION_KEY = "your-key"
dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
```

## Setup Instructions

### 1. Configure testsettings.json

```json
{
  "CosmosDb": {
    "ConnectionString": "YOUR_COSMOS_CONNECTION",
    "DatabaseId": "ProfileMatchingTestDB",
    "ProfilesContainerId": "profiles",
    "ConversationsContainerId": "conversations",
    "EmbeddingsContainerId": "embeddings"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_KEY",
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingDimensions": "1536"
  },
  "Api": {
    "BaseUrl": "http://localhost:7071",
    "FunctionKey": ""
  }
}
```

### 2. Running API Tests Locally

**Step 1: Start Functions App**
```bash
cd ProfileMatching.Functions
func start
```

You should see output like:
```
Functions:
  AdminProcessEmbeddings: [POST] http://localhost:7071/api/admin/embeddings/process
  GetProfiles: [GET] http://localhost:7071/api/v1/profiles
  GetProfileById: [GET] http://localhost:7071/api/v1/profiles/{id}
  CreateProfile: [POST] http://localhost:7071/api/v1/profiles
  UpdateProfile: [PUT] http://localhost:7071/api/v1/profiles/{id}
  DeleteProfile: [DELETE] http://localhost:7071/api/v1/profiles/{id}
  SearchProfiles: [POST] http://localhost:7071/api/v1/profiles/search
  GetSimilarProfiles: [GET] http://localhost:7071/api/v1/profiles/{profileId}/similar
```

**Step 2: Run API Tests** (in a separate terminal)
```bash
cd ProfileMatching.Tests
dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
```

### 3. Running API Tests Against Azure

**Set environment variables:**
```powershell
# PowerShell
$env:API_BASE_URL = "https://your-app.azurewebsites.net"
$env:API_FUNCTION_KEY = "your-function-key"

# Bash
export API_BASE_URL="https://your-app.azurewebsites.net"
export API_FUNCTION_KEY="your-function-key"
```

**Run tests:**
```bash
dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
```

## Test Coverage

### ProfileMatchingApiTests

**Profile CRUD Operations:**
- ✅ Create profile via API
- ✅ Get profile by ID
- ✅ Update profile
- ✅ Get all profiles for user
- ✅ Delete profile

**End-to-End Workflow:**
- ✅ Create multiple profiles with different characteristics
- ✅ Trigger embedding generation via admin endpoint
- ✅ Search for profiles using text queries
- ✅ Find similar profiles

**Error Handling:**
- ✅ Invalid profile ID returns 404
- ✅ Missing userId parameter returns 400
- ✅ Empty search query returns 400

## Enabled Functions

The following functions have been enabled for API testing:

### SearchFunctions.cs
- `POST /api/v1/profiles/search` - Text query-based search
- `GET /api/v1/profiles/{profileId}/similar` - Profile-to-profile similarity

### AdminFunctions.cs
- `POST /api/admin/embeddings/process` - Manually trigger embedding generation
- Other admin endpoints for debugging

### ProcessPendingEmbeddingsFunction.cs
- Timer-triggered function for automatic embedding processing

## Troubleshooting

### "API is not available"

**Problem:** Tests can't connect to Functions app

**Solution:**
1. Make sure Functions app is running (`func start`)
2. Check the URL matches (default: `http://localhost:7071`)
3. Verify no firewall blocking port 7071

### "SearchFunctions may be disabled"

**Problem:** Search endpoints return 404

**Solution:**
1. Verify `SearchFunctions.cs` exists (not `.disabled`)
2. Rebuild Functions project
3. Restart Functions app with `func start`

### "Embedding generation failed"

**Problem:** Admin endpoint can't generate embeddings

**Solution:**
1. Check OpenAI API key in Functions `local.settings.json`
2. Verify Cosmos DB connection in Functions config
3. Check Functions app logs for detailed errors

### Tests timeout

**Problem:** Tests take too long or hang

**Solution:**
1. Embedding generation via OpenAI takes 1-2 seconds per profile
2. Complete workflow test with 4 profiles + processing: ~15-20 seconds
3. Increase timeout if needed (default: 30 seconds)

## CI/CD Integration

### GitHub Actions Example

```yaml
name: API Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2

      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '8.0.x'

      - name: Start Azure Functions
        run: |
          cd ProfileMatching.Functions
          func start &
          sleep 10  # Wait for startup

      - name: Run API Tests
        run: |
          cd ProfileMatching.Tests
          dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
        env:
          API_BASE_URL: http://localhost:7071
```

### Azure DevOps Example

```yaml
stages:
  - stage: IntegrationTests
    jobs:
      - job: ApiTests
        steps:
          - task: DotNetCoreCLI@2
            displayName: 'Start Functions App'
            inputs:
              command: 'custom'
              custom: 'func'
              arguments: 'start'
              workingDirectory: 'ProfileMatching.Functions'

          - task: DotNetCoreCLI@2
            displayName: 'Run API Tests'
            inputs:
              command: 'test'
              projects: 'ProfileMatching.Tests/ProfileMatching.Tests.csproj'
              arguments: '--filter FullyQualifiedName~ProfileMatchingApiTests'
            env:
              API_BASE_URL: $(FunctionsAppUrl)
              API_FUNCTION_KEY: $(FunctionKey)
```

## Development Workflow

### Quick Development Cycle (Service-Level)
```bash
# Make changes to service code
# Run fast service tests
dotnet test --filter FullyQualifiedName~ProfileMatchingWorkflowIntegrationTests
# ~10-15 seconds
```

### Pre-Commit Validation (API-Level)
```bash
# Terminal 1: Start Functions
cd ProfileMatching.Functions && func start

# Terminal 2: Run API tests
cd ProfileMatching.Tests
dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
# ~20-30 seconds
```

### Deployment Validation (Against Azure)
```bash
# Deploy to staging
# Run API tests against staging
$env:API_BASE_URL = "https://staging-app.azurewebsites.net"
dotnet test --filter FullyQualifiedName~ProfileMatchingApiTests
```

## Best Practices

1. **Use service-level tests for:**
   - Algorithm development
   - Business logic validation
   - Fast feedback during coding

2. **Use API-level tests for:**
   - HTTP layer validation
   - Request/response format verification
   - Deployed environment validation
   - Pre-commit checks

3. **Keep both test suites:**
   - Service-level: Fast, focused, easy to debug
   - API-level: Complete, realistic, deployment-ready

4. **Environment-specific testing:**
   - Local: Both test types
   - CI: Service-level only (faster)
   - CD: API-level against deployed environment

## Cost Impact

**Service-Level Tests:**
- OpenAI API calls: ~$0.02-0.04 per test run (7 profiles)
- Cosmos DB: Minimal (serverless)

**API-Level Tests:**
- Same as service-level (calls same services)
- No additional cost for HTTP layer

**Total per full test suite:** ~$0.05-0.10 per run

## Summary

This two-tier approach gives you:
- ✅ Fast iteration during development (service-level)
- ✅ Complete stack validation (API-level)
- ✅ Flexibility to test locally or against Azure
- ✅ Confidence in both business logic and HTTP layer
- ✅ Efficient CI/CD pipelines
