using Newtonsoft.Json;
using System.Collections.Generic;

namespace EntityMatching.Core.Models.Search
{
    /// <summary>
    /// Filter operator for comparing values
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Exact match (==)
        /// </summary>
        Equals = 0,

        /// <summary>
        /// Not equal (!=)
        /// </summary>
        NotEquals = 1,

        /// <summary>
        /// String contains substring or collection contains value
        /// </summary>
        Contains = 2,

        /// <summary>
        /// String does not contain substring or collection does not contain value
        /// </summary>
        NotContains = 3,

        /// <summary>
        /// Numeric greater than (&gt;)
        /// </summary>
        GreaterThan = 4,

        /// <summary>
        /// Numeric less than (&lt;)
        /// </summary>
        LessThan = 5,

        /// <summary>
        /// Numeric greater than or equal (&gt;=)
        /// </summary>
        GreaterOrEqual = 6,

        /// <summary>
        /// Numeric less than or equal (&lt;=)
        /// </summary>
        LessOrEqual = 7,

        /// <summary>
        /// Numeric value in range (min &lt;= value &lt;= max)
        /// </summary>
        InRange = 8,

        /// <summary>
        /// Boolean equals true
        /// </summary>
        IsTrue = 9,

        /// <summary>
        /// Boolean equals false
        /// </summary>
        IsFalse = 10,

        /// <summary>
        /// Field is not null/empty
        /// </summary>
        Exists = 11,

        /// <summary>
        /// Field is null/empty
        /// </summary>
        NotExists = 12
    }

    /// <summary>
    /// Logical operator for combining filters
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>
        /// All filters must match (AND)
        /// </summary>
        And = 0,

        /// <summary>
        /// At least one filter must match (OR)
        /// </summary>
        Or = 1
    }

    /// <summary>
    /// Single attribute filter criterion
    /// Example: { "fieldPath": "naturePreferences.hasPets", "operator": "IsTrue" }
    /// Example: { "fieldPath": "personalityClassifications.mbtiType", "operator": "Equals", "value": "INTJ" }
    /// Example: { "fieldPath": "preferences.favoriteCuisines", "operator": "Contains", "value": "Italian" }
    /// </summary>
    public class AttributeFilter
    {
        /// <summary>
        /// JSON path to the field (dot notation)
        /// Examples:
        ///   - "name"
        ///   - "naturePreferences.hasPets"
        ///   - "preferences.favoriteCuisines"
        ///   - "personalityClassifications.extraversion"
        /// </summary>
        [JsonProperty(PropertyName = "fieldPath")]
        public string FieldPath { get; set; } = "";

        /// <summary>
        /// Comparison operator
        /// </summary>
        [JsonProperty(PropertyName = "operator")]
        public FilterOperator Operator { get; set; }

        /// <summary>
        /// Value to compare against (type depends on field and operator)
        /// For collections: single value to check membership
        /// For ranges: use MinValue/MaxValue instead
        /// Examples:
        ///   - String: "INTJ", "male", "Italian"
        ///   - Number: 7, 25, 3.14
        ///   - Boolean: true, false
        ///   - Collection: ["Dog", "Cat"]
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public object? Value { get; set; }

        /// <summary>
        /// Optional: Minimum value for InRange operator
        /// Example: For age 25-35, minValue=25
        /// </summary>
        [JsonProperty(PropertyName = "minValue")]
        public object? MinValue { get; set; }

        /// <summary>
        /// Optional: Maximum value for InRange operator
        /// Example: For age 25-35, maxValue=35
        /// </summary>
        [JsonProperty(PropertyName = "maxValue")]
        public object? MaxValue { get; set; }
    }

    /// <summary>
    /// Group of filters with AND/OR logic
    /// Supports nested groups for complex queries
    /// Example: (A AND B) OR (C AND D)
    /// </summary>
    public class FilterGroup
    {
        /// <summary>
        /// Logical operator for combining filters in this group
        /// </summary>
        [JsonProperty(PropertyName = "logicalOperator")]
        public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;

        /// <summary>
        /// List of attribute filters in this group
        /// </summary>
        [JsonProperty(PropertyName = "filters")]
        public List<AttributeFilter> Filters { get; set; } = new List<AttributeFilter>();

        /// <summary>
        /// Nested filter groups for complex queries
        /// Example:
        /// {
        ///   "logicalOperator": "Or",
        ///   "filters": [],
        ///   "nestedGroups": [
        ///     {
        ///       "logicalOperator": "And",
        ///       "filters": [ ...filters for group A... ]
        ///     },
        ///     {
        ///       "logicalOperator": "And",
        ///       "filters": [ ...filters for group B... ]
        ///     }
        ///   ]
        /// }
        /// Result: (Group A) OR (Group B)
        /// </summary>
        [JsonProperty(PropertyName = "nestedGroups")]
        public List<FilterGroup> NestedGroups { get; set; } = new List<FilterGroup>();

        /// <summary>
        /// Check if this filter group has any filters or nested groups
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool HasFilters => (Filters != null && Filters.Count > 0) ||
                                  (NestedGroups != null && NestedGroups.Count > 0);
    }
}
