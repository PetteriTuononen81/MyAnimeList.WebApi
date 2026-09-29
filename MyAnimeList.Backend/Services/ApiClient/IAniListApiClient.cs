using System.Net.Http.Json;
using System.Text.Json;
using MyAnimeList.Backend.Models.Dtos;


namespace MyAnimeList.Backend.Services.ApiClient;

public interface IAniListApiClient
{
    Task<AniListMedia?> GetMediaByMalIdAsync(int malId);
}

public class AniListApiClient : IAniListApiClient
{
    private readonly HttpClient _httpClient;

    public AniListApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AniListMedia?> GetMediaByMalIdAsync(int malId)
    {
        var query = @"
            query ($malId: Int) {
              Media (idMal: $malId, type: ANIME) {
                idMal
                genres
                tags {
                  name
                  category
                  isGeneralSpoiler
                  rank
                }
              }
            }";

        var payload = new { query, variables = new { malId } };
        var response = await _httpClient.PostAsJsonAsync("https://graphql.anilist.co", payload);

        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<AniListResponseWrapper>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.Data?.Media;
    }
}