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
        private readonly ILogger<AiImportService> _logger;

        public AiImportService(HttpClient httpClient, ILogger<AiImportService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<AnimeImportDto>> ParseRawTextAsync(string rawText)
        {
            var systemPrompt = """
                You are a strict data extraction parser. Your task is to analyze raw, informal user lists of anime and convert them into a valid JSON array.

                OUTPUT FORMAT:
                Return ONLY a raw JSON array of objects with NO markdown formatting, NO backticks, and NO extra text.

                JSON OBJECT SCHEMA:
                - "title": Cleaned official title of the anime/movie (remove notes, ratings, or format descriptions like "(live action)" or movie or anything similar).
                - "status": Must be one of: "watching", "completed", "plan_to_watch", "dropped", or "on_hold". 
                  * If user mentions "watching", "currently at", "and going", or "need to binge more" -> "watching".
                  * If user mentions "all seasons", "good", "finished", or specific seasons watched -> "completed".
                  * Default to "completed" if implied, or "plan_to_watch" if unknown.
                - "score": A numeric rating out of 10 if explicitly mentioned; otherwise null.
                - "notes": Extract any user commentary/thoughts into a string, or null if none.
                """;

            var payload = new
            {
                model = "qwen2.5:1.5b", // e.g., "llama3" or "mistral"
                prompt = $"{systemPrompt}\n\nInput:\n{rawText}",
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // 1. Send request to your local AI endpoint
            var response = await _httpClient.PostAsync("http://host.docker.internal:11434/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // 2. Extract raw AI text response
            using var doc = JsonDocument.Parse(responseBody);
            var rawAiText = doc.RootElement.GetProperty("response").GetString() ?? "[]";
            // LOG RAW AI OUTPUT
            _logger.LogInformation("Raw AI Response from Ollama:\n{RawAiText}", rawAiText);

            // 3. USE CLEANING HELPER HERE before deserializing
            var cleanedJson = CleanJsonResponse(rawAiText);

            // LOG CLEANED JSON OUTPUT
            _logger.LogInformation("Cleaned JSON String:\n{CleanedJson}", cleanedJson);

            // 4. Deserialize into typed list
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<AnimeImportDto>>(cleanedJson, options) ?? new List<AnimeImportDto>();

            // 5. Remove duplicates by Title (case-insensitive)
            return items
                .Where(x => !string.IsNullOrWhiteSpace(x.Title))
                .DistinctBy(x => x.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Helper method placed inside the class
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
