using System.Text.Json;
using MyAnimeList.Backend.Helpers;
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

        /// <summary>
        /// Fetches a single page of anime data from Jikan API
        /// </summary>
        public async Task<JikanApiResponse> FetchAnimePageAsync(int page = 1, int limit = 25)
        {
            try
            {
                var url = $"{JikanBaseUrl}/anime?page={page}&limit={limit}&order_by=score&sort=desc";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(content);
                var animeList = new List<Anime>();

                // Parse pagination info
                int lastPage = 1;
                bool hasNextPage = false;

                if (jsonDocument.RootElement.TryGetProperty("pagination", out var pagination))
                {
                    lastPage = pagination.GetIntProperty("last_visible_page", 1);
                    hasNextPage = pagination.GetBoolProperty("has_next_page", false);
                }

                // Parse anime data
                if (jsonDocument.RootElement.TryGetProperty("data", out var dataElement))
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var anime = ParseAnimeFromJson(item);
                        animeList.Add(anime);
                    }
                }

                return new JikanApiResponse
                {
                    Data = animeList,
                    CurrentPage = page,
                    LastPage = lastPage,
                    HasNextPage = hasNextPage
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching anime data: {ex.Message}");
                return new JikanApiResponse
                {
                    Data = new List<Anime>(),
                    CurrentPage = page,
                    LastPage = 1,
                    HasNextPage = false
                };
            }
        }

        /// <summary>
        /// Legacy method - fetches just the anime list (for backward compatibility)
        /// </summary>
        public async Task<List<Anime>> FetchAnimeListAsync(int page = 1, int limit = 25)
        {
            var response = await FetchAnimePageAsync(page, limit);
            return response.Data;
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

    /// <summary>
    /// Response from Jikan API including pagination info
    /// </summary>
    public class JikanApiResponse
    {
        public List<Anime> Data { get; set; } = new();
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}