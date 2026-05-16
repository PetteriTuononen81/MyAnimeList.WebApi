using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Repositories;
using Microsoft.Extensions.Logging;

namespace MyAnimeList.Backend.Services
{
    public interface IAnimeService
    {
        Task<List<Anime>> GetAllAnimeAsync();
        Task<(List<Anime> Animes, int TotalCount)> GetAnimePaginatedAsync(int page, int pageSize);
        Task<(List<Anime> Animes, int TotalCount)> SearchAnimeAsync(string searchTerm, int page, int pageSize);
        Task<int> SyncAnimeDataAsync(int? maxPages = null);
    }

    public class AnimeService : IAnimeService
    {
        private readonly IAnimeRepository _animeRepository;
        private readonly JikanApiClient _jikanApiClient;
        private readonly ILogger<AnimeService> _logger;

        // Jikan API rate limit: 3 requests per second, so 4 seconds is safe
        private const int DelayBetweenRequestsMs = 4000;
        private const int ItemsPerPage = 25;

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

        public async Task<(List<Anime> Animes, int TotalCount)> GetAnimePaginatedAsync(int page, int pageSize)
        {
            _logger.LogInformation("Getting paginated anime - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            var allAnime = await _animeRepository.GetAllAsync();
            var totalCount = allAnime.Count;

            var paginatedAnime = allAnime
                .OrderByDescending(a => a.Score)
                .ThenBy(a => a.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (paginatedAnime, totalCount);
        }

        public async Task<(List<Anime> Animes, int TotalCount)> SearchAnimeAsync(string searchTerm, int page, int pageSize)
        {
            _logger.LogInformation("Searching anime with term: '{SearchTerm}' - Page: {Page}, PageSize: {PageSize}", 
                searchTerm, page, pageSize);

            var allAnime = await _animeRepository.GetAllAsync();

            // Filter by search term (case-insensitive search in title, english title, and genre)
            var filteredAnime = allAnime
                .Where(a =>
                    (a.Title != null && a.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (a.EnglishTitle != null && a.EnglishTitle.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (a.Genre != null && a.Genre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                )
                .ToList();

            var totalCount = filteredAnime.Count;

            var paginatedAnime = filteredAnime
                .OrderByDescending(a => a.Score)
                .ThenBy(a => a.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (paginatedAnime, totalCount);
        }

        public async Task<int> SyncAnimeDataAsync(int? maxPages = null)
        {
            try
            {
                _logger.LogInformation("Starting anime data sync from Jikan API...");

                var allExistingAnime = await _animeRepository.GetAllAsync();
                int totalInsertCount = 0;
                int totalUpdateCount = 0;
                int currentPage = 1;
                int lastPage = 1;

                // Limit pages if maxPages is specified
                int pagesToFetch = maxPages ?? int.MaxValue;

                do
                {
                    _logger.LogInformation("Fetching page {CurrentPage} of {LastPage}...", currentPage, lastPage);

                    // Fetch current page
                    var response = await _jikanApiClient.FetchAnimePageAsync(page: currentPage, limit: ItemsPerPage);

                    if (response.Data.Count == 0)
                    {
                        _logger.LogWarning("No anime data fetched from page {CurrentPage}", currentPage);
                        break;
                    }

                    // Update last page from API response
                    lastPage = response.LastPage;

                    // Limit to maxPages if specified
                    if (maxPages.HasValue && lastPage > maxPages.Value)
                    {
                        lastPage = maxPages.Value;
                    }

                    _logger.LogInformation("Fetched {AnimeCount} anime from page {CurrentPage}/{LastPage}. Processing...", 
                        response.Data.Count, currentPage, lastPage);

                    // Process each anime
                    foreach (var newAnime in response.Data)
                    {
                        var existingAnime = allExistingAnime.FirstOrDefault(a => a.MalId == newAnime.MalId);

                        if (existingAnime != null)
                        {
                            existingAnime.Update(newAnime);
                            await _animeRepository.UpdateAsync(existingAnime);
                            totalUpdateCount++;
                        }
                        else
                        {
                            await _animeRepository.AddAsync(newAnime);
                            allExistingAnime.Add(newAnime);
                            totalInsertCount++;
                        }
                    }

                    _logger.LogInformation("Page {CurrentPage} processed. Running total: {InsertCount} inserted, {UpdateCount} updated", 
                        currentPage, totalInsertCount, totalUpdateCount);

                    // Move to next page
                    currentPage++;

                    // Wait 4 seconds before next API call (Jikan rate limit)
                    if (response.HasNextPage && currentPage <= lastPage)
                    {
                        _logger.LogInformation("Waiting {Delay}ms before next request (rate limit)...", DelayBetweenRequestsMs);
                        await Task.Delay(DelayBetweenRequestsMs);
                    }

                } while (currentPage <= lastPage);

                _logger.LogInformation("Sync completed! Total: {InsertCount} inserted, {UpdateCount} updated across {PageCount} pages", 
                    totalInsertCount, totalUpdateCount, currentPage - 1);

                return totalInsertCount + totalUpdateCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during anime data sync: {ErrorMessage}", ex.Message);
                throw;
            }
        }
    }
}
