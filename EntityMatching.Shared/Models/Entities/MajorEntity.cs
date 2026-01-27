using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Strongly-typed entity for academic major/degree program profiles
    /// Represents educational programs enriched with career pathway data
    /// Examples: Computer Science, Mechanical Engineering, Psychology
    /// </summary>
    public class MajorEntity : Entity
    {
        public MajorEntity()
        {
            EntityType = EntityType.Major;
        }

        // === Major-Specific Fields ===

        /// <summary>
        /// CIP (Classification of Instructional Programs) code (e.g., "11.0701" for Computer Science)
        /// </summary>
        [JsonProperty(PropertyName = "cipCode")]
        public string? CipCode { get; set; }

        /// <summary>
        /// Level of degree (Associate, Bachelor's, Master's, Doctoral)
        /// </summary>
        [JsonProperty(PropertyName = "degreeLevel")]
        public string? DegreeLevel { get; set; }

        /// <summary>
        /// Field of study category (STEM, Business, Arts, etc.)
        /// </summary>
        [JsonProperty(PropertyName = "fieldCategory")]
        public string? FieldCategory { get; set; }

        /// <summary>
        /// Core competencies developed in this major
        /// </summary>
        [JsonProperty(PropertyName = "coreCompetencies")]
        public List<string>? CoreCompetencies { get; set; }

        /// <summary>
        /// Typical courses in this major
        /// </summary>
        [JsonProperty(PropertyName = "typicalCourses")]
        public List<string>? TypicalCourses { get; set; }

        /// <summary>
        /// Skills developed through this major
        /// </summary>
        [JsonProperty(PropertyName = "skillsDeveloped")]
        public List<string>? SkillsDeveloped { get; set; }

        /// <summary>
        /// Related career O*NET codes
        /// </summary>
        [JsonProperty(PropertyName = "relatedCareers")]
        public List<string>? RelatedCareers { get; set; }

        /// <summary>
        /// Career pathways with this major
        /// </summary>
        [JsonProperty(PropertyName = "careerPathways")]
        public List<CareerPathway>? CareerPathways { get; set; }

        /// <summary>
        /// Average starting salary for graduates
        /// </summary>
        [JsonProperty(PropertyName = "averageStartingSalary")]
        public decimal? AverageStartingSalary { get; set; }

        /// <summary>
        /// Mid-career salary for this major
        /// </summary>
        [JsonProperty(PropertyName = "midCareerSalary")]
        public decimal? MidCareerSalary { get; set; }

        /// <summary>
        /// Employment rate for graduates
        /// </summary>
        [JsonProperty(PropertyName = "employmentRate")]
        public double? EmploymentRate { get; set; }

        /// <summary>
        /// Common industries for graduates
        /// </summary>
        [JsonProperty(PropertyName = "commonIndustries")]
        public List<string>? CommonIndustries { get; set; }

        /// <summary>
        /// Sync strongly-typed properties to the base Attributes dictionary
        /// Call this before saving to ensure search filters can access major-specific fields
        /// </summary>
        public void SyncToAttributes()
        {
            if (!string.IsNullOrEmpty(CipCode))
                SetAttribute("cipCode", CipCode);

            if (!string.IsNullOrEmpty(DegreeLevel))
                SetAttribute("degreeLevel", DegreeLevel);

            if (!string.IsNullOrEmpty(FieldCategory))
                SetAttribute("fieldCategory", FieldCategory);

            if (CoreCompetencies != null && CoreCompetencies.Count > 0)
                SetAttribute("coreCompetencies", CoreCompetencies);

            if (TypicalCourses != null && TypicalCourses.Count > 0)
                SetAttribute("typicalCourses", TypicalCourses);

            if (SkillsDeveloped != null && SkillsDeveloped.Count > 0)
                SetAttribute("skillsDeveloped", SkillsDeveloped);

            if (RelatedCareers != null && RelatedCareers.Count > 0)
                SetAttribute("relatedCareers", RelatedCareers);

            if (CareerPathways != null && CareerPathways.Count > 0)
                SetAttribute("careerPathways", CareerPathways);

            if (AverageStartingSalary.HasValue)
                SetAttribute("averageStartingSalary", AverageStartingSalary.Value);

            if (MidCareerSalary.HasValue)
                SetAttribute("midCareerSalary", MidCareerSalary.Value);

            if (EmploymentRate.HasValue)
                SetAttribute("employmentRate", EmploymentRate.Value);

            if (CommonIndustries != null && CommonIndustries.Count > 0)
                SetAttribute("commonIndustries", CommonIndustries);
        }
    }

    /// <summary>
    /// Career pathway information for a major
    /// </summary>
    public class CareerPathway
    {
        [JsonProperty(PropertyName = "careerTitle")]
        public string CareerTitle { get; set; } = "";

        [JsonProperty(PropertyName = "onetCode")]
        public string? OnetCode { get; set; }

        [JsonProperty(PropertyName = "matchPercentage")]
        public double MatchPercentage { get; set; }

        [JsonProperty(PropertyName = "typicalPath")]
        public string? TypicalPath { get; set; }
    }
}
