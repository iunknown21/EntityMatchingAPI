# API Endpoints Renamed - Profiles → Entities

## Summary

All API endpoints have been updated from `/v1/profiles/` to `/v1/entities/` to reflect the consistent entity-based terminology throughout the system.

## Deployment

✅ **Deployed to**: `entityaiapi` Azure Function App
✅ **Resource Group**: `entitymatchingai`
✅ **Date**: 2026-01-22

## API Endpoint Changes

### Entity CRUD Operations

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `GET /v1/profiles` | `GET /v1/entities` | List all entities |
| `GET /v1/profiles/{profileId}` | `GET /v1/entities/{entityId}` | Get entity by ID |
| `POST /v1/profiles` | `POST /v1/entities` | Create new entity |
| `PUT /v1/profiles/{profileId}` | `PUT /v1/entities/{entityId}` | Update entity |
| `DELETE /v1/profiles/{profileId}` | `DELETE /v1/entities/{entityId}` | Delete entity |

### Conversation Endpoints

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `POST /v1/profiles/{profileId}/conversation` | `POST /v1/entities/{entityId}/conversation` | Send message |
| `GET /v1/profiles/{profileId}/conversation` | `GET /v1/entities/{entityId}/conversation` | Get history |
| `DELETE /v1/profiles/{profileId}/conversation` | `DELETE /v1/entities/{entityId}/conversation` | Clear history |

### Embedding Endpoints

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `POST /v1/profiles/{profileId}/embeddings/upload` | `POST /v1/entities/{entityId}/embeddings/upload` | Upload embedding |

### Search Endpoints

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `POST /v1/profiles/search` | `POST /v1/entities/search` | Semantic search |
| `GET /v1/profiles/{profileId}/similar` | `GET /v1/entities/{entityId}/similar` | Find similar |

### Metadata Endpoints

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `GET /v1/profiles/{profileId}/metadata` | `GET /v1/entities/{entityId}/metadata` | Get metadata |
| `PUT /v1/profiles/{profileId}/metadata` | `PUT /v1/entities/{entityId}/metadata` | Update metadata |

### Rating & Reputation Endpoints

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `GET /v1/profiles/{profileId}/ratings` | `GET /v1/entities/{entityId}/ratings` | Get ratings |
| `GET /v1/profiles/{profileId}/reputation` | `GET /v1/entities/{entityId}/reputation` | Get reputation |
| `POST /v1/profiles/{profileId}/reputation/recalculate` | `POST /v1/entities/{entityId}/reputation/recalculate` | Recalculate |

### Match Endpoints

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `GET /v1/profiles/{profileId}/matches/incoming` | `GET /v1/entities/{entityId}/matches/incoming` | Incoming matches |
| `GET /v1/profiles/{profileId}/matches/outgoing` | `GET /v1/entities/{entityId}/matches/outgoing` | Outgoing matches |
| `GET /v1/entities/{entityId}/mutual-matches` | `GET /v1/entities/{entityId}/mutual-matches` | Mutual matches |

### Admin Endpoints

| Old Endpoint | New Endpoint | Method |
|--------------|--------------|--------|
| `GET /admin/embeddings/{profileId}` | `GET /admin/embeddings/{entityId}` | Get embedding |
| `POST /admin/embeddings/{profileId}/regenerate` | `POST /admin/embeddings/{entityId}/regenerate` | Regenerate |

## Breaking Changes

⚠️ **Route Parameters**: All route parameters changed from `{profileId}` to `{entityId}`

### Impact on Existing Clients

If you have existing API clients (like the ONetImporter), you need to update:

1. **URL paths**: Change `/v1/profiles/` to `/v1/entities/`
2. **Route parameters**: Use `{entityId}` instead of `{profileId}`

### Example Updates

**Before:**
```bash
POST https://entityaiapi-apim.azure-api.net/v1/profiles
GET https://entityaiapi-apim.azure-api.net/v1/profiles/{id}
POST https://entityaiapi-apim.azure-api.net/v1/profiles/{id}/conversation
```

**After:**
```bash
POST https://entityaiapi-apim.azure-api.net/v1/entities
GET https://entityaiapi-apim.azure-api.net/v1/entities/{id}
POST https://entityaiapi-apim.azure-api.net/v1/entities/{id}/conversation
```

### JSON Response Changes

Response JSON still uses `profileId` field names for backwards compatibility with existing data models. Only the **URL paths** have changed.

**Example Response (unchanged):**
```json
{
  "id": "entity-123",
  "profileId": "entity-123",  // Still uses profileId in JSON
  "name": "Software Engineer",
  "entityType": 7
}
```

## Migration Guide for Clients

### JavaScript/TypeScript SDK

Update your API client base URLs:

```typescript
// Before
const baseUrl = 'https://entityaiapi-apim.azure-api.net/v1/profiles';

// After
const baseUrl = 'https://entityaiapi-apim.azure-api.net/v1/entities';
```

### ONetImporter (CareerService)

Update the EntityMatchingUploader class:

```csharp
// Before
var response = await _httpClient.PostAsJsonAsync(
    $"{_baseUrl}/v1/profiles?code={_apiKey}",
    profile);

// After
var response = await _httpClient.PostAsJsonAsync(
    $"{_baseUrl}/v1/entities?code={_apiKey}",
    entity);
```

### cURL Examples

**Create Entity:**
```bash
# Before
curl -X POST https://entityaiapi-apim.azure-api.net/v1/profiles \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -d '{"name": "Software Engineer", "entityType": 7}'

# After
curl -X POST https://entityaiapi-apim.azure-api.net/v1/entities \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -d '{"name": "Software Engineer", "entityType": 7}'
```

**Send Conversation Message:**
```bash
# Before
curl -X POST https://entityaiapi-apim.azure-api.net/v1/profiles/{id}/conversation \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -d '{"message": "Tell me about this career"}'

# After
curl -X POST https://entityaiapi-apim.azure-api.net/v1/entities/{id}/conversation \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -d '{"message": "Tell me about this career"}'
```

**Search:**
```bash
# Before
curl -X POST https://entityaiapi-apim.azure-api.net/v1/profiles/search \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -d '{"query": "software engineer", "limit": 10}'

# After
curl -X POST https://entityaiapi-apim.azure-api.net/v1/entities/search \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -d '{"query": "software engineer", "limit": 10}'
```

## Unchanged Endpoints

These endpoints were **NOT** changed:

- `/api/version` - Version info
- `/api/v1/matches/*` - Match request endpoints (don't reference profiles directly)
- `/api/v1/ratings/*` - Rating endpoints (standalone)
- `/api/admin/*` - Most admin endpoints

## Testing the Changes

### 1. Test Entity Creation
```bash
curl -X POST "https://entityaiapi.azurewebsites.net/api/v1/entities" \
  -H "Content-Type: application/json" \
  -H "x-functions-key: YOUR_KEY" \
  -d '{
    "ownedByUserId": "test-user",
    "name": "Test Career",
    "entityType": 7,
    "isSearchable": true
  }'
```

### 2. Test Conversation
```bash
curl -X POST "https://entityaiapi.azurewebsites.net/api/v1/entities/{entityId}/conversation" \
  -H "Content-Type: application/json" \
  -H "x-functions-key: YOUR_KEY" \
  -d '{
    "message": "Tell me about this career",
    "userId": "test-user"
  }'
```

### 3. Test Search
```bash
curl -X POST "https://entityaiapi.azurewebsites.net/api/v1/entities/search" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  -d '{
    "query": "software engineer with cloud experience",
    "limit": 10,
    "minSimilarity": 0.7
  }'
```

## Rollback Plan

If needed, you can rollback by:

1. Reverting the route changes in the Function files
2. Redeploying the previous version

The changes are backwards compatible at the data level - only the API routes changed.

## Related Changes

This API rename is part of the larger Profile → Entity refactoring:

- ✅ `EntityEmbedding` → `EntityEmbedding` model
- ✅ `GenerateProfileSummaries` → `GenerateEntitySummaries` function
- ✅ `/v1/profiles/*` → `/v1/entities/*` API endpoints
- ⏳ Future: Rename internal models like `Profile` → `Entity` (if needed)

## Documentation Updates Needed

Update the following documentation to reflect the new endpoints:

- [ ] API reference documentation
- [ ] SDK documentation (JavaScript, C#)
- [ ] ONetImporter usage guide
- [ ] Postman collections
- [ ] Integration tests

---

**Status**: ✅ COMPLETE
**Deployed**: 2026-01-22
**Breaking Change**: YES (URL paths only, JSON format unchanged)
**Migration Required**: YES (update client code to use new URLs)
