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

        public SearchService(IAnimeRepository animeRepository)
        {
            _animeRepository = animeRepository;
        }

        public async Task<List<BulkImportCandidateResponse>> EnrichWithDatabaseDataAsync(List<AnimeImportDto> parsedAnimes)
        {
            var tasks = parsedAnimes.Select(async item =>
            {
                var matchedAnime = await _animeRepository.SearchByTitleAsync(item.Title);
                return item.(matchedAnime);
            });

            var results = await Task.WhenAll(tasks);

            return results.ToList();
        }
    }
}
