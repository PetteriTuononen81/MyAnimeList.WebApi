using MyAnimeList.Backend.Models.Dtos;
using System.Text;
using System.Text.Json;

namespace MyAnimeList.Backend.Services
{
    public interface IAiImportService
    {
        Task<List<AnimeImportDto>> ParseRawTextAsync(string rawText);
    }

    public class AiImportService : IAiImportService
    {
        private readonly HttpClient _httpClient;

        public AiImportService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AnimeImportDto>> ParseRawTextAsync(string rawText)
        {
            var systemPrompt = """
                You are a strict data extraction parser. Analyze raw user lists of anime and convert them into a JSON array of items.

                Always respond with a JSON array containing objects matching this schema:
                [
                  {
                    "title": "Cleaned official title",
                    "status": "watching | completed | plan_to_watch | dropped | on_hold",
                    "score": 8,
                    "notes": "Extract commentary or null"
                  }
                ]
                Do not wrap the array inside a parent object.
                """;

            var payload = new
            {
                model = "qwen2.5:1.5b",
                prompt = $"{systemPrompt}\n\nInput:\n{rawText}",
                format = "json",
                stream = false,
                options = new
                {
                    num_predict = 1000
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseBody);
            var rawAiText = doc.RootElement.GetProperty("response").GetString() ?? "[]";

            var cleanedJson = CleanJsonResponse(rawAiText);

            return ParseAnimeList(cleanedJson);
        }

        private string CleanJsonResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "[]";

            var cleaned = input.Trim();
            if (cleaned.StartsWith("```json")) cleaned = cleaned.Substring(7);
            if (cleaned.StartsWith("```")) cleaned = cleaned.Substring(3);
            if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);

            return cleaned.Trim();
        }

        private List<AnimeImportDto> ParseAnimeList(string json)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                using var parseDoc = JsonDocument.Parse(json);
                var root = parseDoc.RootElement;

                // Case 1: Root is directly an array [...]
                if (root.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<AnimeImportDto>>(json, options) ?? new List<AnimeImportDto>();
                }

                // Case 2: Root is an object {...} containing an array property (e.g. {"items": [...]})
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            return JsonSerializer.Deserialize<List<AnimeImportDto>>(property.Value.GetRawText(), options) ?? new List<AnimeImportDto>();
                        }
                    }

                    // Case 3: Root is a single anime object
                    var singleItem = JsonSerializer.Deserialize<AnimeImportDto>(json, options);
                    return singleItem != null ? new List<AnimeImportDto> { singleItem } : new List<AnimeImportDto>();
                }
            }
            catch (JsonException)
            {
                // Fallback on parse failure
            }

            return new List<AnimeImportDto>();
        }
    }
}