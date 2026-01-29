using EntityMatching.Core.Models.Summary;
using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Core.Models.Conversation;
using EntityMatching.Core.Models.Embedding;
using EntityMatching.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityMatching.Infrastructure.Services.SummaryStrategies
{
    /// <summary>
    /// Summary strategy for Career entities
    /// Generates comprehensive summaries including O*NET data, skills, tasks, and salary information
    /// </summary>
    public class CareerSummaryStrategy : IEntitySummaryStrategy
    {
        private readonly ILogger<CareerSummaryStrategy> _logger;

        public EntityType EntityType => EntityType.Career;

        public CareerSummaryStrategy(ILogger<CareerSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public async Task<EntitySummaryResult> GenerateSummaryAsync(Entity entity, ConversationContext? conversation = null)
        {
            var summary = new StringBuilder();
            var metadata = new SummaryMetadata();

            // Basic Information
            summary.AppendLine($"Career: {entity.Name}");

            if (!string.IsNullOrEmpty(entity.Description))
            {
                summary.AppendLine($"Description: {entity.Description}");
            }

            // O*NET Code
            var onetCode = GetAttributeValue<string>(entity, "onetCode");
            if (!string.IsNullOrEmpty(onetCode))
            {
                summary.AppendLine($"O*NET Code: {onetCode}");
            }

            // Education Level
            var educationLevel = GetAttributeValue<string>(entity, "educationLevel");
            if (!string.IsNullOrEmpty(educationLevel))
            {
                summary.AppendLine($"Education Required: {educationLevel}");
            }

            // Job Zone
            var jobZone = GetAttributeValue<int?>(entity, "jobZone");
            if (jobZone.HasValue)
            {
                summary.AppendLine($"Job Zone: {jobZone} (Preparation Level)");
            }

            // Salary Information
            var medianSalary = GetAttributeValue<decimal?>(entity, "medianSalary");
            if (medianSalary.HasValue)
            {
                summary.AppendLine($"Median Salary: ${medianSalary:N0}/year");
            }

            var growthOutlook = GetAttributeValue<string>(entity, "growthOutlook");
            if (!string.IsNullOrEmpty(growthOutlook))
            {
                summary.AppendLine($"Job Growth Outlook: {growthOutlook}");
            }

            // Skills
            var skills = GetAttributeList(entity, "skills");
            if (skills != null && skills.Count > 0)
            {
                metadata.PreferenceCategories.Add("Skills");
                summary.AppendLine();
                summary.AppendLine("=== Required Skills ===");
                foreach (var skill in skills.Take(10))
                {
                    summary.AppendLine($"- {skill}");
                }
                if (skills.Count > 10)
                {
                    summary.AppendLine($"... and {skills.Count - 10} more skills");
                }
            }

            // Knowledge Areas
            var knowledge = GetAttributeList(entity, "knowledge");
            if (knowledge != null && knowledge.Count > 0)
            {
                metadata.PreferenceCategories.Add("Knowledge");
                summary.AppendLine();
                summary.AppendLine("=== Required Knowledge ===");
                foreach (var item in knowledge.Take(10))
                {
                    summary.AppendLine($"- {item}");
                }
                if (knowledge.Count > 10)
                {
                    summary.AppendLine($"... and {knowledge.Count - 10} more knowledge areas");
                }
            }

            // Abilities
            var abilities = GetAttributeList(entity, "abilities");
            if (abilities != null && abilities.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("=== Required Abilities ===");
                foreach (var ability in abilities.Take(10))
                {
                    summary.AppendLine($"- {ability}");
                }
                if (abilities.Count > 10)
                {
                    summary.AppendLine($"... and {abilities.Count - 10} more abilities");
                }
            }

            // Tasks
            var tasks = GetAttributeList(entity, "tasks");
            if (tasks != null && tasks.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("=== Common Tasks ===");
                foreach (var task in tasks.Take(5))
                {
                    summary.AppendLine($"- {task}");
                }
                if (tasks.Count > 5)
                {
                    summary.AppendLine($"... and {tasks.Count - 5} more tasks");
                }
            }

            // RIASEC Interests
            var interests = GetAttributeDictionary(entity, "interests");
            if (interests != null && interests.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("=== Interest Profile (RIASEC) ===");
                foreach (var interest in interests.OrderByDescending(x => x.Value))
                {
                    summary.AppendLine($"{interest.Key}: {interest.Value:F1}");
                }
            }

            // Related Majors
            var relatedMajors = GetAttributeList(entity, "relatedMajors");
            if (relatedMajors != null && relatedMajors.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine($"=== Related Academic Majors ({relatedMajors.Count}) ===");
            }

            // Metadata
            metadata.PreferenceCategories.Add("Career");

            return await Task.FromResult(new EntitySummaryResult
            {
                Summary = summary.ToString(),
                Metadata = metadata
            });
        }

        private T? GetAttributeValue<T>(Entity entity, string key)
        {
            if (entity.Attributes.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T typedValue)
                        return typedValue;

                    // Handle conversion for numeric types
                    if (typeof(T) == typeof(int?) && value != null)
                    {
                        if (int.TryParse(value.ToString(), out var intValue))
                            return (T)(object)intValue;
                    }
                    if (typeof(T) == typeof(decimal?) && value != null)
                    {
                        if (decimal.TryParse(value.ToString(), out var decimalValue))
                            return (T)(object)decimalValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert attribute {Key} to type {Type}", key, typeof(T).Name);
                }
            }
            return default;
        }

        private List<string>? GetAttributeList(Entity entity, string key)
        {
            if (entity.Attributes.TryGetValue(key, out var value))
            {
                if (value is List<object> objList)
                    return objList.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (value is List<string> strList)
                    return strList;
            }
            return null;
        }

        private Dictionary<string, double>? GetAttributeDictionary(Entity entity, string key)
        {
            if (entity.Attributes.TryGetValue(key, out var value))
            {
                if (value is Dictionary<string, object> dict)
                {
                    return dict.ToDictionary(
                        kvp => kvp.Key,
                        kvp => Convert.ToDouble(kvp.Value)
                    );
                }
            }
            return null;
        }
    }
}
