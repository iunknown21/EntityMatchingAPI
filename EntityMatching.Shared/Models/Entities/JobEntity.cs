using Newtonsoft.Json;
using System;

namespace EntityMatching.Shared.Models
{
    /// <summary>
    /// Strongly-typed entity for job postings
    /// Enables bidirectional matching: jobs can search for candidates, candidates can search for jobs
    /// </summary>
    public class JobEntity : Entity
    {
        public JobEntity()
        {
            EntityType = EntityType.Job;
        }

        // === Job-Specific Properties ===

        /// <summary>
        /// Company offering the position
        /// </summary>
        [JsonProperty(PropertyName = "companyName")]
        public string CompanyName { get; set; } = "";

        /// <summary>
        /// Department or team
        /// </summary>
        [JsonProperty(PropertyName = "department")]
        public string Department { get; set; } = "";

        /// <summary>
        /// Job location (or "Remote")
        /// </summary>
        [JsonProperty(PropertyName = "location")]
        public string Location { get; set; } = "";

        /// <summary>
        /// Whether remote work is available
        /// </summary>
        [JsonProperty(PropertyName = "remoteOk")]
        public bool RemoteOk { get; set; } = false;

        /// <summary>
        /// Required skills for the position
        /// </summary>
        [JsonProperty(PropertyName = "requiredSkills")]
        public string[] RequiredSkills { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Preferred/nice-to-have skills
        /// </summary>
        [JsonProperty(PropertyName = "preferredSkills")]
        public string[] PreferredSkills { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Minimum years of experience required
        /// </summary>
        [JsonProperty(PropertyName = "minYearsExperience")]
        public int MinYearsExperience { get; set; } = 0;

        /// <summary>
        /// Maximum years of experience (0 = no maximum)
        /// </summary>
        [JsonProperty(PropertyName = "maxYearsExperience")]
        public int MaxYearsExperience { get; set; } = 0;

        /// <summary>
        /// Minimum salary offered
        /// </summary>
        [JsonProperty(PropertyName = "minSalary")]
        public decimal? MinSalary { get; set; }

        /// <summary>
        /// Maximum salary offered
        /// </summary>
        [JsonProperty(PropertyName = "maxSalary")]
        public decimal? MaxSalary { get; set; }

        /// <summary>
        /// Employment type (Full-Time, Part-Time, Contract, etc.)
        /// </summary>
        [JsonProperty(PropertyName = "employmentType")]
        public string EmploymentType { get; set; } = "Full-Time";

        /// <summary>
        /// Job level (Entry, Mid, Senior, Lead, Executive)
        /// </summary>
        [JsonProperty(PropertyName = "level")]
        public string Level { get; set; } = "";

        /// <summary>
        /// Required education level
        /// </summary>
        [JsonProperty(PropertyName = "educationRequired")]
        public string EducationRequired { get; set; } = "";

        /// <summary>
        /// Benefits offered
        /// </summary>
        [JsonProperty(PropertyName = "benefits")]
        public string[] Benefits { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Date when the job was posted
        /// </summary>
        [JsonProperty(PropertyName = "postedDate")]
        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Application deadline
        /// </summary>
        [JsonProperty(PropertyName = "applicationDeadline")]
        public DateTime? ApplicationDeadline { get; set; }

        /// <summary>
        /// Sync strongly-typed properties to the base Attributes dictionary
        /// Call this before saving to ensure search filters can access job-specific fields
        /// </summary>
        public void SyncToAttributes()
        {
            SetAttribute("companyName", CompanyName);
            SetAttribute("department", Department);
            SetAttribute("location", Location);
            SetAttribute("remote", RemoteOk);
            SetAttribute("requiredSkills", RequiredSkills);
            SetAttribute("preferredSkills", PreferredSkills);
            SetAttribute("minExperience", MinYearsExperience);
            SetAttribute("maxExperience", MaxYearsExperience);

            if (MinSalary.HasValue || MaxSalary.HasValue)
            {
                SetAttribute("salaryRange", new
                {
                    min = MinSalary ?? 0,
                    max = MaxSalary ?? 0
                });
            }

            SetAttribute("employmentType", EmploymentType);
            SetAttribute("level", Level);
            SetAttribute("educationRequired", EducationRequired);
            SetAttribute("benefits", Benefits);
            SetAttribute("postedDate", PostedDate);

            if (ApplicationDeadline.HasValue)
                SetAttribute("applicationDeadline", ApplicationDeadline.Value);
        }
    }
}
