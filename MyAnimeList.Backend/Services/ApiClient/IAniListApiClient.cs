using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyAnimeList.Backend.Models.Dtos;

namespace MyAnimeList.Backend.Services.ApiClient;

public interface IAniListApiClient
{
    Task<AniListMedia?> GetMediaByMalIdAsync(int malId);
}

public class AniListApiClient : IAniListApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AniListApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AniListApiClient(HttpClient httpClient, ILogger<AniListApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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

        _logger.LogInformation("Sending GraphQL request to AniList for MAL ID: {MalId}", malId);

        var response = await _httpClient.PostAsJsonAsync("https://graphql.anilist.co", payload);

        var rawJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "AniList API returned error status code {StatusCode} for MAL ID {MalId}. Response body: {RawJson}",
                response.StatusCode, malId, rawJson);
            return null;
        }

        _logger.LogInformation("AniList API Raw Response for MAL ID {MalId}: {RawJson}", malId, rawJson);

        try
        {
            var result = JsonSerializer.Deserialize<AniListResponseWrapper>(rawJson, JsonOptions);

            _logger.LogInformation(
                "Deserialized AniList Media for MAL ID {MalId}: GenresCount={GenresCount}, TagsCount={TagsCount}",
                malId,
                result?.Data?.Media?.Genres?.Count ?? 0,
                result?.Data?.Media?.Tags?.Count ?? 0);

            return result?.Data?.Media;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize AniList response for MAL ID: {MalId}", malId);
            return null;
        }
    }
}