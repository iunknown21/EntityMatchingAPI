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
    /// Summary strategy for Major (academic program) entities
    /// Generates comprehensive summaries including CIP data, career pathways, and salary information
    /// </summary>
    public class MajorSummaryStrategy : IEntitySummaryStrategy
    {
        private readonly ILogger<MajorSummaryStrategy> _logger;

        public EntityType EntityType => EntityType.Major;

        public MajorSummaryStrategy(ILogger<MajorSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public async Task<EntitySummaryResult> GenerateSummaryAsync(Entity entity, ConversationContext? conversation = null)
        {
            var major = entity as MajorEntity;
            var summary = new StringBuilder();
            var metadata = new SummaryMetadata();

            // Basic Information
            summary.AppendLine($"Academic Major: {entity.Name}");

            if (!string.IsNullOrEmpty(entity.Description))
            {
                summary.AppendLine($"Description: {entity.Description}");
            }

            // CIP Code
            var cipCode = major?.CipCode ?? GetAttributeValue<string>(entity, "cipCode");
            if (!string.IsNullOrEmpty(cipCode))
            {
                summary.AppendLine($"CIP Code: {cipCode}");
            }

            // Degree Level
            var degreeLevel = major?.DegreeLevel ?? GetAttributeValue<string>(entity, "degreeLevel");
            if (!string.IsNullOrEmpty(degreeLevel))
            {
                summary.AppendLine($"Degree Level: {degreeLevel}");
            }

            // Field Category
            var fieldCategory = major?.FieldCategory ?? GetAttributeValue<string>(entity, "fieldCategory");
            if (!string.IsNullOrEmpty(fieldCategory))
            {
                summary.AppendLine($"Field: {fieldCategory}");
            }

            // Salary Information
            var startingSalary = major?.AverageStartingSalary ?? GetAttributeValue<decimal?>(entity, "averageStartingSalary");
            if (startingSalary.HasValue)
            {
                summary.AppendLine($"Average Starting Salary: ${startingSalary:N0}/year");
            }

            var midCareerSalary = major?.MidCareerSalary ?? GetAttributeValue<decimal?>(entity, "midCareerSalary");
            if (midCareerSalary.HasValue)
            {
                summary.AppendLine($"Mid-Career Salary: ${midCareerSalary:N0}/year");
            }

            // Employment Rate
            var employmentRate = major?.EmploymentRate ?? GetAttributeValue<double?>(entity, "employmentRate");
            if (employmentRate.HasValue)
            {
                summary.AppendLine($"Employment Rate: {employmentRate:P1}");
            }

            // Core Competencies
            var competencies = major?.CoreCompetencies ?? GetAttributeList(entity, "coreCompetencies");
            if (competencies != null && competencies.Count > 0)
            {
                metadata.PreferenceCategories.Add("Competencies");
                summary.AppendLine();
                summary.AppendLine("=== Core Competencies ===");
                foreach (var competency in competencies.Take(10))
                {
                    summary.AppendLine($"- {competency}");
                }
                if (competencies.Count > 10)
                {
                    summary.AppendLine($"... and {competencies.Count - 10} more competencies");
                }
            }

            // Skills Developed
            var skills = major?.SkillsDeveloped ?? GetAttributeList(entity, "skillsDeveloped");
            if (skills != null && skills.Count > 0)
            {
                metadata.PreferenceCategories.Add("Skills");
                summary.AppendLine();
                summary.AppendLine("=== Skills Developed ===");
                foreach (var skill in skills.Take(10))
                {
                    summary.AppendLine($"- {skill}");
                }
                if (skills.Count > 10)
                {
                    summary.AppendLine($"... and {skills.Count - 10} more skills");
                }
            }

            // Typical Courses
            var courses = major?.TypicalCourses ?? GetAttributeList(entity, "typicalCourses");
            if (courses != null && courses.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("=== Typical Courses ===");
                foreach (var course in courses.Take(8))
                {
                    summary.AppendLine($"- {course}");
                }
                if (courses.Count > 8)
                {
                    summary.AppendLine($"... and {courses.Count - 8} more courses");
                }
            }

            // Career Pathways
            var relatedCareers = major?.RelatedCareers ?? GetAttributeList(entity, "relatedCareers");
            if (relatedCareers != null && relatedCareers.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine($"=== Related Career Paths ({relatedCareers.Count}) ===");
                summary.AppendLine("This major prepares students for careers in:");
                foreach (var career in relatedCareers.Take(10))
                {
                    summary.AppendLine($"- {career}");
                }
                if (relatedCareers.Count > 10)
                {
                    summary.AppendLine($"... and {relatedCareers.Count - 10} more career options");
                }
            }

            // Common Industries
            var industries = major?.CommonIndustries ?? GetAttributeList(entity, "commonIndustries");
            if (industries != null && industries.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("=== Common Industries ===");
                foreach (var industry in industries.Take(8))
                {
                    summary.AppendLine($"- {industry}");
                }
                if (industries.Count > 8)
                {
                    summary.AppendLine($"... and {industries.Count - 8} more industries");
                }
            }

            // Metadata
            metadata.PreferenceCategories.Add("Academic Major");

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
                    if (typeof(T) == typeof(decimal?) && value != null)
                    {
                        if (decimal.TryParse(value.ToString(), out var decimalValue))
                            return (T)(object)decimalValue;
                    }
                    if (typeof(T) == typeof(double?) && value != null)
                    {
                        if (double.TryParse(value.ToString(), out var doubleValue))
                            return (T)(object)doubleValue;
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
    }
}
