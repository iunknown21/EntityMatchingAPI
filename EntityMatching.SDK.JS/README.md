# EntityMatching SDK (JavaScript/TypeScript)

Privacy-first client SDK for [EntityMatchingAPI](https://api.bystorm.com) - Zero PII storage with client-side embedding generation.

## Features

- **🔒 Privacy-First**: Generate embeddings locally, upload only vectors (never text)
- **🎯 Semantic Search**: Find profiles using natural language queries
- **🔍 Attribute Filtering**: Combine semantic search with structured filters
- **💬 Conversational Profiling**: Build profiles through AI conversations
- **📊 TypeScript Support**: Full type definitions included

## Installation

```bash
npm install @EntityMatching/sdk
```

## Quick Start

### Initialize the Client

```typescript
import { EntityMatchingClient } from '@EntityMatching/sdk';

const client = new EntityMatchingClient({
  apiKey: 'your-api-key',              // From https://api.bystorm.com
  openaiKey: 'your-openai-key',        // Optional, for client-side embedding
  baseUrl: 'https://api.bystorm.com',  // Optional, defaults to this
});
```

### Privacy-First Resume Upload

The killer feature: upload resumes without sending text to the server.

```typescript
const resumeText = `
  Senior Software Engineer with 10 years experience in Python and AWS.
  Built machine learning pipelines processing 100M+ events/day.
`;

// Generate embedding locally, upload only the vector
await client.uploadResume(profileId, resumeText);

// ✅ Resume text NEVER left your device!
// ✅ Only 1536 numbers were sent to the server
// ✅ Even if hacked, attackers get meaningless data
```

### Search Profiles

```typescript
const results = await client.search.search({
  query: 'Senior Python engineer, AWS experience',
  limit: 10,
  minSimilarity: 0.7,
});

results.matches.forEach(match => {
  console.log(`Profile ${match.profileId}: ${match.similarityScore * 100}% match`);
});
```

### Advanced Search with Filters

```typescript
const results = await client.search.search({
  query: 'loves hiking and outdoor adventures',
  attributeFilters: {
    logicalOperator: 'And',
    filters: [
      { fieldPath: 'naturePreferences.hasPets', operator: 'IsTrue' },
      { fieldPath: 'naturePreferences.petTypes', operator: 'Contains', value: 'Dog' },
      { fieldPath: 'adventurePreferences.riskTolerance', operator: 'GreaterThan', value: 6 },
    ],
  },
  limit: 5,
  minSimilarity: 0.6,
});
```

### Conversational Profiling

```typescript
// Send a message to build a profile
const response = await client.conversations.sendMessage(profileId, {
  userId: 'user-123',
  message: 'They love hiking on weekends and enjoy trying new restaurants',
});

console.log('AI Response:', response.aiResponse);
console.log('Extracted Insights:', response.newInsights);
```

## API Reference

### EntityMatchingClient

Main client class for interacting with EntityMatchingAPI.

#### Constructor Options

```typescript
interface EntityMatchingClientOptions {
  apiKey: string;        // Required: API key from portal
  baseUrl?: string;      // Optional: API base URL
  openaiKey?: string;    // Optional: For client-side embedding
}
```

#### Properties

- `profiles` - Profile CRUD operations
- `embeddings` - Privacy-first vector upload
- `conversations` - Conversational profiling
- `search` - Semantic search with filters

#### Methods

##### `uploadResume(profileId, resumeText): Promise<void>`

Upload a resume with privacy-first approach. Generates embedding locally and uploads only the vector.

**Parameters:**
- `profileId` (string) - Profile ID to associate resume with
- `resumeText` (string) - Resume text (stays local, never sent to server)

**Example:**
```typescript
await client.uploadResume('profile-123', resumeText);
```

##### `generateEmbedding(text): Promise<number[]>`

Generate embedding for any text (not just resumes).

**Returns:** 1536-dimensional embedding vector

### Profiles Endpoint

```typescript
// List all profiles for a user
const profiles = await client.profiles.list('user-123');

// Get single profile
const profile = await client.profiles.get('profile-id');

// Create profile
const newProfile = await client.profiles.create({
  ownedByUserId: 'user-123',
  name: 'John Doe',
  bio: 'Software Engineer',
  isSearchable: true,
});

// Update profile
await client.profiles.update('profile-id', { bio: 'Updated bio' });

// Delete profile
await client.profiles.delete('profile-id');

// Find similar profiles
const similar = await client.profiles.getSimilar('profile-id', 10);
```

### Embeddings Endpoint

```typescript
// Upload pre-computed embedding
await client.embeddings.upload('profile-id', {
  embedding: [0.123, -0.456, ...], // 1536 numbers
  embeddingModel: 'text-embedding-3-small',
});
```

### Conversations Endpoint

```typescript
// Send message
const response = await client.conversations.sendMessage('profile-id', {
  userId: 'user-123',
  message: 'They love hiking and outdoor activities',
});

// Get conversation history
const history = await client.conversations.getHistory('profile-id');

// Delete conversation
await client.conversations.delete('profile-id');
```

### Search Endpoint

```typescript
const results = await client.search.search({
  query: 'Senior Python engineer',
  attributeFilters: { /* ... */ },
  limit: 10,
  minSimilarity: 0.7,
  enforcePrivacy: true,
});
```

## Privacy Guarantees

### What Gets Stored?

**Traditional Job Boards:**
- ❌ Full resume text (can be stolen)
- ❌ Personal information (name, email, address)
- ❌ GDPR compliance burden

**EntityMatching:**
- ✅ Only 1536-dimensional vector
- ✅ No personal information in vector
- ✅ Impossible to reconstruct original text
- ✅ Minimal GDPR requirements

### Cost Savings

Storing vectors instead of text:
- **87% smaller storage** (1536 floats vs. full documents)
- **92% lower costs** ($208/month vs. $2,623/month for 1000 profiles)

## Examples

See the [`examples/`](./examples) directory for complete examples:

- `upload-resume.ts` - Privacy-first resume upload
- `search-profiles.ts` - Semantic search with filters

## License

MIT

## Support

- **API Documentation**: https://api.bystorm.com/docs
- **GitHub Issues**: https://github.com/iunknown21/EntityMatchingAPI/issues
- **Email**: admin@bystorm.com
