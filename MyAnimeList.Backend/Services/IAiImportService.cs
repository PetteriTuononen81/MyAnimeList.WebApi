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
                You are a strict data extraction parser. Your task is to analyze raw, informal user lists of anime and convert them into a valid JSON array.

                JSON OBJECT SCHEMA:
                - "title": Cleaned official title of the anime/movie.
                - "status": Must be one of: "watching", "completed", "plan_to_watch", "dropped", or "on_hold".
                - "score": A numeric rating out of 10 if explicitly mentioned; otherwise null.
                - "notes": Extract any user commentary into a string, or null if none.
                """;

            var payload = new
            {
                model = "qwen2.5:1.5b",
                prompt = $"{systemPrompt}\n\nInput:\n{rawText}",
                format = "json", 
                stream = false,
                options = new
                {
                    num_predict = 500
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Uses relative URI configured via HttpClient BaseAddress in Program.cs
            var response = await _httpClient.PostAsync("api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseBody);
            var rawAiText = doc.RootElement.GetProperty("response").GetString() ?? "[]";

            var cleanedJson = CleanJsonResponse(rawAiText);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<AnimeImportDto>>(cleanedJson, options) ?? new List<AnimeImportDto>();
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
    }
}