# Test Failure Analysis - AttributeSearchIntegrationTests

## Root Cause Identified

The 3 failing AttributeSearch integration tests are failing due to **1466 orphaned embeddings** in the Cosmos DB `embeddings` container from previous test runs.

## The Problem

### 1. Incorrect Embedding ID Generation in Tests

**Issue**: Tests were creating embeddings with random GUIDs instead of using the proper ID format:

```csharp
// WRONG - What tests were doing:
var embedding = new EntityEmbedding
{
    Id = Guid.NewGuid().ToString(),  // ❌ Random GUID
    ProfileId = profile.Id.ToString(),
    ...
};

// CORRECT - What they should do:
var embedding = new EntityEmbedding
{
    Id = EntityEmbedding.GenerateId(profile.Id.ToString()),  // ✅ "embedding_{profileId}"
    ProfileId = profile.Id.ToString(),
    ...
};
```

### 2. Silent Cleanup Failures

The test cleanup code calls:
```csharp
await _embeddingStorageService.DeleteEmbeddingAsync(profileId);
```

This method expects embeddings to have ID = `"embedding_{profileId}"` (see `EntityEmbedding.GenerateId()` in `EntityEmbedding.cs:103`).

When it tries to delete an embedding with a random GUID ID, it gets a `404 Not Found`, logs a warning, and **continues without error**. This caused 1466 orphaned embeddings to accumulate over time.

### 3. Search Performance Impact

When tests run searches with `minSimilarity = 0.0`, ALL 1466 embeddings become candidates. The search then:
1. Tries to fetch profiles for all 1466 embeddings
2. Most profiles don't exist (404 errors)
3. Skips those candidates
4. Eventually returns 0 matches because the test profile gets lost in the noise

## Fix Applied

✅ **Fixed embedding ID generation** in `AttributeSearchIntegrationTests.cs`:
- Changed all 5 occurrences from `Guid.NewGuid().ToString()` to `EntityEmbedding.GenerateId(profile.Id.ToString())`
- Future test runs will now properly clean up their embeddings

## Still TODO - Manual Cleanup Required

⚠️ **The 1466 orphaned embeddings are still in Cosmos DB and need to be manually deleted.**

### Option 1: Azure Portal (Recommended)
1. Open Azure Portal
2. Navigate to your Cosmos DB account
3. Go to Data Explorer
4. Select the `embeddings` container
5. Delete all documents (or just the orphaned ones)

### Option 2: Cosmos DB Data Explorer
Run a query to find orphaned embeddings (embeddings whose profile IDs don't exist in the entities container), then delete them.

### Option 3: Create a cleanup utility
Create a simple C# console app that:
1. Connects to Cosmos DB
2. Queries all embeddings
3. For each embedding, checks if the profile exists
4. Deletes embeddings with non-existent profiles

## Test Status After Fix

### Failing Tests (3):
- `HybridSearch_SemanticAndAttributeFilters_FindsCorrectMatch` - affected by orphaned embeddings
- `AttributeSearch_OrLogic_ReturnsMultipleMatches` - affected by orphaned embeddings
- `AttributeSearch_PublicField_SearchableByAnonymous` - affected by orphaned embeddings

### Passing Tests (2):
- `AttributeSearch_IsSearchableFalse_ProfileNotReturned` - expects empty results, so orphaned embeddings don't affect it
- `AttributeSearch_PrivateField_NotSearchableByAnonymous` - expects empty results, so orphaned embeddings don't affect it

## Verification

Privacy logic is **working correctly**:
- ✅ `IsFieldVisibleToUser('naturePreferences.hasPets', null) = True` for Public fields
- ✅ Privacy settings are correctly serialized/deserialized
- ✅ Field visibility map is correctly populated

The tests are failing purely due to the orphaned embeddings pollution, not due to any logic bugs.

## Recommended Next Steps

1. **Clean up Cosmos DB embeddings container** (delete all 1466 orphaned embeddings)
2. **Run tests again** - they should pass with a clean database
3. **Consider adding a test helper** to clean up orphaned embeddings automatically before test runs
4. **Monitor for similar issues** in other integration test files

## Other Integration Test Files

✅ **Checked other integration test files** - they are using the correct ID format:
- `EntityMatchingWorkflowIntegrationTests.cs` - ✅ Uses `EntityEmbedding.GenerateId(profileId)`
- `EmbeddingStorageServiceIntegrationTests.cs` - ✅ Uses correct format
- `EmbeddingUploadIntegrationTests.cs` - ✅ Uses correct format

The embedding ID bug was **only in AttributeSearchIntegrationTests.cs**, which has now been fixed.
