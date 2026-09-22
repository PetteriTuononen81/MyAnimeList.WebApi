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
                You are an exhaustive list extractor. Extract EVERY single anime entry from the user text into the requested JSON schema.
                
                Rules:
                1. Include every title mentioned in the input, no matter how short.
                2. If status is not mentioned, set "status" to "watching".
                3. If score or notes are not explicitly stated, use empty strings or 0 instead of null.
                """;

            var payload = new
            {
                model = "qwen2.5:1.5b",
                prompt = $"{systemPrompt}\n\nInput Text:\n{rawText}",
                // Enforce a strict JSON schema directly via Ollama
                format = new
                {
                    type = "object",
                    properties = new
                    {
                        animes = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title = new { type = "string" },
                                    status = new { type = "string" },
                                    score = new { type = "integer" },
                                    notes = new { type = "string" }
                                },
                                required = new[] { "title", "status" }
                            }
                        }
                    },
                    required = new[] { "animes" }
                },
                stream = false,
                options = new
                {
                    num_predict = 3072,
                    temperature = 0.0
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

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("animes", out var animesElement))
                {
                    if (animesElement.ValueKind == JsonValueKind.Array)
                    {
                        var items = JsonSerializer.Deserialize<List<AnimeImportDto>>(animesElement.GetRawText(), options) ?? new List<AnimeImportDto>();

                        // Sanitize non-explicit values back to clean nulls for your frontend/DTOs
                        foreach (var item in items)
                        {
                            if (item.Score == 0) item.Score = null;
                            if (string.IsNullOrWhiteSpace(item.Notes)) item.Notes = null;
                        }
                        return items;
                    }
                }
            }
            catch (JsonException)
            {
                // Fallback
            }

            return new List<AnimeImportDto>();
        }
    }
}