using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Search;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for discovering events based on user profiles
    /// Supports web search, embedding search, and hybrid modes
    /// </summary>
    public class EventDiscoveryService : IThingDiscoveryService<EventSearchParams, Event>
    {
        private readonly IEntityService _profileService;
        private readonly IThingSearchStrategy<EventSearchParams, Event> _searchStrategy;
        private readonly IWebSearchService _webSearchService;
        private readonly ILogger<EventDiscoveryService> _logger;

        public EventDiscoveryService(
            IEntityService profileService,
            IThingSearchStrategy<EventSearchParams, Event> searchStrategy,
            IWebSearchService webSearchService,
            ILogger<EventDiscoveryService> logger)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _searchStrategy = searchStrategy ?? throw new ArgumentNullException(nameof(searchStrategy));
            _webSearchService = webSearchService ?? throw new ArgumentNullException(nameof(webSearchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Discover events using real-time web search via Groq
        /// </summary>
        public async Task<ThingSearchResult<Event>> DiscoverViaWebSearchAsync(
            string profileId,
            EventSearchParams parameters,
            int limit = 20)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting web search for profile {ProfileId}, location {Location}",
                    profileId, parameters.Location);

                // Get entity
                var profile = await _profileService.GetEntityAsync(profileId);
                if (profile == null)
                {
                    _logger.LogWarning("Entity {ProfileId} not found", profileId);
                    return CreateEmptyResult("web_search", profileId);
                }

                // Generate profile-based search queries
                var queries = _searchStrategy.GenerateSearchQueries(profile, parameters);
                var safetyRequirements = _searchStrategy.GetCriticalSafetyRequirements(profile);
                var scoringWeights = _searchStrategy.GetScoringWeights(profile);

                _logger.LogInformation("Generated {QueryCount} search queries for profile {ProfileId}",
                    queries.Count, profileId);

                var allEvents = new List<Event>();

                // Execute web searches for each query (limit to 6 queries to avoid overwhelming)
                var queriesToExecute = queries.Take(6).ToList();
                foreach (var query in queriesToExecute)
                {
                    try
                    {
                        _logger.LogDebug("Executing web search: {Query}", query);

                        var searchContext = new SearchContext
                        {
                            ThingType = "event",
                            MaxResults = 10
                        };

                        var events = await _webSearchService.SearchAsync<Event>(query, searchContext);
                        allEvents.AddRange(events);

                        _logger.LogDebug("Found {EventCount} events for query: {Query}",
                            events.Count(), query);

                        // Rate limiting - wait 1 second between queries
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing web search for query: {Query}", query);
                        // Continue with other queries
                    }
                }

                _logger.LogInformation("Total events found from web search: {TotalEvents}", allEvents.Count);

                // Filter by safety requirements
                var safeEvents = allEvents
                    .Where(e => ValidateSafety(e, safetyRequirements))
                    .ToList();

                _logger.LogInformation("Events after safety filtering: {SafeEvents} (removed {RemovedCount})",
                    safeEvents.Count, allEvents.Count - safeEvents.Count);

                // Score and rank
                foreach (var evt in safeEvents)
                {
                    evt.MatchScore = _searchStrategy.CalculateMatchScore(evt, profile, scoringWeights);
                    GenerateMatchReasons(evt, profile);
                }

                // Get top events
                var topEvents = safeEvents
                    .OrderByDescending(e => e.MatchScore)
                    .Take(limit)
                    .ToList();

                stopwatch.Stop();

                return new ThingSearchResult<Event>
                {
                    Matches = topEvents,
                    TotalMatches = topEvents.Count,
                    Metadata = new ThingSearchMetadata
                    {
                        TotalResults = topEvents.Count,
                        QueriesGenerated = queries.Count,
                        SearchMode = "web_search",
                        SearchDurationMs = stopwatch.ElapsedMilliseconds,
                        ProfileId = profileId,
                        WebSearchResults = topEvents.Count,
                        EmbeddingResults = 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during web search for profile {ProfileId}", profileId);
                stopwatch.Stop();
                return CreateEmptyResult("web_search", profileId);
            }
        }

        /// <summary>
        /// Discover events using stored embeddings (not yet implemented)
        /// Placeholder for future embedding-based search
        /// </summary>
        public async Task<ThingSearchResult<Event>> DiscoverViaEmbeddingsAsync(
            string profileId,
            EventSearchParams parameters,
            int limit = 20)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Embedding-based event search not yet implemented for profile {ProfileId}",
                    profileId);

                // TODO: Implement embedding-based search
                // 1. Get profile
                // 2. Generate profile embedding
                // 3. Search stored event embeddings for similar items
                // 4. Score and rank results
                // 5. Return top matches

                stopwatch.Stop();

                return new ThingSearchResult<Event>
                {
                    Matches = new List<Event>(),
                    TotalMatches = 0,
                    Metadata = new ThingSearchMetadata
                    {
                        TotalResults = 0,
                        SearchMode = "embeddings",
                        SearchDurationMs = stopwatch.ElapsedMilliseconds,
                        ProfileId = profileId,
                        EmbeddingResults = 0,
                        WebSearchResults = 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during embedding search for profile {ProfileId}", profileId);
                stopwatch.Stop();
                return CreateEmptyResult("embeddings", profileId);
            }
        }

        /// <summary>
        /// Hybrid approach: Try embeddings first, supplement with web search
        /// Currently falls back to web search only since embeddings are not yet implemented
        /// </summary>
        public async Task<ThingSearchResult<Event>> DiscoverHybridAsync(
            string profileId,
            EventSearchParams parameters,
            int limit = 20)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting hybrid search for profile {ProfileId}", profileId);

                var results = new List<Event>();
                int embeddingCount = 0;
                int webSearchCount = 0;

                // PHASE 1: Try stored embeddings first (fast, cached)
                // Currently skipped since embedding search is not yet implemented
                _logger.LogDebug("Skipping embedding search (not yet implemented)");

                // PHASE 2: Supplement with web search (real-time discovery)
                var remaining = limit - results.Count;
                if (remaining > 0)
                {
                    _logger.LogDebug("Fetching {Count} results from web search", remaining);
                    var webResults = await DiscoverViaWebSearchAsync(profileId, parameters, remaining);
                    results.AddRange(webResults.Matches);
                    webSearchCount = webResults.Matches.Count;
                }

                // PHASE 3: Deduplicate and rank
                var deduplicated = DeduplicateResults(results);
                var ranked = deduplicated
                    .OrderByDescending(e => e.MatchScore)
                    .Take(limit)
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "Hybrid search complete: {TotalResults} results ({EmbeddingCount} from embeddings, {WebCount} from web)",
                    ranked.Count, embeddingCount, webSearchCount);

                return new ThingSearchResult<Event>
                {
                    Matches = ranked,
                    TotalMatches = ranked.Count,
                    Metadata = new ThingSearchMetadata
                    {
                        TotalResults = ranked.Count,
                        SearchMode = "hybrid",
                        SearchDurationMs = stopwatch.ElapsedMilliseconds,
                        ProfileId = profileId,
                        EmbeddingResults = embeddingCount,
                        WebSearchResults = webSearchCount
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hybrid search for profile {ProfileId}", profileId);
                stopwatch.Stop();
                return CreateEmptyResult("hybrid", profileId);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Validate event against critical safety requirements
        /// </summary>
        private bool ValidateSafety(Event eventItem, List<SafetyRequirement> requirements)
        {
            foreach (var req in requirements.Where(r => r.Importance == SafetyImportance.Critical))
            {
                // Simple keyword-based validation
                // Could be enhanced with AI-based validation in the future
                var eventText = $"{eventItem.Title} {eventItem.Description} {eventItem.Category}".ToLower();

                // Check for violations
                if (req.Key.StartsWith("no_"))
                {
                    var forbidden = req.Key.Substring(3).Replace("_", " ");

                    // Handle plural/singular variations by checking the base word (without trailing 's')
                    var forbiddenBase = forbidden.TrimEnd('s');

                    if (eventText.Contains(forbiddenBase))
                    {
                        _logger.LogDebug("Event {EventTitle} violates safety requirement: {Requirement}",
                            eventItem.Title, req.Description);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Generate human-readable match reasons for an event
        /// </summary>
        private void GenerateMatchReasons(Event eventItem, Entity profile)
        {
            // NOTE: PersonEntity has been removed. This method is temporarily disabled.
            // TODO: Refactor to use Entity.Attributes dictionary for person preferences
            dynamic? person = null; // PersonEntity type removed
            if (person == null) return; // Not a person entity, no person-specific match reasons

            eventItem.MatchReasons = new Dictionary<string, string>();

            // Music preferences
            if (person.EntertainmentPreferences?.FavoriteMusicGenres?.Any() == true)
            {
                foreach (var genre in person.EntertainmentPreferences.FavoriteMusicGenres)
                {
                    if (eventItem.Description.Contains(genre, StringComparison.OrdinalIgnoreCase) ||
                        eventItem.Title.Contains(genre, StringComparison.OrdinalIgnoreCase))
                    {
                        eventItem.MatchReasons["music"] = $"Matches your interest in {genre}";
                        break;
                    }
                }
            }

            // Social preferences
            if (person.SocialPreferences != null)
            {
                if (person.SocialPreferences.SocialBatteryLevel <= 3 &&
                    (eventItem.Description.Contains("intimate", StringComparison.OrdinalIgnoreCase) ||
                     eventItem.Description.Contains("small", StringComparison.OrdinalIgnoreCase)))
                {
                    eventItem.MatchReasons["social"] = "Intimate setting matches your preference for smaller groups";
                }
                else if (person.SocialPreferences.SocialBatteryLevel >= 8 &&
                         (eventItem.Description.Contains("festival", StringComparison.OrdinalIgnoreCase) ||
                          eventItem.Description.Contains("crowd", StringComparison.OrdinalIgnoreCase)))
                {
                    eventItem.MatchReasons["social"] = "Large gathering matches your high social energy";
                }
            }

            // Adventure preferences
            if (person.AdventurePreferences != null)
            {
                if (person.AdventurePreferences.RiskTolerance >= 7 &&
                    (eventItem.Description.Contains("adventure", StringComparison.OrdinalIgnoreCase) ||
                     eventItem.Description.Contains("extreme", StringComparison.OrdinalIgnoreCase)))
                {
                    eventItem.MatchReasons["adventure"] = "Adventure activity matches your high risk tolerance";
                }
            }
        }

        /// <summary>
        /// Deduplicate events based on title and location similarity
        /// </summary>
        private List<Event> DeduplicateResults(List<Event> events)
        {
            var unique = new List<Event>();
            var seen = new HashSet<string>();

            foreach (var evt in events)
            {
                // Create a normalized key for deduplication
                var key = $"{evt.Title.ToLower().Trim()}|{evt.Location.ToLower().Trim()}|{evt.EventDate?.Date}";

                if (!seen.Contains(key))
                {
                    seen.Add(key);
                    unique.Add(evt);
                }
                else
                {
                    _logger.LogDebug("Skipping duplicate event: {EventTitle}", evt.Title);
                }
            }

            return unique;
        }

        /// <summary>
        /// Create an empty search result
        /// </summary>
        private ThingSearchResult<Event> CreateEmptyResult(string searchMode, string profileId)
        {
            return new ThingSearchResult<Event>
            {
                Matches = new List<Event>(),
                TotalMatches = 0,
                Metadata = new ThingSearchMetadata
                {
                    TotalResults = 0,
                    SearchMode = searchMode,
                    SearchDurationMs = 0,
                    ProfileId = profileId,
                    EmbeddingResults = 0,
                    WebSearchResults = 0
                }
            };
        }

        #endregion
    }
}
