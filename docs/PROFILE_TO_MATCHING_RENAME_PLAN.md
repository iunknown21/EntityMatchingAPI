# EntityMatching → Matching Rename Plan

**Status**: Planned - To be executed after weekly tokens renew
**Created**: 2026-01-22
**Estimated Effort**: 4-6 hours

## Overview

Complete rename of EntityMatchingAPI to MatchingAPI to reflect the universal entity-based architecture. This service now handles Person, Job, Career, Major, Property, Product, Service, and Event entities - not just profiles.

## What We've Already Done ✅

1. **Container renamed**: `profiles` → `entities`
2. **API routes renamed**: `/v1/profiles` → `/v1/entities`
3. **Created Entity-based system**:
   - Entity model
   - IEntityService & EntityService
   - CareerEntity & MajorEntity
   - CareerSummaryStrategy & MajorSummaryStrategy
4. **Documentation updated**: All API docs reference `/v1/entities`

## What Remains: The "Profile" Naming

### Project Names
- [ ] `EntityMatching.Core` → `Matching.Core`
- [ ] `EntityMatching.Infrastructure` → `Matching.Infrastructure`
- [ ] `EntityMatching.Functions` → `Matching.Functions`
- [ ] `EntityMatching.Shared` → `Matching.Shared`
- [ ] `EntityMatching.Tests` → `Matching.Tests`
- [ ] `EntityMatching.SDK` → `Matching.SDK`
- [ ] `EntityMatching.SDK.JS` → `Matching.SDK.JS`

### Namespaces (in all .cs files)
- [ ] `EntityMatching.Core` → `Matching.Core`
- [ ] `EntityMatching.Infrastructure` → `Matching.Infrastructure`
- [ ] `EntityMatching.Shared` → `Matching.Shared`
- [ ] Update all `using` statements

### Azure Resources
- [ ] Function App: `entityaiapi` → `matchingapi` (or create new)
- [ ] Cosmos DB: `entitymatchingaidb` → `matchingdb` (optional - can keep)
- [ ] Database: `EntityMatchingDB` → `MatchingDB` (optional - can keep)
- [ ] Resource Group: `entitymatchingai` → `matching` (optional - can keep)

### Class Names
- [ ] `ProfileFunctions` → `EntityFunctions`
- [ ] `IEntityService` → Keep (legacy support) or remove
- [ ] `EntityService` → Keep (legacy support) or remove
- [ ] `EntityEmbedding` → `EntityEmbedding`
- [ ] `IProfileSummaryService` → `IEntitySummaryService`
- [ ] `ProfileSummaryService` → `EntitySummaryService`
- [ ] `ProfileSummaryResult` → `EntitySummaryResult`
- [ ] `EntityMatch` → `EntityMatch`
- [ ] `EntityRating` → `EntityRating`
- [ ] `EntityReputation` → `EntityReputation`

### File Names
- [ ] `ProfileFunctions.cs` → `EntityFunctions.cs`
- [ ] `EntityService.cs` → Keep for backward compatibility or remove
- [ ] `IEntityService.cs` → Keep for backward compatibility or remove
- [ ] `EntityEmbedding.cs` → `EntityEmbedding.cs`
- [ ] `EntityMatch.cs` → `EntityMatch.cs`
- [ ] `EntityRating.cs` → `EntityRating.cs`
- [ ] `EntityReputation.cs` → `EntityReputation.cs`
- [ ] All SDK files with "Profile" in name

### Configuration Keys
- [ ] `CosmosDb__ProfilesContainerId` → Already changed to `CosmosDb__EntitiesContainerId` ✅
- [ ] Any other config keys with "Profile"

### Documentation
- [ ] Update README.md title
- [ ] Update all docs that reference "EntityMatching"
- [ ] Update SDK documentation
- [ ] Update example code

### External References
- [ ] CareerService client (already updated to use `/entities`) ✅
- [ ] Any other services calling this API
- [ ] GitHub repo name (if applicable)

## Migration Strategy

### Option 1: Big Bang (Recommended)
Do everything at once during a maintenance window:
1. Rename all projects and namespaces
2. Update Azure resources
3. Deploy all changes
4. Update all client services

**Pros**: Clean break, no hybrid state
**Cons**: Requires coordination, higher risk

### Option 2: Gradual Migration
Keep both old and new naming:
1. Entity-based system is primary (done) ✅
2. Profile-based classes remain for backward compatibility
3. Gradually deprecate Profile classes over time

**Pros**: Lower risk, backward compatible
**Cons**: Confusing codebase with dual naming

### Option 3: New Service + Migration
Create new MatchingAPI alongside EntityMatchingAPI:
1. Deploy new service with clean naming
2. Migrate data and clients gradually
3. Deprecate old service

**Pros**: Zero downtime, safest
**Cons**: Most work, costs more (running two services)

## Recommended Approach: Option 1

Since no one is using the API yet (as you mentioned earlier), we should do a complete rename.

## Step-by-Step Execution Plan

### Phase 1: Local Rename (2 hours)
1. Rename project folders and .csproj files
2. Update namespaces in all .cs files (find/replace)
3. Rename classes (ProfileFunctions, EntityEmbedding, etc.)
4. Update all using statements
5. Build and fix any compilation errors
6. Run all tests

### Phase 2: Azure Resources (1 hour)
1. Create new Function App: `matchingapi`
2. Copy all app settings from `entityaiapi`
3. Update DNS/URLs if needed
4. Deploy to new function app
5. Test endpoints

### Phase 3: Update Clients (1 hour)
1. Update CareerService to use `matchingapi` URL
2. Update any other client services
3. Update SDK packages
4. Deploy client changes

### Phase 4: Documentation (30 min)
1. Update all markdown docs
2. Update README
3. Update SDK docs
4. Update examples

### Phase 5: Cleanup (30 min)
1. Delete old `entityaiapi` function app (after testing)
2. Remove any deprecated Profile classes
3. Final verification

## Breaking Changes

- ✅ API routes already changed: `/v1/profiles` → `/v1/entities`
- Container name already changed: `profiles` → `entities`
- Function App URL will change: `entityaiapi` → `matchingapi`
- NuGet packages (if published) need new names
- npm packages (SDK.JS) need new names

## Rollback Plan

1. Keep old `entityaiapi` function app for 30 days
2. Can quickly switch DNS back if needed
3. Git tags for reverting code changes
4. Database/container names unchanged (optional)

## Testing Checklist

- [ ] All API endpoints work on new function app
- [ ] Entity creation (Person, Job, Career, Major, etc.)
- [ ] Entity retrieval by ID
- [ ] Entity search
- [ ] Embedding generation
- [ ] Similarity search
- [ ] Mutual matching
- [ ] Conversation features
- [ ] Rating/reputation features
- [ ] Health check

## Cost Impact

- New Function App: ~$0 (consumption plan)
- If keeping both during transition: 2x costs temporarily
- Cosmos DB: Same (using same database)

## Timeline

- **Preparation**: 30 min (review this plan)
- **Execution**: 4-5 hours
- **Testing**: 1 hour
- **Total**: ~6 hours

## Notes

- The entity-based architecture is already implemented and working ✅
- Only naming needs to change, not functionality
- Can be done in one session after tokens renew
- Low risk since no production users yet

## Related Issues

- Profile model still exists (but now has entityType field)
- Consider removing Profile model entirely vs keeping for backward compat
- SDK packages need major version bump (breaking changes)

---

**Next Steps**: Execute this plan after weekly token renewal
