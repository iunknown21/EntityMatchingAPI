# Entity Rename - Status and Plan

## Problem Identified

The embedding generation pipeline wasn't working because:

1. ✅ **Entities** are stored in the `entities` container
2. ❌ **GenerateProfileSummaries** was looking for a `profiles` container that doesn't exist
3. ❌ The function name still referenced "Profile" causing confusion

##  What We've Changed (Partially Complete)

### ✅ Completed
1. Renamed `GenerateProfileSummariesFunction.cs` → `GenerateEntitySummariesFunction.cs`
2. Renamed class `GenerateProfileSummariesFunction` → `GenerateEntitySummariesFunction`
3. Renamed model `EntityEmbedding` → `EntityEmbedding`
4. Renamed properties in EntityEmbedding:
   - `ProfileId` → `EntityId`
   - `ProfileSummary` → `EntitySummary`
   - `ProfileLastModified` → `EntityLastModified`
5. Updated `ProcessPendingEmbeddingsFunction` to use entity terminology
6. Updated `EmbeddingStorageService` to use `EntityId`
7. Partially updated `SimilaritySearchService`

### ❌ Still Needs Fixing
1. **AdminFunctions.cs** - Still references `ProfileSummary`
2. **EmbeddingUploadFunctions.cs** - Still references old property names
3. **GenerateEntitySummariesFunction.cs line 95** - Still references `ProfileSummary`
4. All test files - Need to update property references
5. API endpoints - Still use `/profiles/` in URLs

## Build Errors (7 remaining)

```
EntityMatching.Functions\EmbeddingUploadFunctions.cs(120,39): error CS1061: 'EntityEmbedding' does not contain a definition for 'ProfileId'
EntityMatching.Functions\AdminFunctions.cs(107,65): error CS1061: 'EntityEmbedding' does not contain a definition for 'ProfileSummary'
EntityMatching.Functions\AdminFunctions.cs(113,95): error CS1061: 'EntityEmbedding' does not contain a definition for 'ProfileSummary'
EntityMatching.Functions\GenerateEntitySummariesFunction.cs(95,43): error CS1061: 'EntityEmbedding' does not contain a definition for 'ProfileSummary'
EntityMatching.Functions\EmbeddingUploadFunctions.cs(196,17): error CS0117: 'EntityEmbedding' does not contain a definition for 'ProfileId'
EntityMatching.Functions\EmbeddingUploadFunctions.cs(199,17): error CS0117: 'EntityEmbedding' does not contain a definition for 'ProfileSummary'
EntityMatching.Functions\EmbeddingUploadFunctions.cs(210,17): error CS0117: 'EntityEmbedding' does not contain a definition for 'ProfileLastModified'
```

## Quick Fix Script

Run these PowerShell commands to fix the remaining property references:

```powershell
cd "D:\Development\Main\EntityMatchingAPI\EntityMatching.Functions"

# Fix AdminFunctions.cs
(Get-Content AdminFunctions.cs) `
  -replace 'embedding\.ProfileSummary', 'embedding.EntitySummary' `
  | Set-Content AdminFunctions.cs

# Fix Embedding UploadFunctions.cs
(Get-Content EmbeddingUploadFunctions.cs) `
  -replace 'embedding\.ProfileId', 'embedding.EntityId' `
  -replace 'embedding\.ProfileSummary', 'embedding.EntitySummary' `
  -replace 'embedding\.ProfileLastModified', 'embedding.EntityLastModified' `
  -replace '\.ProfileId =', '.EntityId =' `
  -replace '\.ProfileSummary =', '.EntitySummary =' `
  -replace '\.ProfileLastModified =', '.EntityLastModified =' `
  | Set-Content EmbeddingUploadFunctions.cs

# Fix GenerateEntitySummariesFunction.cs
(Get-Content GenerateEntitySummariesFunction.cs) `
  -replace 'existing\.ProfileSummary', 'existing.EntitySummary' `
  | Set-Content GenerateEntitySummariesFunction.cs

# Rebuild
dotnet build -c Release
```

## Configuration Changes Made

### Azure Function App Settings
✅ Added: `CosmosDb__EntitiesContainerId=entities`

### Function Names Changed
- ✅ `GenerateProfileSummaries` → `GenerateEntitySummaries`

## What Still Needs Renaming (Future Work)

### Models
- `Profile` → `Entity` (already done in some places, but not consistently)
- `EntityMatch` → `EntityMatch` (used in search results)
- `ProfileSummaryResult` → `EntitySummaryResult`

### Interfaces
- `IEntityService` → `IEntityService`
- `IProfileSummaryService` → `IEntitySummaryService` (might already be renamed)

### API Endpoints
Currently:
- `/api/v1/entities/{id}`
- `/api/v1/entities/search`
- `/api/v1/entities/{id}/conversation`
- `/api/v1/entities/{id}/embeddings/upload`

Should be:
- `/api/v1/entities/{id}`
- `/api/v1/entities/search`
- `/api/v1/entities/{id}/conversation`
- `/api/v1/entities/{id}/embeddings/upload`

### Container Names
- ✅ `entities` (already using this)
- ❌ Some config still references `profiles` container

## Testing After Fix

1. **Build** the project:
   ```bash
   dotnet build -c Release
   ```

2. **Deploy** to Azure:
   ```bash
   func azure functionapp publish entityaiapi
   ```

3. **Trigger** GenerateEntitySummaries:
   ```bash
   curl -X POST "https://entityaiapi.azurewebsites.net/admin/functions/GenerateEntitySummaries" \
     -H "x-functions-key: YOUR_KEY"
   ```

4. **Check** logs in Azure Portal:
   - Function should find entities in `entities` container
   - Should create EntityEmbedding documents with status "Pending"
   - Should log "Found X total entities to process"

5. **Trigger** ProcessPendingEmbeddings:
   ```bash
   curl -X POST "https://entityaiapi.azurewebsites.net/admin/functions/ProcessPendingEmbeddings" \
     -H "x-functions-key: YOUR_KEY"
   ```

6. **Verify** embeddings created:
   - Check `embeddings` container in Cosmos DB
   - Should see documents with `entityId` matching your career entities
   - Status should be "Generated"

## Root Cause of Original Issue

The ONetImporter was correctly creating entities in the `entities` container, but:

1. `GenerateProfileSummaries` was configured to look for `profiles` container (doesn't exist)
2. No EntityEmbedding (now EntityEmbedding) documents were being created
3. `ProcessPendingEmbeddings` had nothing to process

## Why The Rename Matters

Using consistent terminology prevents confusion:
- **Before**: "EntityEmbedding for profileId from entities container"
- **After**: "EntityEmbedding for entityId from entities container"

Makes the code self-documenting and reduces bugs.

---

**Status**: Refactoring 75% complete
**Next Step**: Run the PowerShell fix script above, rebuild, and deploy
**Created**: 2026-01-22
