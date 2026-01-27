using Microsoft.Extensions.Logging;
using EntityMatching.Core.Interfaces;
using EntityMatching.Shared.Models;
using EntityMatching.Core.Models.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EntityMatching.Infrastructure.Services
{
    /// <summary>
    /// Service for evaluating structured attribute filters on profiles with privacy enforcement
    /// Uses reflection to access nested properties via JSON path notation
    /// </summary>
    public class AttributeFilterService : IAttributeFilterService
    {
        private readonly ILogger<AttributeFilterService> _logger;

        public AttributeFilterService(ILogger<AttributeFilterService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool EvaluateFilters(
            Entity profile,
            FilterGroup filterGroup,
            string? requestingUserId,
            bool enforcePrivacy)
        {
            if (profile == null || filterGroup == null || !filterGroup.HasFilters)
            {
                return true; // No filters = match all
            }

            // Evaluate all filters in this group
            var filterResults = new List<bool>();

            foreach (var filter in filterGroup.Filters ?? new List<AttributeFilter>())
            {
                // Check field visibility first
                if (enforcePrivacy && !profile.IsFieldVisibleToUser(filter.FieldPath, requestingUserId))
                {
                    _logger.LogDebug(
                        "Filter on field '{FieldPath}' skipped due to privacy settings for user '{UserId}'",
                        filter.FieldPath, requestingUserId ?? "anonymous");

                    // Fail-closed: If field not visible, skip this filter (don't count as match or non-match)
                    continue;
                }

                // Get field value using reflection
                var fieldValue = GetFieldValue(profile, filter.FieldPath);

                // Evaluate the filter
                var matches = EvaluateFilter(filter, fieldValue);
                filterResults.Add(matches);
            }

            // Evaluate nested groups recursively
            foreach (var nestedGroup in filterGroup.NestedGroups ?? new List<FilterGroup>())
            {
                var nestedResult = EvaluateFilters(profile, nestedGroup, requestingUserId, enforcePrivacy);
                filterResults.Add(nestedResult);
            }

            // If no filters evaluated (all skipped due to privacy), return false (fail-closed)
            if (filterResults.Count == 0)
            {
                return false;
            }

            // Combine results based on logical operator
            return filterGroup.LogicalOperator == LogicalOperator.And
                ? filterResults.All(r => r) // AND: all must be true
                : filterResults.Any(r => r); // OR: at least one must be true
        }

        /// <inheritdoc/>
        public Dictionary<string, object> GetMatchedAttributes(
            Entity profile,
            FilterGroup filterGroup,
            string? requestingUserId,
            bool enforcePrivacy)
        {
            var matchedAttributes = new Dictionary<string, object>();

            if (profile == null || filterGroup == null || !filterGroup.HasFilters)
            {
                return matchedAttributes;
            }

            // Collect matched attributes from all filters
            foreach (var filter in filterGroup.Filters ?? new List<AttributeFilter>())
            {
                // Check field visibility
                if (enforcePrivacy && !profile.IsFieldVisibleToUser(filter.FieldPath, requestingUserId))
                {
                    continue; // Skip private fields
                }

                var fieldValue = GetFieldValue(profile, filter.FieldPath);
                if (fieldValue != null)
                {
                    matchedAttributes[filter.FieldPath] = fieldValue;
                }
            }

            // Recursively collect from nested groups
            foreach (var nestedGroup in filterGroup.NestedGroups ?? new List<FilterGroup>())
            {
                var nestedAttributes = GetMatchedAttributes(profile, nestedGroup, requestingUserId, enforcePrivacy);
                foreach (var kvp in nestedAttributes)
                {
                    matchedAttributes[kvp.Key] = kvp.Value;
                }
            }

            return matchedAttributes;
        }

        /// <inheritdoc/>
        public string BuildCosmosQuery(FilterGroup filterGroup)
        {
            if (filterGroup == null || !filterGroup.HasFilters)
            {
                return "";
            }

            var conditions = new List<string>();

            foreach (var filter in filterGroup.Filters ?? new List<AttributeFilter>())
            {
                var condition = BuildCosmosCondition(filter);
                if (!string.IsNullOrEmpty(condition))
                {
                    conditions.Add(condition);
                }
            }

            // Handle nested groups recursively
            foreach (var nestedGroup in filterGroup.NestedGroups ?? new List<FilterGroup>())
            {
                var nestedQuery = BuildCosmosQuery(nestedGroup);
                if (!string.IsNullOrEmpty(nestedQuery))
                {
                    conditions.Add($"({nestedQuery})");
                }
            }

            if (conditions.Count == 0)
            {
                return "";
            }

            var logicalOp = filterGroup.LogicalOperator == LogicalOperator.And ? " AND " : " OR ";
            return string.Join(logicalOp, conditions);
        }

        /// <inheritdoc/>
        public bool CanEvaluateInCosmosDb(FilterGroup filterGroup)
        {
            if (filterGroup == null || !filterGroup.HasFilters)
            {
                return true;
            }

            // Check all filters in this group
            foreach (var filter in filterGroup.Filters ?? new List<AttributeFilter>())
            {
                // Computed properties require app-level evaluation
                if (filter.FieldPath.Equals("age", StringComparison.OrdinalIgnoreCase) ||
                    filter.FieldPath.Equals("location", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Contains on strings requires case-insensitive search (harder in Cosmos)
                if (filter.Operator == FilterOperator.Contains || filter.Operator == FilterOperator.NotContains)
                {
                    return false;
                }
            }

            // Check nested groups recursively
            foreach (var nestedGroup in filterGroup.NestedGroups ?? new List<FilterGroup>())
            {
                if (!CanEvaluateInCosmosDb(nestedGroup))
                {
                    return false;
                }
            }

            return true;
        }

        #region Private Helper Methods

        /// <summary>
        /// Get field value from profile using JSON path notation
        /// Supports nested properties: "naturePreferences.hasPets"
        /// </summary>
        private object? GetFieldValue(Entity profile, string fieldPath)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
            {
                return null;
            }

            try
            {
                var parts = fieldPath.Split('.');
                object? currentValue = profile;

                foreach (var part in parts)
                {
                    if (currentValue == null)
                    {
                        return null;
                    }

                    var propertyInfo = currentValue.GetType().GetProperty(
                        part,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (propertyInfo == null)
                    {
                        _logger.LogWarning("Property '{PropertyName}' not found in type '{TypeName}'",
                            part, currentValue.GetType().Name);
                        return null;
                    }

                    currentValue = propertyInfo.GetValue(currentValue);
                }

                return currentValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field value for path '{FieldPath}'", fieldPath);
                return null;
            }
        }

        /// <summary>
        /// Evaluate a single filter against a field value
        /// </summary>
        private bool EvaluateFilter(AttributeFilter filter, object? fieldValue)
        {
            try
            {
                switch (filter.Operator)
                {
                    case FilterOperator.Equals:
                        return AreEqual(fieldValue, filter.Value);

                    case FilterOperator.NotEquals:
                        return !AreEqual(fieldValue, filter.Value);

                    case FilterOperator.Contains:
                        return Contains(fieldValue, filter.Value);

                    case FilterOperator.NotContains:
                        return !Contains(fieldValue, filter.Value);

                    case FilterOperator.GreaterThan:
                        return CompareNumeric(fieldValue, filter.Value) > 0;

                    case FilterOperator.LessThan:
                        return CompareNumeric(fieldValue, filter.Value) < 0;

                    case FilterOperator.GreaterOrEqual:
                        return CompareNumeric(fieldValue, filter.Value) >= 0;

                    case FilterOperator.LessOrEqual:
                        return CompareNumeric(fieldValue, filter.Value) <= 0;

                    case FilterOperator.InRange:
                        var minCompare = CompareNumeric(fieldValue, filter.MinValue);
                        var maxCompare = CompareNumeric(fieldValue, filter.MaxValue);
                        return minCompare >= 0 && maxCompare <= 0;

                    case FilterOperator.IsTrue:
                        return fieldValue is bool b && b == true;

                    case FilterOperator.IsFalse:
                        return fieldValue is bool bf && bf == false;

                    case FilterOperator.Exists:
                        return fieldValue != null && !IsEmpty(fieldValue);

                    case FilterOperator.NotExists:
                        return fieldValue == null || IsEmpty(fieldValue);

                    default:
                        _logger.LogWarning("Unknown filter operator: {Operator}", filter.Operator);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating filter with operator {Operator}", filter.Operator);
                return false;
            }
        }

        /// <summary>
        /// Check if two values are equal (handles strings, numbers, bools)
        /// </summary>
        private bool AreEqual(object? fieldValue, object? filterValue)
        {
            if (fieldValue == null && filterValue == null) return true;
            if (fieldValue == null || filterValue == null) return false;

            // String comparison (case-insensitive)
            if (fieldValue is string strField && filterValue is string strFilter)
            {
                return strField.Equals(strFilter, StringComparison.OrdinalIgnoreCase);
            }

            // Numeric comparison
            if (IsNumeric(fieldValue) && IsNumeric(filterValue))
            {
                return Convert.ToDouble(fieldValue) == Convert.ToDouble(filterValue);
            }

            // Default comparison
            return fieldValue.Equals(filterValue);
        }

        /// <summary>
        /// Check if field value contains filter value
        /// Works for: strings (substring), collections (membership)
        /// </summary>
        private bool Contains(object? fieldValue, object? filterValue)
        {
            if (fieldValue == null || filterValue == null) return false;

            // String contains (case-insensitive)
            if (fieldValue is string strField && filterValue is string strFilter)
            {
                return strField.Contains(strFilter, StringComparison.OrdinalIgnoreCase);
            }

            // Collection contains
            if (fieldValue is IEnumerable enumerable && !(fieldValue is string))
            {
                foreach (var item in enumerable)
                {
                    if (AreEqual(item, filterValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Compare two numeric values
        /// Returns: -1 (field < filter), 0 (equal), 1 (field > filter)
        /// </summary>
        private int CompareNumeric(object? fieldValue, object? filterValue)
        {
            if (fieldValue == null || filterValue == null)
            {
                throw new InvalidOperationException("Cannot compare null values");
            }

            if (!IsNumeric(fieldValue) || !IsNumeric(filterValue))
            {
                throw new InvalidOperationException("Values are not numeric");
            }

            var fieldDouble = Convert.ToDouble(fieldValue);
            var filterDouble = Convert.ToDouble(filterValue);

            return fieldDouble.CompareTo(filterDouble);
        }

        /// <summary>
        /// Check if value is numeric type
        /// </summary>
        private bool IsNumeric(object value)
        {
            return value is int || value is long || value is float || value is double || value is decimal;
        }

        /// <summary>
        /// Check if value is empty (null, empty string, empty collection)
        /// </summary>
        private bool IsEmpty(object? value)
        {
            if (value == null) return true;
            if (value is string str) return string.IsNullOrWhiteSpace(str);
            if (value is ICollection collection) return collection.Count == 0;
            if (value is IEnumerable enumerable && !(value is string))
            {
                return !enumerable.Cast<object>().Any();
            }
            return false;
        }

        /// <summary>
        /// Build Cosmos DB SQL condition for a single filter
        /// </summary>
        private string BuildCosmosCondition(AttributeFilter filter)
        {
            var fieldPath = $"c.{filter.FieldPath}";

            switch (filter.Operator)
            {
                case FilterOperator.Equals:
                    return $"{fieldPath} = {FormatCosmosValue(filter.Value)}";

                case FilterOperator.NotEquals:
                    return $"{fieldPath} != {FormatCosmosValue(filter.Value)}";

                case FilterOperator.GreaterThan:
                    return $"{fieldPath} > {FormatCosmosValue(filter.Value)}";

                case FilterOperator.LessThan:
                    return $"{fieldPath} < {FormatCosmosValue(filter.Value)}";

                case FilterOperator.GreaterOrEqual:
                    return $"{fieldPath} >= {FormatCosmosValue(filter.Value)}";

                case FilterOperator.LessOrEqual:
                    return $"{fieldPath} <= {FormatCosmosValue(filter.Value)}";

                case FilterOperator.InRange:
                    return $"{fieldPath} >= {FormatCosmosValue(filter.MinValue)} AND {fieldPath} <= {FormatCosmosValue(filter.MaxValue)}";

                case FilterOperator.IsTrue:
                    return $"{fieldPath} = true";

                case FilterOperator.IsFalse:
                    return $"{fieldPath} = false";

                case FilterOperator.Exists:
                    return $"IS_DEFINED({fieldPath}) AND {fieldPath} != null";

                case FilterOperator.NotExists:
                    return $"(NOT IS_DEFINED({fieldPath}) OR {fieldPath} = null)";

                default:
                    _logger.LogWarning("Cannot build Cosmos query for operator: {Operator}", filter.Operator);
                    return "";
            }
        }

        /// <summary>
        /// Format value for Cosmos DB SQL query
        /// </summary>
        private string FormatCosmosValue(object? value)
        {
            if (value == null) return "null";
            if (value is string str) return $"'{str.Replace("'", "''")}'";
            if (value is bool b) return b.ToString().ToLower();
            if (IsNumeric(value)) return value.ToString() ?? "0";
            return $"'{value}'";
        }

        #endregion
    }
}
