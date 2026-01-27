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
    /// Summary strategy for Job entities
    /// Generates summaries highlighting job requirements, skills, compensation, and company details
    /// </summary>
    public class JobSummaryStrategy : IEntitySummaryStrategy
    {
        private readonly ILogger<JobSummaryStrategy> _logger;

        public EntityType EntityType => EntityType.Job;

        public JobSummaryStrategy(ILogger<JobSummaryStrategy> logger)
        {
            _logger = logger;
        }

        public async Task<EntitySummaryResult> GenerateSummaryAsync(Entity entity, ConversationContext? conversation = null)
        {
            var job = entity as JobEntity;

            var summary = new StringBuilder();
            var metadata = new SummaryMetadata();

            // Basic Information
            summary.AppendLine($"Job Opening: {entity.Name}");
            summary.AppendLine($"Description: {entity.Description}");

            if (job != null)
            {
                // Company & Location
                if (!string.IsNullOrEmpty(job.CompanyName))
                    summary.AppendLine($"Company: {job.CompanyName}");

                if (!string.IsNullOrEmpty(job.Department))
                    summary.AppendLine($"Department: {job.Department}");

                if (!string.IsNullOrEmpty(job.Location))
                {
                    summary.AppendLine(job.RemoteOk
                        ? $"Location: {job.Location} (Remote available)"
                        : $"Location: {job.Location}");
                }
                else if (job.RemoteOk)
                {
                    summary.AppendLine("Location: Remote");
                }

                // Job Details
                if (!string.IsNullOrEmpty(job.EmploymentType))
                    summary.AppendLine($"Employment Type: {job.EmploymentType}");

                if (!string.IsNullOrEmpty(job.Level))
                    summary.AppendLine($"Level: {job.Level}");

                // Requirements Section
                summary.AppendLine();
                summary.AppendLine("=== Requirements ===");

                if (job.RequiredSkills?.Any() == true)
                {
                    summary.AppendLine($"Required Skills: {string.Join(", ", job.RequiredSkills)}");
                    metadata.PreferenceCategories.Add("Required Skills");
                }

                if (job.PreferredSkills?.Any() == true)
                {
                    summary.AppendLine($"Preferred Skills: {string.Join(", ", job.PreferredSkills)}");
                    metadata.PreferenceCategories.Add("Preferred Skills");
                }

                if (job.MinYearsExperience > 0)
                {
                    var expText = job.MaxYearsExperience > 0
                        ? $"{job.MinYearsExperience}-{job.MaxYearsExperience} years"
                        : $"{job.MinYearsExperience}+ years";
                    summary.AppendLine($"Experience Required: {expText}");
                }

                if (!string.IsNullOrEmpty(job.EducationRequired))
                    summary.AppendLine($"Education: {job.EducationRequired}");

                // Compensation & Benefits
                if (job.MinSalary.HasValue || job.MaxSalary.HasValue)
                {
                    summary.AppendLine();
                    summary.AppendLine("=== Compensation ===");

                    if (job.MinSalary.HasValue && job.MaxSalary.HasValue)
                        summary.AppendLine($"Salary Range: ${job.MinSalary:N0} - ${job.MaxSalary:N0}");
                    else if (job.MinSalary.HasValue)
                        summary.AppendLine($"Minimum Salary: ${job.MinSalary:N0}");
                    else if (job.MaxSalary.HasValue)
                        summary.AppendLine($"Maximum Salary: ${job.MaxSalary:N0}");

                    metadata.PreferenceCategories.Add("Compensation");
                }

                if (job.Benefits?.Any() == true)
                {
                    summary.AppendLine($"Benefits: {string.Join(", ", job.Benefits)}");
                }

                // Timeline
                if (job.ApplicationDeadline.HasValue)
                {
                    summary.AppendLine();
                    summary.AppendLine($"Application Deadline: {job.ApplicationDeadline.Value:yyyy-MM-dd}");
                }
            }
            else
            {
                // Fallback: extract from attributes
                AppendFromAttributes(summary, entity, metadata);
            }

            // Conversation Insights (e.g., from conversational job posting creation)
            if (conversation?.ExtractedInsights?.Any() == true)
            {
                metadata.HasConversationData = true;
                metadata.ConversationChunksCount = conversation.ConversationChunks?.Count ?? 0;
                metadata.ExtractedInsightsCount = conversation.ExtractedInsights.Count;

                summary.AppendLine();
                summary.AppendLine("=== Additional Details from Conversations ===");
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
            if (entity.Attributes.TryGetValue("companyName", out var company))
                summary.AppendLine($"Company: {company}");

            if (entity.Attributes.TryGetValue("location", out var location))
                summary.AppendLine($"Location: {location}");

            if (entity.Attributes.TryGetValue("remote", out var remote) && (bool)remote)
                summary.AppendLine("Remote work available");

            summary.AppendLine();
            summary.AppendLine("=== Requirements ===");

            if (entity.Attributes.TryGetValue("requiredSkills", out var reqSkills))
            {
                summary.AppendLine($"Required Skills: {string.Join(", ", (string[])reqSkills)}");
                metadata.PreferenceCategories.Add("Required Skills");
            }

            if (entity.Attributes.TryGetValue("minExperience", out var minExp))
                summary.AppendLine($"Experience: {minExp}+ years");

            if (entity.Attributes.TryGetValue("salaryRange", out var salary))
            {
                try
                {
                    dynamic range = salary;
                    summary.AppendLine($"Salary Range: ${range.min:N0} - ${range.max:N0}");
                    metadata.PreferenceCategories.Add("Compensation");
                }
                catch
                {
                    // Ignore if salary range format is unexpected
                }
            }
        }
    }
}
