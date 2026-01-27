# ProfileMatching.Tests

Comprehensive test suite for the ProfileMatchingAPI, ensuring quality and reliability for licensed usage.

## Test Structure

```
ProfileMatching.Tests/
├── Services/                  # Unit tests for service layer
│   ├── ProfileServiceTests.cs
│   ├── ProfileSummaryServiceTests.cs
│   ├── ConversationServiceTests.cs (planned)
│   └── EmbeddingStorageServiceTests.cs (planned)
│
├── Integration/               # Integration tests (2 approaches)
│   ├── Service-Level (Direct service calls)
│   │   ├── ProfileServiceIntegrationTests.cs
│   │   ├── EmbeddingStorageServiceIntegrationTests.cs
│   │   ├── ConversationServiceIntegrationTests.cs
│   │   └── ProfileMatchingWorkflowIntegrationTests.cs
│   │
│   ├── API-Level (HTTP calls to Functions)
│   │   ├── ProfileMatchingApiTests.cs
│   │   └── ApiTestHelper.cs
│   │
│   └── ProfileFunctionsTests.cs (legacy)
│
└── Helpers/                   # Test utilities and data factories
    └── TestDataFactory.cs
```

## Testing Approaches

### Service-Level Integration Tests
**Direct service instantiation** - Fast, focused, great for development
```
Test → Services → Cosmos DB / OpenAI
```
- Requires: OpenAI key in testsettings.json
- Speed: ~10-15 seconds
- Use for: Development, debugging, TDD

### API-Level Integration Tests
**HTTP calls to Functions app** - Complete stack validation
```
Test → HTTP → Azure Functions → Services → Cosmos DB / OpenAI
```
- Requires: Functions app running (local or Azure)
- Speed: ~20-30 seconds
- Use for: Pre-commit, deployment validation, CI/CD

**See [API_TESTING_GUIDE.md](./API_TESTING_GUIDE.md) for detailed instructions**

## Testing Frameworks

- **xUnit** - Test framework
- **Moq** - Mocking framework for dependencies
- **FluentAssertions** - Readable, chainable assertions
- **Microsoft.NET.Test.Sdk** - Test execution
- **coverlet.collector** - Code coverage

## Running Tests

### Run All Tests
```bash
cd ProfileMatching.Tests
dotnet test
```

### Run with Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run Specific Test Class
```bash
dotnet test --filter ProfileServiceTests
```

### Run Specific Test Method
```bash
dotnet test --filter "ProfileServiceTests.GetProfileAsync_WithValidId_ReturnsProfile"
```

## Test Categories

### Unit Tests (Services/)

**ProfileServiceTests** - 15+ tests covering:
- ✅ CRUD operations (Create, Read, Update, Delete)
- ✅ Ownership validation
- ✅ Profile search functionality
- ✅ Timestamp handling
- ✅ Error handling (not found, cosmos exceptions)

**ProfileSummaryServiceTests** - 15+ tests covering:
- ✅ Summary generation from profiles
- ✅ All preference category inclusion
- ✅ Accessibility and dietary restrictions
- ✅ Conversation context integration
- ✅ Metadata tracking (word count, categories)
- ✅ Comprehensive profile handling

### Integration Tests (Integration/)

**ProfileFunctionsTests** - 20+ tests covering:
- ✅ HTTP OPTIONS (CORS preflight)
- ✅ GET /api/v1/profiles (list)
- ✅ GET /api/v1/profiles/{id} (retrieve)
- ✅ POST /api/v1/profiles (create)
- ✅ PUT /api/v1/profiles/{id} (update)
- ✅ DELETE /api/v1/profiles/{id} (delete)
- ✅ Request validation
- ✅ Error handling
- ✅ Ownership verification

**EmbeddingStorageServiceIntegrationTests** - Tests covering:
- ✅ Get/Upsert/Delete embedding operations
- ✅ Query by status
- ✅ Count by status
- ✅ Edge cases (long summaries, metadata)

**ProfileMatchingWorkflowIntegrationTests** - End-to-end workflow tests:
- ✅ Complete workflow: Create profiles → Generate summaries → Generate embeddings → Search
- ✅ Profile similarity search (profile-to-profile matching)
- ✅ Text query-based search
- ✅ Tests with diverse profile types:
  - Outdoor Adventure Enthusiasts
  - Artistic Introverts
  - Tech Enthusiasts
  - Social Butterflies
  - Health & Wellness Advocates
- ✅ Validates similarity scores and ranking
- ✅ Tests cross-type dissimilarity

## Test Helpers

### TestDataFactory

Provides factory methods for creating test data consistently:

```csharp
using ProfileMatching.Tests.Helpers;

// Create minimal profile
var profile = TestDataFactory.CreateMinimalProfile(userId: "user-123");

// Create comprehensive profile with all preferences
var fullProfile = TestDataFactory.CreateCompleteProfile(userId: "user-123");

// Create specialized profiles with distinct characteristics
var outdoorProfile = TestDataFactory.CreateOutdoorAdventureProfile(userId, "Alex Hiker");
var artistProfile = TestDataFactory.CreateArtisticIntrovertProfile(userId, "Morgan Artist");
var techProfile = TestDataFactory.CreateTechEnthusiastProfile(userId, "Casey Coder");
var socialProfile = TestDataFactory.CreateSocialButterflyProfile(userId, "Jordan Party");
var wellnessProfile = TestDataFactory.CreateHealthWellnessProfile(userId, "Taylor Zen");

// Create specific preferences
var stylePrefs = TestDataFactory.CreateStylePreferences();
var giftPrefs = TestDataFactory.CreateGiftPreferences();

// Create conversation context
var conversation = TestDataFactory.CreateConversationContext(
    profileId: "profile-id",
    chunksCount: 5,
    insightsCount: 3
);

// Create embeddings
var embedding = TestDataFactory.CreateProfileEmbedding(profileId: "profile-id");
var embeddingWithVector = TestDataFactory.CreateProfileEmbeddingWithVector(profileId: "profile-id");
var vector = TestDataFactory.CreateTestEmbeddingVector(dimensions: 1536);
```

## Test Coverage Goals

Target: **80%+ code coverage** for all components

Current Coverage:
- ✅ ProfileService: 85%+
- ✅ ProfileSummaryService: 90%+
- ✅ ProfileFunctions: 75%+
- ⏳ ConversationService: Pending
- ⏳ EmbeddingStorageService: Pending
- ⏳ ConversationFunctions: Pending
- ⏳ GenerateProfileSummariesFunction: Pending

## Writing New Tests

### Example: Service Unit Test

```csharp
[Fact]
public async Task MethodName_WithScenario_ExpectedBehavior()
{
    // Arrange
    var mockDependency = new Mock<IDependency>();
    mockDependency.Setup(d => d.MethodAsync(It.IsAny<string>()))
        .ReturnsAsync(expectedResult);

    var service = new MyService(mockDependency.Object, _mockLogger.Object);

    // Act
    var result = await service.MethodUnderTest("input");

    // Assert
    result.Should().NotBeNull();
    result.Property.Should().Be("expected value");
    mockDependency.Verify(d => d.MethodAsync("input"), Times.Once);
}
```

### Example: Azure Function Integration Test

```csharp
[Fact]
public async Task FunctionName_WithValidInput_ReturnsExpectedStatus()
{
    // Arrange
    var mockRequest = CreateMockHttpRequest("POST", "{\"data\":\"value\"}");

    _mockService.Setup(s => s.ProcessAsync(It.IsAny<Data>()))
        .ReturnsAsync(expectedResult);

    // Act
    var response = await _function.EndpointName(mockRequest.Object);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    _mockService.Verify(s => s.ProcessAsync(It.IsAny<Data>()), Times.Once);
}
```

## Best Practices

### Test Naming
Use the pattern: `MethodName_WithScenario_ExpectedBehavior`
- ✅ `GetProfileAsync_WithValidId_ReturnsProfile`
- ✅ `CreateProfile_WithoutName_ReturnsBadRequest`
- ❌ `TestGetProfile`
- ❌ `Test1`

### Arrange-Act-Assert (AAA)
Always structure tests with clear sections:
```csharp
// Arrange - Set up test data and mocks

// Act - Execute the method being tested

// Assert - Verify expected outcomes
```

### FluentAssertions
Use FluentAssertions for readable, maintainable assertions:
```csharp
// ✅ Good
result.Should().NotBeNull();
result.Name.Should().Be("Expected Name");
result.Items.Should().HaveCount(3);
result.Items.Should().OnlyContain(i => i.IsActive);

// ❌ Avoid
Assert.NotNull(result);
Assert.Equal("Expected Name", result.Name);
Assert.Equal(3, result.Items.Count);
```

### Mock Verification
Always verify important interactions:
```csharp
// Verify method was called
_mockService.Verify(s => s.MethodAsync(expectedParam), Times.Once);

// Verify method was never called
_mockService.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
```

### Test Independence
Each test should be independent and not rely on other tests:
- ✅ Use fresh mocks for each test
- ✅ Use TestDataFactory for consistent data
- ❌ Don't share state between tests
- ❌ Don't assume test execution order

## Continuous Integration

Tests run automatically on:
- Every commit
- Every pull request
- Before deployment

All tests must pass before merging to main branch.

## Integration Test Configuration

### Prerequisites for ProfileMatchingWorkflowIntegrationTests

These tests require:
1. **Cosmos DB Connection** - For profile and embedding storage
2. **OpenAI API Key** - For generating embeddings

### Configuration Setup

Configure `testsettings.json` in the test project:

```json
{
  "CosmosDb": {
    "ConnectionString": "YOUR_COSMOS_DB_CONNECTION_STRING",
    "DatabaseId": "ProfileMatchingTestDB",
    "ProfilesContainerId": "profiles",
    "ConversationsContainerId": "conversations",
    "EmbeddingsContainerId": "embeddings"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY",
    "EmbeddingModel": "text-embedding-3-small",
    "EmbeddingDimensions": "1536",
    "MaxRetries": "3"
  }
}
```

### Running Integration Tests

```bash
# Run all integration tests
dotnet test --filter FullyQualifiedName~Integration

# Run only workflow tests
dotnet test --filter FullyQualifiedName~ProfileMatchingWorkflowIntegrationTests

# Run specific workflow test
dotnet test --filter "FullyQualifiedName~CompleteWorkflow_CreateProfilesGenerateEmbeddingsAndSearch"
```

### Cost Considerations

**OpenAI API Costs:**
- `text-embedding-3-small`: ~$0.00002 per 1K tokens
- Each workflow test creates 7 profiles: ~0.014 - 0.035 cents per run
- Query search test creates 3 profiles + 3 queries: ~0.012 - 0.020 cents per run

**Cosmos DB Costs:**
- Uses serverless billing
- Tests automatically clean up after themselves
- Minimal cost impact

## License Testing

Since this API is intended for licensing to other companies, tests should:
- ✅ Cover all public API endpoints
- ✅ Verify authentication and authorization
- ✅ Test rate limiting and quotas
- ✅ Validate error responses
- ✅ Ensure CORS configuration
- ✅ Test subscription tier enforcement
- ✅ Validate vector similarity search accuracy
- ✅ Test embedding generation and storage

## Support

For questions about tests:
- Review existing tests for examples
- Check this README for patterns
- Refer to xUnit, Moq, and FluentAssertions documentation

---

**Test Coverage is Quality Assurance**

Comprehensive testing ensures the ProfileMatchingAPI is reliable, secure, and ready for production use by licensed clients.
