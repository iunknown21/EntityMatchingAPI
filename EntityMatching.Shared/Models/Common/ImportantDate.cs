using Newtonsoft.Json;
using System;

namespace EntityMatching.Shared.Models
{
    public class ImportantDate
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; } = "";

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "isRecurring")]
        public bool IsRecurring { get; set; }
    }
}
