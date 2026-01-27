using EntityMatching.Core.Models.Embedding;

namespace EntityMatching.Core.Models.Summary
{
    /// <summary>
    /// Result of entity summary generation
    /// Contains the generated summary text and metadata about the summary
    /// </summary>
    public class EntitySummaryResult
    {
        /// <summary>
        /// The generated summary text
        /// </summary>
        public string Summary { get; set; } = "";

        /// <summary>
        /// Metadata about the generated summary
        /// </summary>
        public SummaryMetadata Metadata { get; set; } = new();
    }
}
