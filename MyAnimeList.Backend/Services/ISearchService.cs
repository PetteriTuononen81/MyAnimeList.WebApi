using MyAnimeList.Backend.Database.Repositories;
using MyAnimeList.Backend.Mappers;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Models.Response;

namespace MyAnimeList.Backend.Services
{
    public interface ISearchService
    {
        Task<List<BulkImportCandidateResponse>> EnrichWithDatabaseDataAsync(List<AnimeImportDto> parsedAnimes);
    }

    public class SearchService : ISearchService
    {
        private readonly IAnimeRepository _animeRepository;
        private readonly ILogger<SearchService> _logger;

        public SearchService(IAnimeRepository animeRepository, ILogger<SearchService> logger)
        {
            _animeRepository = animeRepository;
            _logger = logger;
        }

        public async Task<List<BulkImportCandidateResponse>> EnrichWithDatabaseDataAsync(List<AnimeImportDto> parsedAnimes)
        {
            var tasks = parsedAnimes.Select(async item =>
            {
                var matchedAnime = await _animeRepository.SearchByTitleAsync(item.Title);

                if (matchedAnime != null)
                {
                    _logger.LogInformation("Match found for '{Title}' -> ID: {AnimeId}", item.Title, matchedAnime.Id);
                }
                else
                {
                    _logger.LogWarning("No match found in database for '{Title}'", item.Title);
                }

                return item.ToCandidateResponse(matchedAnime);
            });

            var results = await Task.WhenAll(tasks);
            _logger.LogInformation("Enrichment complete. Total candidate responses generated: {Count}", results.Length);
            return results.ToList();
        }
    }
}
