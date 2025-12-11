using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Repositories;
using Microsoft.Extensions.Logging;

namespace MyAnimeList.Backend.Services
{
    public interface IAnimeService
    {
        Task<List<Anime>> GetAllAnimeAsync();
        Task<int> SyncAnimeDataAsync();
    }

    public class AnimeService : IAnimeService
    {
        private readonly IAnimeRepository _animeRepository;
        private readonly JikanApiClient _jikanApiClient;
        private readonly ILogger<AnimeService> _logger;

        public AnimeService(IAnimeRepository animeRepository, JikanApiClient jikanApiClient, ILogger<AnimeService> logger)
        {
            _animeRepository = animeRepository;
            _jikanApiClient = jikanApiClient;
            _logger = logger;
        }

        public async Task<List<Anime>> GetAllAnimeAsync()
        {
            _logger.LogInformation("Getting all anime");
            return await _animeRepository.GetAllAsync();
        }

        public async Task<int> SyncAnimeDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting anime data sync from Jikan API...");

                var animeList = await _jikanApiClient.FetchAnimeListAsync(page: 1, limit: 25);

                if (animeList.Count == 0)
                {
                    _logger.LogWarning("No anime data fetched from Jikan API");
                    return 0;
                }

                _logger.LogInformation("Fetched {AnimeCount} anime from Jikan API. Processing updates...", animeList.Count);

                var allExistingAnime = await _animeRepository.GetAllAsync();
                int insertCount = 0;
                int updateCount = 0;

                foreach (var newAnime in animeList)
                {
                    var existingAnime = allExistingAnime.FirstOrDefault(a => a.MalId == newAnime.MalId);

                    if (existingAnime != null)
                    {
                        // Update existing anime
                        _logger.LogInformation("Updating anime with MalId: {MalId}", newAnime.MalId);

                        existingAnime.Update(newAnime);

                        await _animeRepository.UpdateAsync(existingAnime);
                        updateCount++;
                    }
                    else
                    {
                        // Insert new anime
                        _logger.LogInformation("Inserting new anime with MalId: {MalId}", newAnime.MalId);
                        await _animeRepository.AddAsync(newAnime);
                        insertCount++;
                    }
                }

                _logger.LogInformation("Sync completed: {InsertCount} inserted, {UpdateCount} updated", insertCount, updateCount);
                return insertCount + updateCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during anime data sync: {ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}
