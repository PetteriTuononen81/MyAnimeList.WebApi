using System.Text.Json;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Services
{
    public class JikanApiService
    {
        private readonly HttpClient _httpClient;
        private const string JikanBaseUrl = "https://api.jikan.moe/v4";

        public JikanApiService(HttpClient httpClient)
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
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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
            var anime = new Anime
            {
                MalId = element.TryGetProperty("mal_id", out var malId) && malId.ValueKind != System.Text.Json.JsonValueKind.Null 
                    ? malId.GetInt32() 
                    : 0,
                Title = element.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                Episodes = element.TryGetProperty("episodes", out var episodes) && episodes.ValueKind != System.Text.Json.JsonValueKind.Null 
                    ? episodes.GetInt32() 
                    : 0,
                Status = element.TryGetProperty("status", out var status) ? status.GetString() : null,
                Score = element.TryGetProperty("score", out var score) && score.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? score.GetDouble()
                    : null,
                Synopsis = element.TryGetProperty("synopsis", out var synopsis) ? synopsis.GetString() : null,
                ImageUrl = GetImageUrl(element),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (element.TryGetProperty("aired", out var aired))
            {
                if (aired.TryGetProperty("from", out var from) && from.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    if (DateTime.TryParse(from.GetString(), out var fromDate))
                        anime.AiredFrom = fromDate;
                }
                if (aired.TryGetProperty("to", out var to) && to.ValueKind != System.Text.Json.JsonValueKind.Null)
                {
                    if (DateTime.TryParse(to.GetString(), out var toDate))
                        anime.AiredTo = toDate;
                }
            }

            if (element.TryGetProperty("genres", out var genres))
            {
                var genreNames = new List<string>();
                foreach (var genre in genres.EnumerateArray())
                {
                    if (genre.TryGetProperty("name", out var name))
                        genreNames.Add(name.GetString() ?? "Unknown");
                }
                anime.Genre = string.Join(", ", genreNames);
            }

            return anime;
        }

        private string? GetImageUrl(JsonElement element)
        {
            if (element.TryGetProperty("images", out var images))
            {
                if (images.TryGetProperty("jpg", out var jpg) && jpg.TryGetProperty("image_url", out var url))
                    return url.GetString();
            }
            return null;
        }
    }
}