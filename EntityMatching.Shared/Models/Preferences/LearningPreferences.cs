using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class LearningPreferences
    {
        [JsonProperty(PropertyName = "learningStyles")]
        public ICollection<string> LearningStyles { get; set; } = new List<string>(); // Visual, Auditory, Hands-on, etc.

        [JsonProperty(PropertyName = "subjectsOfInterest")]
        public ICollection<string> SubjectsOfInterest { get; set; } = new List<string>(); // History, Science, Art, etc.

        [JsonProperty(PropertyName = "enjoysTryingNewSkills")]
        public bool EnjoysTryingNewSkills { get; set; }

        [JsonProperty(PropertyName = "prefersStructuredLearning")]
        public bool PrefersStructuredLearning { get; set; }

        [JsonProperty(PropertyName = "skillsTheyWantToLearn")]
        public ICollection<string> SkillsTheyWantToLearn { get; set; } = new List<string>();
    }
}
