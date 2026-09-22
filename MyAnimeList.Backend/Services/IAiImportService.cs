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
                Extract EVERY single anime listed in the user text. Do NOT stop after one item.

                Respond ONLY with a JSON object containing an "animes" array like this:
                {
                  "animes": [
                    {
                      "title": "Anime Title 1",
                      "status": "watching",
                      "score": 8,
                      "notes": "optional notes"
                    },
                    {
                      "title": "Anime Title 2",
                      "status": "completed",
                      "score": null,
                      "notes": null
                    }
                  ]
                }

                Status must be one of: "watching", "completed", "plan_to_watch", "dropped", "on_hold".
                """;

            var payload = new
            {
                model = "qwen2.5:1.5b",
                prompt = $"{systemPrompt}\n\nInput Text:\n{rawText}",
                format = "json",
                stream = false,
                options = new
                {
                    num_predict = 2048,
                    temperature = 0.1
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseBody);
            var rawAiText = doc.RootElement.GetProperty("response").GetString() ?? "{}";

            var cleanedJson = CleanJsonResponse(rawAiText);

            return ParseAnimeList(cleanedJson);
        }

        private string CleanJsonResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "{}";

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

                // Handle direct object wrapping: {"animes": [...]} or {"items": [...]}
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            return JsonSerializer.Deserialize<List<AnimeImportDto>>(property.Value.GetRawText(), options) ?? new List<AnimeImportDto>();
                        }
                    }

                    // Fallback if model returned a single object instead of array wrapper
                    var singleItem = JsonSerializer.Deserialize<AnimeImportDto>(json, options);
                    return singleItem != null && !string.IsNullOrEmpty(singleItem.Title)
                        ? new List<AnimeImportDto> { singleItem }
                        : new List<AnimeImportDto>();
                }

                // Direct array fallback: [...]
                if (root.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<AnimeImportDto>>(json, options) ?? new List<AnimeImportDto>();
                }
            }
            catch (JsonException)
            {
                // Parse failure fallback
            }

            return new List<AnimeImportDto>();
        }
    }
}