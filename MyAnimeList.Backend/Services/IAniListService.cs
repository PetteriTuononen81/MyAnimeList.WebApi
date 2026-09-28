using YourProjectNamespace.Clients;
using YourProjectNamespace.Entities;
using YourProjectNamespace.Repositories;

namespace YourProjectNamespace.Services;

public interface IAnimeMetadataService
{
    Task<AnimeMetadata?> GetOrFetchMetadataAsync(int malId);
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
}