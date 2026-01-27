using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Strongly-typed entity for career/occupation profiles
    /// Represents career paths enriched with O*NET and BLS data
    /// Examples: Software Engineer, Registered Nurse, Marketing Manager
    /// </summary>
    public class CareerEntity : Entity
    {
        public CareerEntity()
        {
            EntityType = EntityType.Career;
        }

        // === Career-Specific Fields ===

        /// <summary>
        /// O*NET-SOC occupation code (e.g., "15-1252.00" for Software Developers)
        /// </summary>
        [JsonProperty(PropertyName = "onetCode")]
        public string? OnetCode { get; set; }

        /// <summary>
        /// RIASEC interest profile (Holland Codes)
        /// </summary>
        [JsonProperty(PropertyName = "interests")]
        public Dictionary<string, double>? Interests { get; set; }

        /// <summary>
        /// Required skills for this career
        /// </summary>
        [JsonProperty(PropertyName = "skills")]
        public List<string>? Skills { get; set; }

        /// <summary>
        /// Required knowledge areas
        /// </summary>
        [JsonProperty(PropertyName = "knowledge")]
        public List<string>? Knowledge { get; set; }

        /// <summary>
        /// Required abilities
        /// </summary>
        [JsonProperty(PropertyName = "abilities")]
        public List<string>? Abilities { get; set; }

        /// <summary>
        /// Common tasks in this career
        /// </summary>
        [JsonProperty(PropertyName = "tasks")]
        public List<string>? Tasks { get; set; }

        /// <summary>
        /// Work activities
        /// </summary>
        [JsonProperty(PropertyName = "workActivities")]
        public List<string>? WorkActivities { get; set; }

        /// <summary>
        /// Education level required
        /// </summary>
        [JsonProperty(PropertyName = "educationLevel")]
        public string? EducationLevel { get; set; }

        /// <summary>
        /// O*NET Job Zone (1-5, indicating preparation level)
        /// </summary>
        [JsonProperty(PropertyName = "jobZone")]
        public int? JobZone { get; set; }

        /// <summary>
        /// Median annual salary (from BLS)
        /// </summary>
        [JsonProperty(PropertyName = "medianSalary")]
        public decimal? MedianSalary { get; set; }

        /// <summary>
        /// Salary range (10th to 90th percentile)
        /// </summary>
        [JsonProperty(PropertyName = "salaryRange")]
        public SalaryRange? SalaryRange { get; set; }

        /// <summary>
        /// Job growth outlook
        /// </summary>
        [JsonProperty(PropertyName = "growthOutlook")]
        public string? GrowthOutlook { get; set; }

        /// <summary>
        /// Related major CIP codes
        /// </summary>
        [JsonProperty(PropertyName = "relatedMajors")]
        public List<string>? RelatedMajors { get; set; }

        /// <summary>
        /// Sync strongly-typed properties to the base Attributes dictionary
        /// Call this before saving to ensure search filters can access career-specific fields
        /// </summary>
        public void SyncToAttributes()
        {
            if (!string.IsNullOrEmpty(OnetCode))
                SetAttribute("onetCode", OnetCode);

            if (Interests != null && Interests.Count > 0)
                SetAttribute("interests", Interests);

            if (Skills != null && Skills.Count > 0)
                SetAttribute("skills", Skills);

            if (Knowledge != null && Knowledge.Count > 0)
                SetAttribute("knowledge", Knowledge);

            if (Abilities != null && Abilities.Count > 0)
                SetAttribute("abilities", Abilities);

            if (Tasks != null && Tasks.Count > 0)
                SetAttribute("tasks", Tasks);

            if (WorkActivities != null && WorkActivities.Count > 0)
                SetAttribute("workActivities", WorkActivities);

            if (!string.IsNullOrEmpty(EducationLevel))
                SetAttribute("educationLevel", EducationLevel);

            if (JobZone.HasValue)
                SetAttribute("jobZone", JobZone.Value);

            if (MedianSalary.HasValue)
                SetAttribute("medianSalary", MedianSalary.Value);

            if (SalaryRange != null)
                SetAttribute("salaryRange", SalaryRange);

            if (!string.IsNullOrEmpty(GrowthOutlook))
                SetAttribute("growthOutlook", GrowthOutlook);

            if (RelatedMajors != null && RelatedMajors.Count > 0)
                SetAttribute("relatedMajors", RelatedMajors);
        }
    }

    /// <summary>
    /// Salary range information
    /// </summary>
    public class SalaryRange
    {
        [JsonProperty(PropertyName = "min")]
        public decimal Min { get; set; }

        [JsonProperty(PropertyName = "max")]
        public decimal Max { get; set; }

        [JsonProperty(PropertyName = "percentile10")]
        public decimal? Percentile10 { get; set; }

        [JsonProperty(PropertyName = "percentile90")]
        public decimal? Percentile90 { get; set; }
    }
}
