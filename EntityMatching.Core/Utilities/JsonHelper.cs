using System.Text.Json;
using System.Text.Json.Serialization;

namespace EntityMatching.Core.Utilities
{
    /// <summary>
    /// Centralized JSON serialization configuration to ensure consistency across the entire application.
    /// This prevents PascalCase vs camelCase mismatches between API responses and client deserialization.
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Standard JsonSerializerOptions for API responses (Azure Functions).
        /// Uses PascalCase to match C# naming conventions.
        /// </summary>
        public static JsonSerializerOptions ApiOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // PascalCase (default C# convention)
            PropertyNameCaseInsensitive = true,
            WriteIndented = false, // Compact JSON for API responses
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter() // Serialize enums as strings
            }
        };

        /// <summary>
        /// Standard JsonSerializerOptions for client-side operations (Blazor).
        /// Case-insensitive to handle both PascalCase and camelCase gracefully.
        /// </summary>
        public static JsonSerializerOptions ClientOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // PascalCase (matches API)
            PropertyNameCaseInsensitive = true, // Tolerant of both cases
            WriteIndented = true, // Pretty-print for debugging
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter() // Serialize enums as strings
            }
        };

        /// <summary>
        /// Options for Cosmos DB operations.
        /// Must match exactly how documents are stored.
        /// </summary>
        public static JsonSerializerOptions CosmosOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // PascalCase
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        /// <summary>
        /// Serialize object to JSON string using API conventions.
        /// </summary>
        public static string SerializeApi<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, ApiOptions);
        }

        /// <summary>
        /// Deserialize JSON string using API conventions.
        /// </summary>
        public static T? DeserializeApi<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, ApiOptions);
        }

        /// <summary>
        /// Serialize object to JSON string using client conventions.
        /// </summary>
        public static string SerializeClient<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, ClientOptions);
        }

        /// <summary>
        /// Deserialize JSON string using client conventions.
        /// </summary>
        public static T? DeserializeClient<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, ClientOptions);
        }
    }
}
