# API Test Progress Summary

## Session Goal
Fix all failing tests to reach 100% pass rate for a clean project.

## Starting Point
- **Total Tests**: 149
- **Passing**: 134
- **Failing**: 15

### Initial Failing Tests
1. ❌ 9 API tests - connection refused (Functions host not running)
2. ❌ 3 AttributeSearch tests - orphaned embeddings issue
3. ❌ 2 search ranking tests - non-deterministic OpenAI embeddings
4. ❌ 1 scoring weight test - math precision issue

---

## Problems Fixed

### 1. ✅ Orphaned Embeddings (3 tests fixed)
**Problem**: Tests were creating embeddings with random GUIDs instead of proper IDs, causing cleanup failures and accumulating 1,466 orphaned embeddings.

**Fix**: Changed `Id = Guid.NewGuid().ToString()` to `Id = EntityEmbedding.GenerateId(profileId)` in `AttributeSearchIntegrationTests.cs`.

**Result**: All 3 AttributeSearch tests now passing after manual cleanup of Cosmos DB.

---

### 2. ✅ EntityService DI Registration (Dependency Injection)
**Problem**: EntityService wasn't registered with required string parameters (databaseId, containerId), causing `InvalidOperationException: Unable to resolve service for type 'System.String'`.

**Fix**: Changed from simple registration to factory pattern in `Program.cs`:
```csharp
services.AddScoped<IEntityService>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var databaseId = configuration["CosmosDb:DatabaseId"] ?? configuration["CosmosDb__DatabaseId"];
    var containerId = configuration["CosmosDb:EntitiesContainerId"] ?? configuration["CosmosDb__EntitiesContainerId"] ?? "profiles";
    var logger = sp.GetRequiredService<ILogger<EntityService>>();
    return new EntityService(cosmosClient, databaseId!, containerId, logger);
});
```

**Result**: Functions host starts successfully without DI errors.

---

### 3. ✅ Empty Request Body Crash
**Problem**: SearchEntities endpoint crashed when receiving empty request body, trying to deserialize empty string as JSON.

**Fix**: Added empty body check before deserialization in `SearchFunctions.cs`:
```csharp
if (string.IsNullOrWhiteSpace(requestBody))
{
    return CreateBadRequestResponse(req, "Request body is required");
}
```

**Result**: Empty query test now returns proper 400 BadRequest instead of crashing with 500.

---

### 4. ✅ Plain Text Error Responses
**Problem**: Error response helper methods returned plain text like "Resource not found" instead of JSON, causing test deserialization failures with "'R' is an invalid start of a value".

**Fix**: Updated `BaseApiFunction.cs` helper methods to return JSON:
```csharp
protected HttpResponseData CreateBadRequestResponse(HttpRequestData req, string message)
{
    var response = req.CreateResponse(HttpStatusCode.BadRequest);
    SetCorsHeaders(response);
    response.Headers.Add("Content-Type", "application/json");
    response.WriteString($"{{\"error\":\"{message}\"}}");
    return response;
}
```

**Result**: All API error responses now return proper JSON, eliminating parsing errors.

---

### 5. ✅ Test Expectation Mismatch
**Problem**: CreateProfile test expected HTTP 200 but function correctly returns HTTP 201 Created (REST standard).

**Fix**: Updated test expectation in `EntityMatchingApiTests.cs`:
```csharp
response.StatusCode.Should().Be(HttpStatusCode.Created); // Changed from OK
```

**Result**: Test now expects correct status code.

---

## Current Status

### API Tests with Functions Host Running
- **Total**: 9 tests
- **Passing**: 3 ✅
- **Failing**: 6 ❌

#### ✅ Passing (3)
1. `Api_GetProfile_WithInvalidId_ReturnsNotFound` - Returns 404 for invalid ID
2. `Api_SearchProfiles_WithEmptyQuery_ReturnsBadRequest` - Returns 400 for empty query
3. `Api_GetProfiles_WithoutUserId_ReturnsBadRequest` - Returns 400 for missing userId

#### ❌ Remaining Failures (6)
1. `Api_CreateProfile_ReturnsCreatedProfile` - Getting 400 BadRequest (validation error)
2. `Api_GetProfile_ReturnsProfile` - Getting 404 NotFound (profile doesn't exist)
3. `Api_DeleteProfile_RemovesProfile` - Getting 404 NotFound (profile doesn't exist)
4. `Api_UpdateProfile_UpdatesProfile` - Getting 400 BadRequest (validation error)
5. `Api_GetAllProfiles_ReturnsUserProfiles` - Getting 500 InternalServerError
6. `Api_CompleteWorkflow_CreateProfilesAndSearch_FindsMatches` - Getting 500 InternalServerError

**Note**: Manual curl test of CreateProfile endpoint succeeded, but automated test gets 400 error. Needs investigation of request differences.

---

## Overall Test Status

### All Tests
- **Total**: 149
- **Passing**: 140 (93.9%)
- **Failing**: 9 (6.1%)

### Breakdown by Category
- ✅ **Unit Tests**: All passing
- ✅ **AttributeSearch Integration**: All 5 tests passing (fixed!)
- ✅ **Entity Model Tests**: All 9 tests passing
- ⚠️ **API Integration Tests**: 3/9 passing (67%)
- ⚠️ **Search Ranking Tests**: 0/2 passing (non-deterministic OpenAI embeddings)
- ⚠️ **Scoring Weight Test**: 0/1 passing (math precision issue)

---

## Next Steps

### High Priority
1. **Investigate CreateProfile 400 error** - Compare automated test request vs successful manual curl request
2. **Fix downstream 404 errors** - Once CreateProfile works, GetProfile/DeleteProfile should pass
3. **Debug GetAllProfiles 500 error** - Check Functions terminal for error details

### Medium Priority
4. **Fix scoring weight test** - Investigate why weights sum to 1.05 instead of 1.0
5. **Relax search ranking assertions** - Change to assert profile is in top 3 instead of exactly #1

### Low Priority (Optional)
6. **Add logging** - More verbose logging in Functions to aid debugging
7. **Consider test isolation** - Tests may be interfering with each other

---

## Key Learnings

1. **Embedding ID Format Matters**: EntityEmbedding IDs must follow the `embedding_{profileId}` format for cleanup to work
2. **DI Registration Patterns**: Services requiring configuration strings need factory pattern registration
3. **API Error Format**: Always return JSON error responses, not plain text
4. **REST Conventions**: POST should return 201 Created, not 200 OK
5. **Request Validation**: Check for empty/null bodies before deserializing JSON

---

## Files Modified

### Functions
- `EntityMatching.Functions/Program.cs` - Fixed EntityService DI registration
- `EntityMatching.Functions/SearchFunctions.cs` - Added empty body check
- `EntityMatching.Functions/Common/BaseApiFunction.cs` - Fixed error responses to return JSON

### Tests
- `EntityMatching.Tests/Integration/AttributeSearchIntegrationTests.cs` - Fixed embedding ID generation
- `EntityMatching.Tests/Integration/EntityMatchingApiTests.cs` - Fixed status code expectation

### Documentation
- `TEST_FAILURE_ANALYSIS.md` - Detailed analysis of orphaned embeddings issue
- `FINAL_TEST_STATUS.md` - Complete test status breakdown
- `API_TEST_PROGRESS.md` - This file!

---

**Last Updated**: Session in progress
**Next Action**: Debug why CreateProfile test gets 400 error when manual curl succeeds
