using System.Text.Json;
using MyAnimeList.Backend.Helpers;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Services.ApiClient
{

    public class TenraiApiClient : IAnimeApiClient
    {
        private readonly HttpClient _httpClient;
        private const string TenraiBaseUrl = "https://api.tenrai.org/v1";

        public TenraiApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Fetches a single page of anime data from Tenrai API
        /// </summary>
        public async Task<AnimeApiResponse> FetchAnimePageAsync(int page = 1, int limit = 25)
        {
            try
            {
                var url = $"{TenraiBaseUrl}/anime?page={page}&limit={limit}";
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"[TenraiApiClient] Response Status: {(int)response.StatusCode} {response.StatusCode} for URL: {url}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[TenraiApiClient] Received {content.Length} bytes of data from Tenrai API.");

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

                return new AnimeApiResponse
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
                return new AnimeApiResponse
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
            var titles = element.GetTitlesArray();
            var malId = element.GetIntProperty("mal_id");

            var defaultTitle = titles.FirstOrDefault(t => t.Type.Equals("Default", StringComparison.OrdinalIgnoreCase)).Title
                ?? element.GetStringProperty("title")
                ?? string.Empty;

            var englishTitle = titles.FirstOrDefault(t => t.Type.Equals("English", StringComparison.OrdinalIgnoreCase)).Title
                ?? element.GetStringProperty("title_english");

            var anime = new Anime
            {
                MalId = malId,
                Title = defaultTitle,
                EnglishTitle = englishTitle,
                Episodes = element.GetIntProperty("episodes"),
                Status = element.GetStringProperty("status"),
                Score = element.GetDoubleProperty("score"),
                Synopsis = element.GetStringProperty("synopsis"),
                ImageUrl = element.GetNestedStringProperty("images", "jpg", "image_url"),
                AiredFrom = element.GetNestedProperty("aired")?.GetDateTimeProperty("from"),
                AiredTo = element.GetNestedProperty("aired")?.GetDateTimeProperty("to"),
                Genre = element.GetArrayAsString("genres", "name"),
                Studio = element.GetArrayAsString("studios", "name"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var (type, title) in titles)
            {
                anime.Titles.Add(new AnimeTitle
                {
                    MalId = malId,
                    Type = type,
                    Title = title
                });
            }

            return anime;
        }
    }

}