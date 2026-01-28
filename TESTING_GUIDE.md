# Testing Guide - EntityMatching API

This guide explains how to run tests for the EntityMatching API.

---

## ✅ Test Configuration Complete

Your `testsettings.json` has been updated with the new Azure deployment:

```json
{
  "CosmosDb": {
    "ConnectionString": "AccountEndpoint=https://entitymatchingdb.documents.azure.com:443/...",
    "DatabaseId": "EntityMatchingDB",
    "ProfilesContainerId": "entities",
    "ConversationsContainerId": "conversations",
    "EmbeddingsContainerId": "embeddings"
  },
  "OpenAI": {
    "ApiKey": "sk-svcacct-...",
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingDimensions": "1536",
    "MaxRetries": "3"
  },
  "ApiKeys": {
    "Groq": "gsk_..."
  },
  "Api": {
    "BaseUrl": "https://entityaiapi.azurewebsites.net",
    "FunctionKey": ""
  }
}
```

---

## Running Tests

### Option 1: Run Unit Tests Only (No API Key Required)

Unit tests don't require Azure access and run locally:

```bash
# Run all unit tests (fast, no external dependencies)
dotnet test --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~Demo"

# Or exclude by category
dotnet test --filter "Category!=Integration&Category!=Demo"
```

**Expected:** All unit tests should pass immediately.

---

### Option 2: Run Integration Tests (Requires Setup)

Integration tests connect to Azure Cosmos DB and test real services.

#### Step 1: Verify testsettings.json (Already Done ✅)

The file is already configured with your Azure deployment.

#### Step 2: Run Integration Tests

```bash
# Run integration tests (uses Cosmos DB)
dotnet test --filter "FullyQualifiedName~Integration"
```

**What these tests do:**
- Create/read/update/delete entities in Cosmos DB
- Test embedding storage and retrieval
- Test conversation service
- Test search functionality

**Note:** These tests create temporary test data and clean up after themselves (unless `SKIP_TEST_CLEANUP=true` is set).

---

### Option 3: Run Demo Tests (Requires Setup)

Demo tests showcase full workflows with real data.

```bash
# Run demo tests
dotnet test --filter "FullyQualifiedName~Demo"
```

**What these tests do:**
- Large-scale search demonstrations
- Conversational entity interactions
- End-to-end workflow scenarios

---

### Option 4: Run API Integration Tests (Requires Function Key)

API tests call your deployed Azure Function App endpoints.

#### Get Function App Key:

```bash
# Get the master key (not recommended for production)
az functionapp keys list --name entityaiapi --resource-group entitymatchingai

# Or get a specific function key
az functionapp function keys list \
  --name entityaiapi \
  --resource-group entitymatchingai \
  --function-name GetVersion
```

#### Option A: Set Environment Variable

```bash
# Windows (PowerShell)
$env:API_BASE_URL = "https://entityaiapi.azurewebsites.net"
$env:API_FUNCTION_KEY = "your-function-key-here"

# Windows (CMD)
set API_BASE_URL=https://entityaiapi.azurewebsites.net
set API_FUNCTION_KEY=your-function-key-here

# Linux/Mac
export API_BASE_URL=https://entityaiapi.azurewebsites.net
export API_FUNCTION_KEY=your-function-key-here
```

#### Option B: Update testsettings.json

```json
{
  "Api": {
    "BaseUrl": "https://entityaiapi.azurewebsites.net",
    "FunctionKey": "your-function-key-here"
  }
}
```

#### Run API Tests:

```bash
dotnet test --filter "FullyQualifiedName~ApiTests"
```

---

## Test Categories

### Unit Tests (Fast, No Dependencies)
- Core domain logic tests
- Service mock tests
- Validation tests
- **Run Time:** < 5 seconds
- **Requirements:** None

### Integration Tests (Medium, Requires Cosmos DB)
- Database operations
- Service integration
- Real data workflows
- **Run Time:** 30-60 seconds
- **Requirements:** testsettings.json configured ✅

### Demo Tests (Slow, Requires All Services)
- Full workflow demonstrations
- Large dataset tests
- Conversational AI tests
- **Run Time:** 2-5 minutes
- **Requirements:** testsettings.json configured ✅

### API Tests (Medium, Requires Deployed API)
- HTTP endpoint tests
- API contract validation
- End-to-end API workflows
- **Run Time:** 1-2 minutes
- **Requirements:** Function key needed

---

## Quick Test Commands

```bash
# Run everything (may take 5-10 minutes)
dotnet test

# Run only fast unit tests (recommended for development)
dotnet test --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~Demo"

# Run integration tests only
dotnet test --filter "FullyQualifiedName~Integration"

# Run with verbose output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~EntityMatchingWorkflowIntegrationTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~EntityMatchingWorkflowIntegrationTests.CompleteMatchingWorkflow_ShouldSucceed"

# Generate code coverage report
dotnet test --collect:"XPlat Code Coverage"
```

---

## Environment Variables Reference

| Variable | Purpose | Required For | Example |
|----------|---------|--------------|---------|
| `API_BASE_URL` | Function App URL | API Tests | `https://entityaiapi.azurewebsites.net` |
| `API_FUNCTION_KEY` | Auth key for API | API Tests | `abc123...` |
| `SKIP_TEST_CLEANUP` | Keep test data | Debugging | `true` or `false` |

---

## Test Data Cleanup

By default, all tests clean up their test data after running. To keep test data for debugging:

```bash
# Windows (PowerShell)
$env:SKIP_TEST_CLEANUP = "true"

# Linux/Mac
export SKIP_TEST_CLEANUP=true
```

Then run tests:
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

**Note:** Remember to manually clean up test data from Cosmos DB afterward.

---

## Troubleshooting

### "Cosmos DB connection string not configured"

**Solution:** The testsettings.json file should already be configured. If you see this error:
1. Check that `testsettings.json` exists in `EntityMatching.Tests/`
2. Verify it's being copied to the output directory
3. Rebuild the test project: `dotnet build EntityMatching.Tests`

### "OpenAI API key not configured"

**Solution:** Already configured in testsettings.json. If you see this error:
1. Verify the OpenAI key in testsettings.json is valid
2. Check if the key has expired
3. Get a new key from OpenAI dashboard if needed

### Tests timeout

**Solution:**
1. Increase timeout in test code (some tests have 30-60 second timeouts)
2. Check Azure Cosmos DB is accessible
3. Verify network connectivity to Azure

### API tests return 401 Unauthorized

**Solution:**
1. Get a function key from Azure (see "Get Function App Key" above)
2. Set `API_FUNCTION_KEY` environment variable or update testsettings.json
3. Or temporarily disable authentication on the Function App for testing

### Integration tests fail with "Container not found"

**Solution:**
1. Verify all 6 Cosmos containers exist:
   ```bash
   az cosmosdb sql container list \
     --account-name entitymatchingdb \
     --resource-group entitymatchingai \
     --database-name EntityMatchingDB
   ```
2. Check the container names match in testsettings.json

---

## CI/CD Integration

### GitHub Actions

Add to your workflow:

```yaml
- name: Run Unit Tests
  run: dotnet test --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~Demo"

- name: Run Integration Tests
  run: dotnet test --filter "FullyQualifiedName~Integration"
  env:
    COSMOS_CONNECTION_STRING: ${{ secrets.COSMOS_CONNECTION_STRING }}
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
```

### Azure DevOps

Add test task:

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Tests'
  inputs:
    command: test
    projects: '**/*Tests.csproj'
    arguments: '--filter "FullyQualifiedName!~Integration"'
```

---

## Best Practices

1. **Run unit tests frequently** during development (fast feedback)
2. **Run integration tests** before committing (verify functionality)
3. **Run all tests** before creating a PR (comprehensive validation)
4. **Use SKIP_TEST_CLEANUP** when debugging test failures
5. **Keep testsettings.json** in .gitignore (already configured ✅)
6. **Monitor test execution time** - slow tests may indicate issues

---

## Test Coverage

To generate a code coverage report:

```bash
# Install coverage tool (once)
dotnet tool install --global dotnet-coverage

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# View results
# Coverage report will be in: TestResults/{guid}/coverage.cobertura.xml
```

---

## Summary

**Ready to Test:**
- ✅ testsettings.json configured with Azure deployment
- ✅ Cosmos DB connection string set
- ✅ OpenAI & Groq API keys configured
- ✅ API base URL updated to Azure Function App

**Recommended First Test:**
```bash
dotnet test --filter "FullyQualifiedName!~Integration&FullyQualifiedName!~Demo"
```

This runs all unit tests and should complete in under 5 seconds with all passing.

**Then try integration tests:**
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

This tests real Cosmos DB operations and should complete in 30-60 seconds.
