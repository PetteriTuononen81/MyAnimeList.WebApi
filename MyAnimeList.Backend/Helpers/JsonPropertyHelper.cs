using System.Text.Json;

namespace MyAnimeList.Backend.Helpers
{
    /// <summary>
    /// Helper class for safely extracting typed values from JsonElement properties
    /// </summary>
    public static class JsonPropertyHelper
    {
        /// <summary>
        /// Gets an integer property from a JsonElement, returning a default value if not found or null
        /// </summary>
        public static int GetIntProperty(this JsonElement element, string propertyName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
            {
                try
                {
                    return property.GetInt32();
                }
                catch (InvalidOperationException)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a string property from a JsonElement, returning null if not found or null
        /// </summary>
        public static string? GetStringProperty(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
            {
                return property.GetString();
            }
            return null;
        }

        /// <summary>
        /// Gets a double property from a JsonElement, returning null if not found or null
        /// </summary>
        public static double? GetDoubleProperty(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
            {
                try
                {
                    return property.GetDouble();
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a boolean property from a JsonElement, returning a default value if not found or null
        /// </summary>
        public static bool GetBoolProperty(this JsonElement element, string propertyName, bool defaultValue = false)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
            {
                try
                {
                    return property.GetBoolean();
                }
                catch (InvalidOperationException)
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a nested JsonElement property
        /// </summary>
        public static JsonElement? GetNestedProperty(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
            {
                return property;
            }
            return null;
        }

        /// <summary>
        /// Gets a DateTime property from a JsonElement string representation
        /// </summary>
        public static DateTime? GetDateTimeProperty(this JsonElement element, string propertyName)
        {
            var dateString = element.GetStringProperty(propertyName);
            if (dateString != null && DateTime.TryParse(dateString, out var date))
            {
                return new DateTime(date.Ticks, DateTimeKind.Utc);
            }
            return null;
        }

        /// <summary>
        /// Gets a comma-separated string of items from an array property
        /// </summary>
        public static string? GetArrayAsString(this JsonElement element, string propertyName, string itemPropertyName, string separator = ", ")
        {
            var arrayElement = element.GetNestedProperty(propertyName);
            if (arrayElement == null)
                return null;

            var items = arrayElement.Value.EnumerateArray()
                .Select(item => item.GetStringProperty(itemPropertyName) ?? "Unknown")
                .ToList();

            return items.Any() ? string.Join(separator, items) : null;
        }

        /// <summary>
        /// Gets a nested string property through multiple levels
        /// </summary>
        public static string? GetNestedStringProperty(this JsonElement element, params string[] propertyPath)
        {
            JsonElement current = element;

            foreach (var propertyName in propertyPath[..^1])
            {
                var nested = current.GetNestedProperty(propertyName);
                if (nested == null)
                    return null;

                current = nested.Value;
            }

            return current.GetStringProperty(propertyPath[^1]);
        }

        /// <summary>
        /// Parses the titles array from Jikan API v4 format
        /// Returns a list of tuples containing (Type, Title)
        /// </summary>
        public static List<(string Type, string Title)> GetTitlesArray(this JsonElement element, string propertyName = "titles")
        {
            var titles = new List<(string Type, string Title)>();

            if (element.TryGetProperty(propertyName, out var titlesArray) && 
                titlesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var titleElement in titlesArray.EnumerateArray())
                {
                    var type = titleElement.GetStringProperty("type");
                    var title = titleElement.GetStringProperty("title");

                    if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(title))
                    {
                        titles.Add((type, title));
                    }
                }
            }

            return titles;
        }
    }
}