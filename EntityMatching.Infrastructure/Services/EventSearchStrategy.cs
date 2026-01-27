using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Search;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Implements intelligent search query generation and scoring for events
    /// Converts user profiles into targeted search queries with safety-first approach
    /// </summary>
    public class EventSearchStrategy : IThingSearchStrategy<EventSearchParams, Event>
    {
        private readonly ILogger<EventSearchStrategy> _logger;

        public EventSearchStrategy(ILogger<EventSearchStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Helper method to safely cast Entity to PersonEntity
        /// Returns null if entity is not a PersonEntity
        /// </summary>
        private PersonEntity? AsPersonEntity(Entity entity)
        {
            return entity as PersonEntity;
        }

        /// <summary>
        /// Generate 6-8 targeted search queries based on comprehensive profile analysis
        /// Priority order: Safety → Entertainment → Social → Adventure → Learning → Sensory → Seasonal
        /// </summary>
        public List<string> GenerateSearchQueries(Entity profile, EventSearchParams parameters)
        {
            var queries = new List<string>();
            var location = parameters.Location;
            var timeframe = GenerateTimeframe(parameters);

            try
            {
                // Always include a base query
                queries.Add($"events activities {location} {timeframe}");

                // Priority 1: Critical safety considerations
                AddSafetyAwareQueries(queries, profile, location, timeframe);

                // Priority 2: Primary entertainment preferences
                AddEntertainmentQueries(queries, profile, location, timeframe);

                // Priority 3: Social compatibility
                AddSocialCompatibilityQueries(queries, profile, location, timeframe);

                // Priority 4: Adventure/risk preferences
                AddAdventureQueries(queries, profile, location, timeframe);

                // Priority 5: Learning and growth interests
                AddLearningQueries(queries, profile, location, timeframe);

                // Priority 6: Sensory preferences
                AddSensoryQueries(queries, profile, location, timeframe);

                // Priority 7: Seasonal and nature preferences
                AddSeasonalQueries(queries, profile, location, timeframe);

                // Apply category filter if specified
                if (!string.IsNullOrEmpty(parameters.Category))
                {
                    queries = queries.Select(q => $"{q} {parameters.Category}").ToList();
                }

                // Limit to 8 most relevant queries to avoid overwhelming the search
                return queries.Distinct().Take(8).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating search queries for profile {ProfileId}", profile.Id);
                return new List<string> { $"events activities {location} {timeframe}" };
            }
        }

        /// <summary>
        /// Extract critical safety and accommodation requirements from profile
        /// </summary>
        public List<SafetyRequirement> GetCriticalSafetyRequirements(Entity profile)
        {
            var requirements = new List<SafetyRequirement>();

            try
            {
                var person = AsPersonEntity(profile);
                if (person == null)
                {
                    return requirements; // Not a person entity, no person-specific safety requirements
                }

                // Critical allergies (highest priority)
                if (person.DietaryRestrictions?.Allergies?.Any() == true)
                {
                    foreach (var allergy in person.DietaryRestrictions.Allergies)
                    {
                        requirements.Add(new SafetyRequirement(
                            $"no_{allergy.ToLower().Replace(" ", "_")}",
                            $"Critical allergy to {allergy} - must be avoided",
                            SafetyImportance.Critical,
                            $"Person has severe allergy to {allergy}"));
                    }
                }

                // Sensory critical requirements
                if (person.SensoryPreferences != null)
                {
                    if (person.SensoryPreferences.SensitiveToFlashingLights)
                    {
                        requirements.Add(new SafetyRequirement(
                            "no_flashing_lights",
                            "No flashing lights or strobes - sensory sensitivity",
                            SafetyImportance.Critical,
                            "Person is sensitive to flashing lights (seizure risk)"));
                    }

                    if (person.SensoryPreferences.Claustrophobic)
                    {
                        requirements.Add(new SafetyRequirement(
                            "no_enclosed_spaces",
                            "Avoid small, enclosed spaces - claustrophobia",
                            SafetyImportance.Critical,
                            "Person experiences claustrophobia"));
                    }

                    if (person.SensoryPreferences.NoiseToleranceLevel < 5 ||
                        person.SensoryPreferences.PrefersQuietEnvironments)
                    {
                        requirements.Add(new SafetyRequirement(
                            "quiet_environment",
                            "Prefer quiet or moderate noise levels",
                            SafetyImportance.High,
                            "Person is sensitive to loud sounds"));
                    }
                }

                // Adventure/risk critical requirements
                if (person.AdventurePreferences != null)
                {
                    if (person.AdventurePreferences.RiskTolerance <= 2)
                    {
                        requirements.Add(new SafetyRequirement(
                            "low_risk_only",
                            "Very low risk tolerance - only safe, gentle activities",
                            SafetyImportance.Critical,
                            "Person prefers very safe, low-risk activities"));
                    }
                }

                // Accessibility requirements
                if (person.AccessibilityNeeds != null)
                {
                    if (person.AccessibilityNeeds.RequiresWheelchairAccess)
                    {
                        requirements.Add(new SafetyRequirement(
                            "wheelchair_accessible",
                            "Must be wheelchair accessible",
                            SafetyImportance.Critical,
                            "Person requires wheelchair accessibility"));
                    }
                }

                // Dietary restrictions (high importance for food-related events)
                if (person.DietaryRestrictions?.Restrictions?.Any() == true)
                {
                    foreach (var restriction in person.DietaryRestrictions.Restrictions)
                    {
                        requirements.Add(new SafetyRequirement(
                            $"{restriction.ToLower().Replace(" ", "_").Replace("-", "_")}_options",
                            $"Must accommodate {restriction} dietary needs",
                            SafetyImportance.High,
                            $"Person follows {restriction} diet")
                        {
                            ApplicableThingTypes = new List<string> { "food", "restaurant", "dining" }
                        });
                    }
                }

                // Sort by importance (Critical first)
                return requirements.OrderByDescending(r => r.Importance).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting critical requirements for entity {EntityId}", profile.Id);
                return new List<SafetyRequirement>();
            }
        }

        /// <summary>
        /// Get scoring weights for multi-dimensional evaluation
        /// Default: Safety 35%, Social 25%, Sensory 20%, Interest 15%, Practical 5%
        /// Weights are adjusted based on profile characteristics
        /// </summary>
        public Dictionary<string, double> GetScoringWeights(Entity profile)
        {
            var person = AsPersonEntity(profile);

            var weights = new Dictionary<string, double>
            {
                { "Safety", 0.35 },
                { "Social", 0.25 },
                { "Sensory", 0.20 },
                { "Interest", 0.15 },
                { "Practical", 0.05 }
            };

            if (person == null)
            {
                return weights; // Not a person, return default weights
            }

            try
            {
                // Adjust weights based on profile characteristics
                bool hasCriticalSafety =
                    (person.DietaryRestrictions?.Allergies?.Any() == true) ||
                    (person.SensoryPreferences?.SensitiveToFlashingLights == true) ||
                    (person.SensoryPreferences?.Claustrophobic == true) ||
                    (person.AccessibilityNeeds?.RequiresWheelchairAccess == true);

                if (hasCriticalSafety)
                {
                    // Increase safety weight for high-risk profiles
                    weights["Safety"] = 0.45;
                    weights["Social"] = 0.20;
                    weights["Sensory"] = 0.20;
                    weights["Interest"] = 0.10;
                    weights["Practical"] = 0.05;
                }

                // Adjust for introversion
                if (person.SocialPreferences?.SocialBatteryLevel <= 3)
                {
                    // Increase sensory weight for introverts
                    weights["Sensory"] = 0.30;
                    weights["Social"] = 0.15;
                }

                // Adjust for extroversion
                if (person.SocialPreferences?.SocialBatteryLevel >= 8)
                {
                    // Increase social weight for extroverts
                    weights["Social"] = 0.35;
                    weights["Sensory"] = 0.15;
                }

                return weights;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating scoring weights for entity {EntityId}", profile.Id);
                return weights;
            }
        }

        /// <summary>
        /// Calculate match score for an event against a profile
        /// Uses multi-dimensional scoring based on weights
        /// </summary>
        public double CalculateMatchScore(Event eventItem, Entity profile, Dictionary<string, double> weights)
        {
            try
            {
                var scores = new Dictionary<string, double>();

                // Safety score (critical requirements validation)
                scores["Safety"] = CalculateSafetyScore(eventItem, profile);

                // Social score (group size, crowd level, interaction style)
                scores["Social"] = CalculateSocialScore(eventItem, profile);

                // Sensory score (noise, lights, environment)
                scores["Sensory"] = CalculateSensoryScore(eventItem, profile);

                // Interest score (categories, entertainment preferences)
                scores["Interest"] = CalculateInterestScore(eventItem, profile);

                // Practical score (location, price, accessibility)
                scores["Practical"] = CalculatePracticalScore(eventItem, profile);

                // Store scoring breakdown in event for transparency
                eventItem.ScoringBreakdown = scores;

                // Calculate weighted total score
                double totalScore = 0.0;
                foreach (var dimension in weights.Keys)
                {
                    if (scores.ContainsKey(dimension))
                    {
                        totalScore += scores[dimension] * weights[dimension];
                    }
                }

                return Math.Min(1.0, Math.Max(0.0, totalScore));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating match score for event {EventTitle}", eventItem.Title);
                return 0.0;
            }
        }

        /// <summary>
        /// Generate human-readable profile summary for AI prompts
        /// </summary>
        public string GenerateEntitySummary(Entity profile, EventSearchParams parameters)
        {
            var person = AsPersonEntity(profile);
            if (person == null)
            {
                return $"Events in {parameters.Location} from {parameters.StartDate:MMM d} to {parameters.EndDate:MMM d, yyyy}";
            }

            var summary = new StringBuilder();

            try
            {
                summary.AppendLine($"Searching for events for {person.Name} in {parameters.Location}");
                summary.AppendLine($"Date range: {parameters.StartDate:MMM d} - {parameters.EndDate:MMM d, yyyy}");
                summary.AppendLine();

                // Critical safety requirements first
                var safetyReqs = GetCriticalSafetyRequirements(profile);
                if (safetyReqs.Any())
                {
                    summary.AppendLine("CRITICAL SAFETY REQUIREMENTS:");
                    foreach (var req in safetyReqs.Where(r => r.Importance == SafetyImportance.Critical))
                    {
                        summary.AppendLine($"  • {req.Description}");
                    }
                    summary.AppendLine();
                }

                // Entertainment preferences
                if (person.EntertainmentPreferences != null)
                {
                    if (person.EntertainmentPreferences.FavoriteMusicGenres?.Any() == true)
                    {
                        summary.AppendLine($"Music: {string.Join(", ", person.EntertainmentPreferences.FavoriteMusicGenres)}");
                    }
                    if (person.EntertainmentPreferences.FavoriteMovieGenres?.Any() == true)
                    {
                        summary.AppendLine($"Movies: {string.Join(", ", person.EntertainmentPreferences.FavoriteMovieGenres)}");
                    }
                }

                // Social preferences
                if (person.SocialPreferences != null)
                {
                    summary.AppendLine($"Social battery: {person.SocialPreferences.SocialBatteryLevel}/10");
                }

                // Adventure preferences
                if (person.AdventurePreferences != null)
                {
                    summary.AppendLine($"Risk tolerance: {person.AdventurePreferences.RiskTolerance}/10");
                }

                return summary.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating profile summary for {ProfileId}", person.Id);
                return $"Events for {person.Name} in {parameters.Location}";
            }
        }

        #region Private Helper Methods

        private string GenerateTimeframe(EventSearchParams parameters)
        {
            var start = parameters.StartDate;
            var end = parameters.EndDate;

            if (start.Year == end.Year && start.Month == end.Month)
            {
                return $"{start:MMMM yyyy}";
            }
            else if (start.Year == end.Year)
            {
                return $"{start:MMM}-{end:MMM yyyy}";
            }
            else
            {
                return $"{start:MMM yyyy}-{end:MMM yyyy}";
            }
        }

        private void AddSafetyAwareQueries(List<string> queries, Entity profile, string location, string timeframe)
        {
            var person = AsPersonEntity(profile);
            if (person == null) return;

            // If person has allergies, prioritize safe food events
            if (person.DietaryRestrictions?.Allergies?.Any() == true)
            {
                queries.Add($"allergy-friendly safe events {location} {timeframe}");
            }

            // If person needs wheelchair access
            if (person.AccessibilityNeeds?.RequiresWheelchairAccess == true)
            {
                queries.Add($"wheelchair accessible events {location} {timeframe}");
            }

            // If person has sensory sensitivities
            if (person.SensoryPreferences?.NoiseToleranceLevel < 5 ||
                person.SensoryPreferences?.PrefersQuietEnvironments == true)
            {
                queries.Add($"quiet calm events {location} {timeframe}");
            }
        }

        private void AddEntertainmentQueries(List<string> queries, Entity profile, string location, string timeframe)
        {
            var person = AsPersonEntity(profile);
            if (person?.EntertainmentPreferences == null) return;

            // Music preferences
            if (person.EntertainmentPreferences.FavoriteMusicGenres?.Any() == true)
            {
                var topGenres = string.Join(" ", person.EntertainmentPreferences.FavoriteMusicGenres.Take(2));
                queries.Add($"{topGenres} live music concerts {location} {timeframe}");
            }

            // Movie/theater preferences
            if (person.EntertainmentPreferences.FavoriteMovieGenres?.Any() == true)
            {
                queries.Add($"theater shows films {location} {timeframe}");
            }
        }

        private void AddSocialCompatibilityQueries(List<string> queries, Entity profile, string location, string timeframe)
        {
            var person = AsPersonEntity(profile);
            if (person?.SocialPreferences == null) return;

            // Introvert vs Extrovert
            if (person.SocialPreferences.SocialBatteryLevel <= 3)
            {
                queries.Add($"intimate quiet small group events {location} {timeframe}");
            }
            else if (person.SocialPreferences.SocialBatteryLevel >= 8)
            {
                queries.Add($"social interactive group events {location} {timeframe}");
            }

            // Conversation style
            if (person.SocialPreferences.PrefersDeepConversations)
            {
                queries.Add($"discussion intellectual events {location} {timeframe}");
            }
        }

        private void AddAdventureQueries(List<string> queries, Entity profile, string location, string timeframe)
        {
            var person = AsPersonEntity(profile);
            if (person?.AdventurePreferences == null) return;

            // Risk tolerance
            if (person.AdventurePreferences.RiskTolerance >= 7)
            {
                queries.Add($"adventure extreme thrilling events {location} {timeframe}");
            }
            else if (person.AdventurePreferences.RiskTolerance <= 3)
            {
                queries.Add($"calm relaxing safe events {location} {timeframe}");
            }

            // Spontaneity
            if (person.AdventurePreferences.NoveltyPreference >= 7 ||
                person.AdventurePreferences.EnjoysSpontaneousActivities)
            {
                queries.Add($"unique unusual spontaneous events {location} {timeframe}");
            }
        }

        private void AddLearningQueries(List<string> queries, Entity profile, string location, string timeframe)
        {
            var person = AsPersonEntity(profile);
            if (person?.LearningPreferences == null) return;

            // Learning interests
            if (person.LearningPreferences.SubjectsOfInterest?.Any() == true)
            {
                var topInterest = person.LearningPreferences.SubjectsOfInterest.First();
                queries.Add($"{topInterest} workshops classes educational events {location} {timeframe}");
            }

            // Preferred learning style
            if (person.LearningPreferences.LearningStyles?.Any() == true)
            {
                var hasHandsOn = person.LearningPreferences.LearningStyles
                    .Any(style => style.Contains("hands", StringComparison.OrdinalIgnoreCase));
                if (hasHandsOn)
                {
                    queries.Add($"hands-on interactive workshop events {location} {timeframe}");
                }
            }
        }

        private void AddSensoryQueries(List<string> queries, Entity profile, string location, string timeframe)
        {
            var person = AsPersonEntity(profile);
            if (person?.SensoryPreferences == null) return;

            // Sound sensitivity (already handled in safety, but reinforce)
            if (person.SensoryPreferences.NoiseToleranceLevel < 5 ||
                person.SensoryPreferences.PrefersQuietEnvironments)
            {
                queries.Add($"quiet peaceful serene events {location} {timeframe}");
            }
        }

        private void AddSeasonalQueries(List<string> queries, Entity profile, string location, string timeframe)
        {
            var person = AsPersonEntity(profile);
            if (person?.NaturePreferences == null) return;

            // Favorite seasons or outdoor preferences
            if (person.NaturePreferences.PreferredSeasons?.Any() == true)
            {
                var currentSeason = GetCurrentSeason();
                if (person.NaturePreferences.PreferredSeasons.Contains(currentSeason, StringComparer.OrdinalIgnoreCase))
                {
                    queries.Add($"outdoor {currentSeason} events {location} {timeframe}");
                }
            }

            // Nature connection - use gardening/birdwatching as proxy for nature interest
            if (person.NaturePreferences.EnjoysGardening || person.NaturePreferences.EnjoysBirdWatching)
            {
                queries.Add($"outdoor nature parks events {location} {timeframe}");
            }
        }

        private string GetCurrentSeason()
        {
            var month = DateTime.Now.Month;
            return month switch
            {
                12 or 1 or 2 => "winter",
                3 or 4 or 5 => "spring",
                6 or 7 or 8 => "summer",
                _ => "fall"
            };
        }

        private double CalculateSafetyScore(Event eventItem, Entity profile)
        {
            // Start with perfect score, deduct for violations
            double score = 1.0;

            var requirements = GetCriticalSafetyRequirements(profile);
            foreach (var req in requirements)
            {
                // Check if event description violates this requirement
                // This is a simple keyword-based check - could be enhanced with AI
                bool violated = false;

                if (req.Key.Contains("no_") &&
                    eventItem.Description.Contains(req.Key.Replace("no_", ""), StringComparison.OrdinalIgnoreCase))
                {
                    violated = true;
                }

                if (violated)
                {
                    // Penalty based on importance
                    score -= req.Importance switch
                    {
                        SafetyImportance.Critical => 1.0, // Complete disqualification
                        SafetyImportance.High => 0.5,
                        SafetyImportance.Medium => 0.25,
                        _ => 0.1
                    };
                }
            }

            return Math.Max(0.0, score);
        }

        private double CalculateSocialScore(Event eventItem, Entity profile)
        {
            // Placeholder - would analyze event type, venue capacity, interaction style
            return 0.7; // Default moderate match
        }

        private double CalculateSensoryScore(Event eventItem, Entity profile)
        {
            // Placeholder - would analyze noise level, lighting, crowding
            return 0.7; // Default moderate match
        }

        private double CalculateInterestScore(Event eventItem, Entity profile)
        {
            var person = AsPersonEntity(profile);
            if (person == null) return 0.5; // Not a person, return baseline score

            double score = 0.5; // Baseline

            // Check music genre matches
            if (person.EntertainmentPreferences?.FavoriteMusicGenres?.Any() == true)
            {
                foreach (var genre in person.EntertainmentPreferences.FavoriteMusicGenres)
                {
                    if (eventItem.Category.Contains(genre, StringComparison.OrdinalIgnoreCase) ||
                        eventItem.Title.Contains(genre, StringComparison.OrdinalIgnoreCase) ||
                        eventItem.Description.Contains(genre, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.2;
                    }
                }
            }

            // Check movie genre matches
            if (person.EntertainmentPreferences?.FavoriteMovieGenres?.Any() == true)
            {
                foreach (var genre in person.EntertainmentPreferences.FavoriteMovieGenres)
                {
                    if (eventItem.Category.Contains(genre, StringComparison.OrdinalIgnoreCase) ||
                        eventItem.Title.Contains(genre, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.15;
                    }
                }
            }

            return Math.Min(1.0, score);
        }

        private double CalculatePracticalScore(Event eventItem, Entity profile)
        {
            // Placeholder - would check price, distance, timing
            return 0.8; // Default good match
        }

        #endregion
    }
}
