using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Shared.Models
{
    public class DietaryRestrictions
    {
        [JsonProperty(PropertyName = "allergies")]
        public ICollection<string> Allergies { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "restrictions")]
        public ICollection<string> Restrictions { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "severityLevel")]
        public int SeverityLevel { get; set; } // 1-10, 10 being life-threatening

        [JsonProperty(PropertyName = "specialInstructions")]
        public string SpecialInstructions { get; set; } = "";
    }
}
