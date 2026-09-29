using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services.ApiClient;
using MyAnimeList.Backend.Models.Response;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Database.Repositories;

namespace MyAnimeList.Backend.Services;

public interface IAnimeMetadataService
{
    Task<AnimeMetadata?> GetOrFetchMetadataAsync(int malId);
    Task<UserAnalyticsDto> GetUserAnalyticsAsync(int userId);
}

public class AnimeMetadataService : IAnimeMetadataService
{
    private readonly IAniListApiClient _apiClient;
    private readonly IAnimeMetadataRepository _repository;

    public AnimeMetadataService(
        IAniListApiClient apiClient,
        IAnimeMetadataRepository repository)
    {
        _apiClient = apiClient;
        _repository = repository;
    }

    public async Task<AnimeMetadata?> GetOrFetchMetadataAsync(int malId)
    {
        var existsMetadata = await _repository.GetByMalIdAsync(malId);
        if (existsMetadata != null)
        {
            return existsMetadata;
        }

        var media = await _apiClient.GetMediaByMalIdAsync(malId);
        if (media == null) return null;

        var metadata = AnimeMetadata.FromAniListMedia(media);

        await _repository.UpsertAsync(metadata);

        return metadata;
    }

    public async Task<UserAnalyticsDto> GetUserAnalyticsAsync(int userId)
    {
        // Fetch all user's library items metadata from repository
        var metadataList = await _repository.GetMetadataForUserLibraryAsync(userId);

        var demographics = metadataList
            .Where(m => !string.IsNullOrEmpty(m.Demographic))
            .GroupBy(m => m.Demographic!)
            .ToDictionary(g => g.Key, g => g.Count());

        var topGenres = metadataList
            .SelectMany(m => m.Genres)
            .GroupBy(g => g)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        var topThemes = metadataList
            .SelectMany(m => m.Themes)
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        return new UserAnalyticsDto
        {
            Demographics = demographics,
            TopGenres = topGenres,
            TopThemes = topThemes
        };
    }
}