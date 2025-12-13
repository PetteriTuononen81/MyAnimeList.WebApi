using System.Text.Json;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Services
{
    public class JikanApiClient
    {
        private readonly HttpClient _httpClient;
        private const string JikanBaseUrl = "https://api.jikan.moe/v4";

        public JikanApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Anime>> FetchAnimeListAsync(int page = 1, int limit = 25)
        {
            try
            {
                var url = $"{JikanBaseUrl}/anime?page={page}&limit={limit}&order_by=score&sort=desc";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(content);
                var animeList = new List<Anime>();

                if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var anime = ParseAnimeFromJson(item);
                        animeList.Add(anime);
                    }
                }

                return animeList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching anime data: {ex.Message}");
                return new List<Anime>();
            }
        }

        private Anime ParseAnimeFromJson(JsonElement element)
        {
            return new Anime
            {
                MalId = element.GetIntProperty("mal_id"),
                Title = element.GetStringProperty("title") ?? string.Empty,
                Episodes = element.GetIntProperty("episodes"),
                Status = element.GetStringProperty("status"),
                Score = element.GetDoubleProperty("score"),
                Synopsis = element.GetStringProperty("synopsis"),
                ImageUrl = element.GetNestedStringProperty("images", "jpg", "image_url"),
                AiredFrom = element.GetNestedProperty("aired")?.GetDateTimeProperty("from"),
                AiredTo = element.GetNestedProperty("aired")?.GetDateTimeProperty("to"),
                Genre = element.GetArrayAsString("genres", "name"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}