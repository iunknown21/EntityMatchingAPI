using EntityMatching.Core.Models.Summary;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services.SummaryStrategies
{
    /// <summary>
    /// Summary strategy for Person entities
    /// Generates comprehensive summaries including personality, preferences, and conversation insights
    /// </summary>
    public class PersonSummaryStrategy : IEntitySummaryStrategy
    {
        private readonly ILogger<PersonSummaryStrategy> _logger;

        public EntityType EntityType => EntityType.Person;

        public PersonSummaryStrategy(ILogger<PersonSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public async Task<EntitySummaryResult> GenerateSummaryAsync(Entity entity, ConversationContext? conversation = null)
        {
            // Cast to PersonEntity if possible, otherwise work with base Entity
            var person = entity as PersonEntity;

            var summary = new StringBuilder();
            var metadata = new SummaryMetadata();

            // Basic Information
            summary.AppendLine($"Person: {entity.Name}");

            if (!string.IsNullOrEmpty(entity.Description))
            {
                summary.AppendLine($"Bio: {entity.Description}");
            }

            // Try to get person-specific data from strongly-typed model or attributes
            if (person != null)
            {
                if (!string.IsNullOrEmpty(person.Location))
                {
                    summary.AppendLine($"Located in {person.Location}");
                }

                if (person.Age.HasValue)
                {
                    summary.AppendLine($"Age: {person.Age} years old");
                }

                // Personality Data
                if (person.PersonalityClassifications != null)
                {
                    metadata.HasPersonalityData = true;
                    summary.AppendLine();
                    summary.AppendLine("=== Personality ===");
                    summary.AppendLine(person.PersonalityClassifications.ToString() ?? "Personality data available");
                }

                // Love Languages
                if (person.LoveLanguages != null)
                {
                    summary.AppendLine();
                    summary.AppendLine("=== Love Languages ===");
                    summary.AppendLine(person.LoveLanguages.ToString() ?? "Love languages data available");
                }

                // Entertainment Preferences
                if (person.EntertainmentPreferences != null)
                {
                    metadata.PreferenceCategories.Add("Entertainment");
                    summary.AppendLine();
                    summary.AppendLine("=== Entertainment Preferences ===");
                    summary.AppendLine("Entertainment preferences recorded");
                }

                // Adventure Preferences
                if (person.AdventurePreferences != null)
                {
                    metadata.PreferenceCategories.Add("Adventure");
                    summary.AppendLine();
                    summary.AppendLine("=== Adventure & Activity ===");
                    summary.AppendLine("Adventure preferences recorded");
                }

                // Learning Preferences
                if (person.LearningPreferences != null)
                {
                    metadata.PreferenceCategories.Add("Learning");
                    summary.AppendLine();
                    summary.AppendLine("=== Learning & Culture ===");
                    summary.AppendLine("Learning preferences recorded");
                }

                // Sensory Preferences
                if (person.SensoryPreferences != null)
                {
                    metadata.PreferenceCategories.Add("Sensory");
                    summary.AppendLine();
                    summary.AppendLine("=== Sensory Preferences ===");
                    summary.AppendLine("Sensory preferences recorded");
                }

                // Social Preferences
                if (person.SocialPreferences != null)
                {
                    metadata.PreferenceCategories.Add("Social");
                    summary.AppendLine();
                    summary.AppendLine("=== Social Preferences ===");
                    summary.AppendLine("Social preferences recorded");
                }

                // Style Preferences
                if (person.StylePreferences != null)
                {
                    metadata.PreferenceCategories.Add("Style");
                    summary.AppendLine();
                    summary.AppendLine("=== Style & Aesthetics ===");

                    var sp = person.StylePreferences;
                    if (sp.FavoriteColors?.Any() == true)
                        summary.AppendLine($"Favorite Colors: {string.Join(", ", sp.FavoriteColors)}");
                    if (sp.FashionStyle?.Any() == true)
                        summary.AppendLine($"Fashion Style: {string.Join(", ", sp.FashionStyle)}");
                    if (sp.HomeDecorStyle?.Any() == true)
                        summary.AppendLine($"Home Decor Style: {string.Join(", ", sp.HomeDecorStyle)}");
                }

                // Nature Preferences
                if (person.NaturePreferences != null)
                {
                    metadata.PreferenceCategories.Add("Nature");
                    summary.AppendLine();
                    summary.AppendLine("=== Nature & Environment ===");

                    var np = person.NaturePreferences;
                    if (np.PreferredSeasons?.Any() == true)
                        summary.AppendLine($"Preferred Seasons: {string.Join(", ", np.PreferredSeasons)}");
                    if (np.HasPets)
                        summary.AppendLine($"Has Pets: {string.Join(", ", np.PetTypes)}");
                    if (np.EnjoysGardening)
                        summary.AppendLine("Enjoys gardening");
                }

                // Gift Preferences
                if (person.GiftPreferences != null)
                {
                    metadata.PreferenceCategories.Add("Gift");
                    summary.AppendLine();
                    summary.AppendLine("=== Gift Preferences ===");

                    var gp = person.GiftPreferences;
                    if (gp.MeaningfulGiftTypes?.Any() == true)
                        summary.AppendLine($"Meaningful Gift Types: {string.Join(", ", gp.MeaningfulGiftTypes)}");
                    if (!string.IsNullOrEmpty(gp.PreferredGiftStyle))
                        summary.AppendLine($"Gift Style: {gp.PreferredGiftStyle}");
                    if (gp.LikesSurprises)
                        summary.AppendLine("Enjoys surprises");
                    if (gp.CollectsOrHobbies?.Any() == true)
                        summary.AppendLine($"Collects/Hobbies: {string.Join(", ", gp.CollectsOrHobbies)}");
                }

                // Accessibility Needs
                if (person.AccessibilityNeeds != null)
                {
                    var an = person.AccessibilityNeeds;
                    if (an.RequiresWheelchairAccess || an.HasLimitedMobility || an.RequiresHearingAssistance || !string.IsNullOrEmpty(an.SpecialConsiderations))
                    {
                        summary.AppendLine();
                        summary.AppendLine("=== Accessibility Needs ===");
                        if (an.RequiresWheelchairAccess)
                            summary.AppendLine("Requires wheelchair access");
                        if (an.HasLimitedMobility)
                            summary.AppendLine("Has limited mobility");
                        if (an.RequiresHearingAssistance)
                            summary.AppendLine("Requires hearing assistance");
                        if (!string.IsNullOrEmpty(an.SpecialConsiderations))
                            summary.AppendLine($"Special Considerations: {an.SpecialConsiderations}");
                    }
                }

                // Dietary Restrictions
                if (person.DietaryRestrictions != null)
                {
                    var dr = person.DietaryRestrictions;
                    if (dr.Restrictions?.Any() == true || dr.Allergies?.Any() == true)
                    {
                        summary.AppendLine();
                        summary.AppendLine("=== Dietary Restrictions ===");
                        if (dr.Restrictions?.Any() == true)
                            summary.AppendLine($"Dietary Restrictions: {string.Join(", ", dr.Restrictions)}");
                        if (dr.Allergies?.Any() == true)
                            summary.AppendLine($"Allergies: {string.Join(", ", dr.Allergies)}");
                    }
                }
            }
            else
            {
                // Fallback: extract from attributes if not a PersonEntity
                AppendFromAttributes(summary, entity, metadata);
            }

            // Conversation Insights (universal for all entities)
            if (conversation?.ExtractedInsights?.Any() == true)
            {
                metadata.HasConversationData = true;
                metadata.ConversationChunksCount = conversation.ConversationChunks?.Count ?? 0;
                metadata.ExtractedInsightsCount = conversation.ExtractedInsights.Count;

                summary.AppendLine();
                summary.AppendLine("=== Additional Insights from Conversations ===");
                var insightsSummary = conversation.GetInsightsSummary();
                if (!string.IsNullOrEmpty(insightsSummary))
                {
                    summary.AppendLine(insightsSummary);
                }
            }

            var summaryText = summary.ToString();
            metadata.SummaryWordCount = summaryText.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            return await Task.FromResult(new EntitySummaryResult
            {
                Summary = summaryText,
                Metadata = metadata
            });
        }

        private void AppendFromAttributes(StringBuilder summary, Entity entity, SummaryMetadata metadata)
        {
            // Extract person data from attributes dictionary if available
            if (entity.Attributes.TryGetValue("age", out var age))
            {
                summary.AppendLine($"Age: {age}");
            }

            if (entity.Attributes.TryGetValue("location", out var location))
            {
                summary.AppendLine($"Location: {location}");
            }

            if (entity.Attributes.TryGetValue("skills", out var skills))
            {
                summary.AppendLine($"Skills: {string.Join(", ", (string[])skills)}");
            }

            if (entity.Attributes.TryGetValue("interests", out var interests))
            {
                summary.AppendLine($"Interests: {string.Join(", ", (string[])interests)}");
            }
        }
    }
}
