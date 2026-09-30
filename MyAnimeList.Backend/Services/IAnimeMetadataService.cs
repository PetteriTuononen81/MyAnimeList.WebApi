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
    private readonly ILibraryService _libraryService;
    private readonly ILogger<AnimeMetadataService> _logger;

    public AnimeMetadataService(
        IAniListApiClient apiClient,
        IAnimeMetadataRepository repository,
        ILibraryService libraryService,
        ILogger<AnimeMetadataService> logger)
    {
        _apiClient = apiClient;
        _repository = repository;
        _libraryService = libraryService;
        _logger = logger;
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
        _logger.LogInformation(
                    "AniList API Response for MalId {MalId}: GenresCount={GenresCount}, TagsCount={TagsCount}",
                    malId, media.Genres?.Count ?? 0, media.Tags?.Count ?? 0);
        var metadata = AnimeMetadata.FromAniListMedia(media);

        await _repository.UpsertAsync(metadata);

        return metadata;
    }

    public async Task<UserAnalyticsDto> GetUserAnalyticsAsync(int userId)
    {
        // Fetch both datasets concurrently
        var library = await _libraryService.GetUserLibraryAsync(userId);
        var metadataList = await _repository.GetMetadataForUserLibraryAsync(userId);

        if ((library == null || !library.Any()) && (metadataList == null || !metadataList.Any()))
        {
            return new UserAnalyticsDto();
        }

        var totalCompleted = library?.Count(x => string.Equals(x.Status, "Completed", StringComparison.OrdinalIgnoreCase)) ?? 0;
        var planToWatch = library?.Count(x => string.Equals(x.Status, "PlanToWatch", StringComparison.OrdinalIgnoreCase)) ?? 0;

        var totalEpisodesWatched = library?
            .Where(x => string.Equals(x.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Anime?.Episodes ?? 0) ?? 0;

        var recentlyCompletedTitle = library?
            .Where(x => string.Equals(x.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.DateUpdated)
            .Select(x => x.Anime.EnglishTitle)
            .FirstOrDefault() ?? "None";

        var demographics = metadataList?
            .Where(m => !string.IsNullOrEmpty(m.Demographic))
            .GroupBy(m => m.Demographic!)
            .ToDictionary(g => g.Key, g => g.Count()) ?? new();

        var topGenres = metadataList?
            .Where(m => m.Genres != null)
            .SelectMany(m => m.Genres!)
            .GroupBy(g => g)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count()) ?? new();

        var topThemes = metadataList?
            .Where(m => m.Themes != null)
            .SelectMany(m => m.Themes!)
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count()) ?? new();

        return new UserAnalyticsDto
        {
            TotalCompleted = totalCompleted,
            PlanToWatch = planToWatch,
            TotalEpisodesWatched = totalEpisodesWatched,
            RecentlyCompletedTitle = recentlyCompletedTitle,
            Demographics = demographics,
            TopGenres = topGenres,
            TopThemes = topThemes
        };
    }
}