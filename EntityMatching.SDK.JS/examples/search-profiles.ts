/**
 * Example: Semantic Profile Search with Attribute Filtering
 *
 * This example shows how to search for profiles using semantic similarity
 * combined with structured attribute filters.
 */

import { ProfileMatchingClient, SearchRequest } from '../src';

async function main() {
  const client = new ProfileMatchingClient({
    apiKey: process.env.PROFILEMATCHING_API_KEY || 'your-api-key',
    baseUrl: 'https://profileaiapi.azurewebsites.net',
  });

  // Example 1: Simple semantic search
  const simpleSearch: SearchRequest = {
    query: 'Senior Python engineer with AWS experience and machine learning background',
    limit: 10,
    minSimilarity: 0.7,
  };

  console.log('=== Simple Semantic Search ===');
  const simpleResults = await client.search.search(simpleSearch);
  console.log(`Found ${simpleResults.totalMatches} matches in ${simpleResults.metadata.searchDurationMs}ms`);

  simpleResults.matches.forEach((match, index) => {
    console.log(`${index + 1}. Profile ${match.profileId} - ${(match.similarityScore * 100).toFixed(1)}% match`);
  });

  // Example 2: Advanced search with attribute filters
  const advancedSearch: SearchRequest = {
    query: 'loves hiking and outdoor adventures',
    attributeFilters: {
      logicalOperator: 'And',
      filters: [
        {
          fieldPath: 'naturePreferences.hasPets',
          operator: 'IsTrue',
        },
        {
          fieldPath: 'naturePreferences.petTypes',
          operator: 'Contains',
          value: 'Dog',
        },
        {
          fieldPath: 'adventurePreferences.riskTolerance',
          operator: 'GreaterThan',
          value: 6,
        },
      ],
    },
    enforcePrivacy: true,
    limit: 5,
    minSimilarity: 0.6,
  };

  console.log('\n=== Advanced Search with Filters ===');
  console.log('Query: Outdoor enthusiasts who own dogs and have high risk tolerance');
  const advancedResults = await client.search.search(advancedSearch);

  console.log(`Found ${advancedResults.totalMatches} matches`);
  advancedResults.matches.forEach((match, index) => {
    console.log(`\n${index + 1}. Profile ${match.profileId}`);
    console.log(`   Similarity: ${(match.similarityScore * 100).toFixed(1)}%`);
    console.log(`   Matched Attributes:`, JSON.stringify(match.matchedAttributes, null, 2));
  });

  // Example 3: Privacy-protected search (company searching for candidates)
  const privacySearch: SearchRequest = {
    query: 'Senior software engineer, Python, AWS, 10+ years experience',
    requestingUserId: 'company-recruiter-456',
    enforcePrivacy: true,
    limit: 20,
    minSimilarity: 0.75,
  };

  console.log('\n=== Privacy-Protected Search (Recruiter View) ===');
  const privacyResults = await client.search.search(privacySearch);
  console.log(`Found ${privacyResults.totalMatches} candidates`);
  console.log('Note: Only profile IDs returned - names/emails remain private until candidate opts in');

  privacyResults.matches.slice(0, 3).forEach((match, index) => {
    console.log(`${index + 1}. Profile #${match.profileId} - ${(match.similarityScore * 100).toFixed(1)}% match`);
  });
}

main().catch(console.error);
