using EntityMatching.Core.Models.Search;
using EntityMatching.SDK;
using SearchResult = ProfileMatching.Core.Models.Search.SearchResult;

namespace EntityMatching.SDK.Examples;

/// <summary>
/// Example: Semantic Entity Search with Attribute Filtering
///
/// This example shows how to search for profiles using semantic similarity
/// combined with structured attribute filters.
/// </summary>
public static class SearchProfilesExample
{
    public static async Task RunAsync()
    {
        var client = new ProfileMatchingClient(new ProfileMatchingClientOptions
        {
            ApiKey = Environment.GetEnvironmentVariable("PROFILEMATCHING_API_KEY") ?? "your-api-key",
            BaseUrl = "https://profileaiapi.azurewebsites.net"
        });

        // Example 1: Simple semantic search
        var simpleSearch = new SearchRequest
        {
            Query = "Senior Python engineer with AWS experience and machine learning background",
            Limit = 10,
            MinSimilarity = 0.7f
        };

        Console.WriteLine("=== Simple Semantic Search ===");
        var simpleResults = await client.Search.SearchAsync(simpleSearch);
        Console.WriteLine($"Found {simpleResults.TotalMatches} matches in {simpleResults.Metadata.SearchDurationMs}ms");

        foreach (var (match, index) in simpleResults.Matches.Select((m, i) => (m, i)))
        {
            Console.WriteLine($"{index + 1}. Entity {match.EntityId} - {match.SimilarityScore * 100:F1}% match");
        }

        // Example 2: Advanced search with attribute filters
        var advancedSearch = new SearchRequest
        {
            Query = "loves hiking and outdoor adventures",
            AttributeFilters = new FilterGroup
            {
                LogicalOperator = LogicalOperator.And,
                Filters = new List<AttributeFilter>
                {
                    new() { FieldPath = "naturePreferences.hasPets", Operator = FilterOperator.IsTrue },
                    new() { FieldPath = "naturePreferences.petTypes", Operator = FilterOperator.Contains, Value = "Dog" },
                    new() { FieldPath = "adventurePreferences.riskTolerance", Operator = FilterOperator.GreaterThan, Value = 6 }
                }
            },
            EnforcePrivacy = true,
            Limit = 5,
            MinSimilarity = 0.6f
        };

        Console.WriteLine("\n=== Advanced Search with Filters ===");
        Console.WriteLine("Query: Outdoor enthusiasts who own dogs and have high risk tolerance");
        var advancedResults = await client.Search.SearchAsync(advancedSearch);

        Console.WriteLine($"Found {advancedResults.TotalMatches} matches");
        foreach (var (match, index) in advancedResults.Matches.Select((m, i) => (m, i)))
        {
            Console.WriteLine($"\n{index + 1}. Entity {match.EntityId}");
            Console.WriteLine($"   Similarity: {match.SimilarityScore * 100:F1}%");
            Console.WriteLine($"   Matched Attributes: {System.Text.Json.JsonSerializer.Serialize(match.MatchedAttributes)}");
        }

        // Example 3: Privacy-protected search (company searching for candidates)
        var privacySearch = new SearchRequest
        {
            Query = "Senior software engineer, C#, Azure, 10+ years experience",
            RequestingUserId = "company-recruiter-456",
            EnforcePrivacy = true,
            Limit = 20,
            MinSimilarity = 0.75f
        };

        Console.WriteLine("\n=== Privacy-Protected Search (Recruiter View) ===");
        var privacyResults = await client.Search.SearchAsync(privacySearch);
        Console.WriteLine($"Found {privacyResults.TotalMatches} candidates");
        Console.WriteLine("Note: Only profile IDs returned - names/emails remain private until candidate opts in");

        foreach (var (match, index) in privacyResults.Matches.Take(3).Select((m, i) => (m, i)))
        {
            Console.WriteLine($"{index + 1}. Entity #{match.EntityId} - {match.SimilarityScore * 100:F1}% match");
        }
    }
}
