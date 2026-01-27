# JavaScript/TypeScript SDK Guide

Complete guide to using the EntityMatching JavaScript/TypeScript SDK.

## Table of Contents

1. [Installation](#installation)
2. [Configuration](#configuration)
3. [Profile Management](#profile-management)
4. [Privacy-First Resume Upload](#privacy-first-resume-upload)
5. [Conversational Profiling](#conversational-profiling)
6. [Semantic Search](#semantic-search)
7. [Advanced Search with Filters](#advanced-search-with-filters)
8. [Error Handling](#error-handling)
9. [TypeScript Support](#typescript-support)
10. [Examples](#examples)

---

## Installation

```bash
npm install @EntityMatching/sdk
```

**Requirements:**
- Node.js 16+ or modern browser
- TypeScript 4.5+ (optional, for type safety)

---

## Configuration

### Basic Setup

```javascript
import { EntityMatchingClient } from '@EntityMatching/sdk';

const client = new EntityMatchingClient({
  apiKey: 'your-api-key-here',
  baseUrl: 'https://EntityMatching-apim.azure-api.net/v1'
});
```

### With OpenAI for Client-Side Embeddings

```javascript
const client = new EntityMatchingClient({
  apiKey: 'your-EntityMatching-api-key',
  baseUrl: 'https://EntityMatching-apim.azure-api.net/v1',
  openaiKey: 'your-openai-api-key' // For client-side embedding generation
});
```

### Environment Variables (Recommended)

```.env
PROFILE_MATCHING_API_KEY=your-api-key-here
PROFILE_MATCHING_BASE_URL=https://EntityMatching-apim.azure-api.net/v1
OPENAI_API_KEY=your-openai-key-here
```

```javascript
const client = new EntityMatchingClient({
  apiKey: process.env.PROFILE_MATCHING_API_KEY,
  baseUrl: process.env.PROFILE_MATCHING_BASE_URL,
  openaiKey: process.env.OPENAI_API_KEY
});
```

---

## Profile Management

### Create a Profile

```javascript
const profile = await client.profiles.create({
  ownedByUserId: 'user-123',
  name: 'Alice Johnson',
  bio: 'Software engineer passionate about AI and machine learning',
  isSearchable: true,
  createdAt: new Date().toISOString(),
  lastModified: new Date().toISOString()
});

console.log('Profile created:', profile.id);
```

### Get All Profiles for a User

```javascript
const profiles = await client.profiles.list('user-123');

profiles.forEach(profile => {
  console.log(`- ${profile.name} (${profile.id})`);
});
```

### Get a Specific Profile

```javascript
const profile = await client.profiles.get('profile-id-here');

console.log(`Name: ${profile.name}`);
console.log(`Bio: ${profile.bio}`);
```

### Update a Profile

```javascript
const updated = await client.profiles.update('profile-id', {
  bio: 'Updated bio with new information',
  lastModified: new Date().toISOString(),
  preferences: {
    entertainment: {
      movieGenres: ['Sci-Fi', 'Drama', 'Thriller'],
      musicGenres: ['Rock', 'Jazz', 'Electronic']
    },
    adventure: {
      riskTolerance: 7,
      noveltySeeking: 8
    }
  }
});
```

### Delete a Profile

```javascript
await client.profiles.delete('profile-id');
console.log('Profile deleted');
```

### Get Similar Profiles

```javascript
const similarProfiles = await client.profiles.getSimilar('profile-id', 10);

similarProfiles.forEach(match => {
  console.log(`${match.profileId}: ${(match.similarityScore * 100).toFixed(1)}% similar`);
});
```

---

## Privacy-First Resume Upload

### Complete Example (Browser)

```html
<!DOCTYPE html>
<html>
<head>
  <title>Privacy-First Resume Upload</title>
</head>
<body>
  <h1>Upload Your Resume Safely</h1>

  <textarea id="resume" rows="10" cols="50"
    placeholder="Paste your resume here..."></textarea>
  <br>
  <button onclick="uploadResume()">Upload (Privacy-First)</button>

  <div id="status"></div>

  <script type="module">
    import { EntityMatchingClient } from '@EntityMatching/sdk';

    const client = new EntityMatchingClient({
      apiKey: 'your-api-key',
      baseUrl: 'https://EntityMatching-apim.azure-api.net/v1',
      openaiKey: 'your-openai-key'
    });

    window.uploadResume = async function() {
      const resumeText = document.getElementById('resume').value;
      const statusDiv = document.getElementById('status');

      statusDiv.innerHTML = '⏳ Generating embedding locally...';

      try {
        // Step 1: Create profile
        const profile = await client.profiles.create({
          ownedByUserId: 'user-123',
          name: 'Anonymous User',
          isSearchable: true
        });

        // Step 2: Generate embedding and upload vector only
        await client.uploadResume(profile.id, resumeText);

        statusDiv.innerHTML = `
          ✅ Success! Your resume is secure.<br>
          📝 Resume text: <strong>Stayed in your browser</strong><br>
          📊 Vector uploaded: <strong>Yes (1536 numbers)</strong><br>
          🔒 Privacy: <strong>100% Protected</strong><br>
          Profile ID: ${profile.id}
        `;
      } catch (error) {
        statusDiv.innerHTML = `❌ Error: ${error.message}`;
      }
    };
  </script>
</body>
</html>
```

### Node.js Backend Example

```javascript
const fs = require('fs');

// Read resume from file
const resumeText = fs.readFileSync('./resume.txt', 'utf-8');

// Create profile
const profile = await client.profiles.create({
  ownedByUserId: 'user-456',
  name: 'John Smith',
  isSearchable: true
});

// Upload resume (privacy-first)
await client.uploadResume(profile.id, resumeText);

console.log('✅ Resume uploaded as vector embedding');
console.log('📄 Original text: NOT stored on server');
console.log('🔒 Privacy: Maximum');
```

---

## Conversational Profiling

Build profiles through natural conversations:

### Start a Conversation

```javascript
const response = await client.conversations.sendMessage(
  'profile-id',
  'user-123',
  'I really enjoy hiking and rock climbing. I go out almost every weekend!'
);

console.log('AI Response:', response.message);

// AI extracts insights automatically
response.insights.forEach(insight => {
  console.log(`- ${insight.category}: ${insight.content} (${insight.confidence})`);
});
```

### Multi-Turn Conversation

```javascript
async function buildProfileThroughChat(profileId, userId) {
  const questions = [
    "What do you like to do in your free time?",
    "What kind of movies do you enjoy?",
    "Are you more of an introvert or extrovert?",
    "What are your career goals?"
  ];

  for (const question of questions) {
    console.log(`\nQ: ${question}`);
    const userAnswer = await getUserInput(); // Your input method

    const response = await client.conversations.sendMessage(
      profileId,
      userId,
      userAnswer
    );

    console.log(`AI: ${response.message}`);

    // Insights are automatically added to profile
    if (response.insights.length > 0) {
      console.log('📝 Extracted insights:');
      response.insights.forEach(i => {
        console.log(`  - ${i.content} (${i.confidence})`);
      });
    }
  }
}
```

### Get Conversation History

```javascript
const conversation = await client.conversations.get('profile-id');

console.log(`Messages: ${conversation.messages.length}`);
console.log(`Insights extracted: ${conversation.insights.length}`);

conversation.messages.forEach(msg => {
  console.log(`${msg.role}: ${msg.content}`);
});
```

### Clear Conversation

```javascript
await client.conversations.delete('profile-id');
console.log('Conversation cleared');
```

---

## Semantic Search

### Basic Search

```javascript
const results = await client.search.search({
  query: "Full-stack developer with React and Node.js experience",
  limit: 10,
  minSimilarity: 0.7
});

console.log(`Found ${results.totalMatches} matches in ${results.metadata.searchDurationMs}ms`);

results.matches.forEach((match, index) => {
  console.log(`${index + 1}. Profile ${match.profileId}: ${(match.similarityScore * 100).toFixed(1)}% match`);
});
```

### Privacy-Protected Search (Recruiter View)

```javascript
const results = await client.search.search({
  query: "Senior software engineer, Python, AWS, 10+ years",
  requestingUserId: 'company-recruiter-456',
  enforcePrivacy: true, // Only returns profile IDs
  limit: 20,
  minSimilarity: 0.75
});

console.log('🔒 Privacy mode: Only profile IDs returned');
results.matches.forEach(match => {
  console.log(`Profile #${match.profileId.substring(0, 8)}: ${(match.similarityScore * 100).toFixed(1)}% match`);
  // Name, email, phone are NOT included
});
```

---

## Advanced Search with Filters

### Combine Semantic + Structured Search

```javascript
const results = await client.search.search({
  // Semantic part
  query: "loves outdoor activities and adventure sports",

  // Structured filters
  attributeFilters: {
    logicalOperator: 'AND',
    filters: [
      {
        fieldPath: 'preferences.adventure.riskTolerance',
        operator: 'GreaterThan',
        value: 6
      },
      {
        fieldPath: 'preferences.nature.outdoorActivities',
        operator: 'Contains',
        value: 'Hiking'
      },
      {
        fieldPath: 'location.city',
        operator: 'Equals',
        value: 'Denver'
      }
    ]
  },

  minSimilarity: 0.6,
  limit: 15
});
```

### Complex Filter Groups (OR Logic)

```javascript
const results = await client.search.search({
  query: "experienced software engineer",

  attributeFilters: {
    logicalOperator: 'OR',
    filters: [
      {
        fieldPath: 'skills',
        operator: 'Contains',
        value: 'Python'
      },
      {
        fieldPath: 'skills',
        operator: 'Contains',
        value: 'JavaScript'
      },
      {
        fieldPath: 'skills',
        operator: 'Contains',
        value: 'Java'
      }
    ]
  },

  minSimilarity: 0.7,
  limit: 20
});
```

### Nested Filter Groups

```javascript
const results = await client.search.search({
  query: "looking for travel companions",

  attributeFilters: {
    logicalOperator: 'AND',
    filters: [
      // Must be in specific location
      {
        fieldPath: 'location.country',
        operator: 'Equals',
        value: 'USA'
      }
    ],
    subGroups: [
      // AND (has high risk tolerance OR loves adventure)
      {
        logicalOperator: 'OR',
        filters: [
          {
            fieldPath: 'preferences.adventure.riskTolerance',
            operator: 'GreaterThan',
            value: 7
          },
          {
            fieldPath: 'preferences.adventure.noveltySeeking',
            operator: 'GreaterThan',
            value: 8
          }
        ]
      }
    ]
  }
});
```

---

## Error Handling

### Basic Try-Catch

```javascript
try {
  const profile = await client.profiles.get('invalid-id');
} catch (error) {
  if (error.response?.status === 404) {
    console.log('Profile not found');
  } else if (error.response?.status === 401) {
    console.log('Invalid API key');
  } else if (error.response?.status === 429) {
    console.log('Rate limit exceeded');
  } else {
    console.error('Error:', error.message);
  }
}
```

### Retry Logic

```javascript
async function withRetry(fn, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await fn();
    } catch (error) {
      if (i === maxRetries - 1) throw error;

      // Exponential backoff
      const delay = Math.pow(2, i) * 1000;
      console.log(`Retry ${i + 1}/${maxRetries} after ${delay}ms...`);
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }
}

// Usage
const profile = await withRetry(() =>
  client.profiles.get('profile-id')
);
```

---

## TypeScript Support

### Full Type Safety

```typescript
import {
  EntityMatchingClient,
  Profile,
  SearchRequest,
  SearchResult,
  ConversationResponse
} from '@EntityMatching/sdk';

const client = new EntityMatchingClient({
  apiKey: process.env.API_KEY!,
  baseUrl: process.env.BASE_URL!
});

// Type-safe profile creation
const profile: Profile = await client.profiles.create({
  ownedByUserId: 'user-123',
  name: 'Alice Johnson',
  bio: 'Engineer',
  isSearchable: true,
  createdAt: new Date().toISOString(),
  lastModified: new Date().toISOString()
});

// Type-safe search
const searchRequest: SearchRequest = {
  query: "Python developer",
  minSimilarity: 0.7,
  limit: 10,
  enforcePrivacy: true
};

const results: SearchResult = await client.search.search(searchRequest);

// TypeScript ensures you can't misspell fields
results.matches.forEach(match => {
  console.log(match.profileId); // ✅ Valid
  // console.log(match.profileID); // ❌ TypeScript error
});
```

### Custom Types

```typescript
interface MyAppProfile extends Profile {
  customField?: string;
  appMetadata?: {
    lastLogin: string;
    subscription: 'free' | 'premium';
  };
}

const profile: MyAppProfile = await client.profiles.get('id');
```

---

## Examples

### Example 1: Job Board Integration

```javascript
class JobMatchingService {
  constructor(apiKey) {
    this.client = new EntityMatchingClient({ apiKey });
  }

  async createCandidateProfile(userId, resume) {
    // Create profile
    const profile = await this.client.profiles.create({
      ownedByUserId: userId,
      name: 'Anonymous Candidate',
      isSearchable: true
    });

    // Upload resume (privacy-first)
    await this.client.uploadResume(profile.id, resume);

    return profile.id;
  }

  async findCandidates(jobDescription, requiredSkills) {
    const results = await this.client.search.search({
      query: jobDescription,
      attributeFilters: {
        logicalOperator: 'AND',
        filters: requiredSkills.map(skill => ({
          fieldPath: 'skills',
          operator: 'Contains',
          value: skill
        }))
      },
      minSimilarity: 0.75,
      enforcePrivacy: true,
      limit: 50
    });

    return results.matches;
  }
}

// Usage
const service = new JobMatchingService('your-api-key');

// Candidate uploads resume
const profileId = await service.createCandidateProfile(
  'user-123',
  resumeText
);

// Company searches
const candidates = await service.findCandidates(
  "We're looking for a senior Python engineer with AWS and ML experience",
  ['Python', 'AWS', 'Machine Learning']
);
```

### Example 2: React Hook

```javascript
import { useState, useEffect } from 'react';
import { EntityMatchingClient } from '@EntityMatching/sdk';

const useEntityMatching = (apiKey) => {
  const [client] = useState(() => new EntityMatchingClient({ apiKey }));

  const createProfile = async (data) => {
    return await client.profiles.create(data);
  };

  const searchProfiles = async (query, options = {}) => {
    return await client.search.search({ query, ...options });
  };

  return { createProfile, searchProfiles, client };
};

// Usage in component
function ProfileSearch() {
  const { searchProfiles } = useEntityMatching(process.env.REACT_APP_API_KEY);
  const [results, setResults] = useState([]);

  const handleSearch = async (query) => {
    const data = await searchProfiles(query, { limit: 20 });
    setResults(data.matches);
  };

  return (
    <div>
      <input onChange={(e) => handleSearch(e.target.value)} />
      {results.map(match => (
        <div key={match.profileId}>
          {match.profileId}: {(match.similarityScore * 100).toFixed(1)}%
        </div>
      ))}
    </div>
  );
}
```

---

## Best Practices

1. **Store API Keys Securely**
   - Use environment variables
   - Never commit keys to Git
   - Rotate keys regularly

2. **Handle Rate Limits**
   - Implement exponential backoff
   - Cache results when possible
   - Batch operations when available

3. **Privacy First**
   - Always use `enforcePrivacy: true` for public searches
   - Generate embeddings client-side when possible
   - Never expose profile IDs to unauthorized users

4. **Error Handling**
   - Wrap API calls in try-catch
   - Provide user-friendly error messages
   - Log errors for debugging

5. **Performance**
   - Use pagination for large result sets
   - Cache frequently accessed profiles
   - Minimize API calls with batching

---

## API Reference

See [CORE_PLATFORM_API.md](./CORE_PLATFORM_API.md) for complete API reference.

## Support

- **GitHub**: https://github.com/iunknown21/EntityMatchingAPI/issues
- **Email**: support@bystorm.com
