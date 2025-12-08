using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;

namespace MyAnimeList.Backend.Services
{
    public class DatabaseInitializationService
    {
        private readonly AnimeDbContext _context;
        private readonly JikanApiService _jikanService;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(AnimeDbContext context, JikanApiService jikanService, ILogger<DatabaseInitializationService> logger)
        {
            _context = context;
            _jikanService = jikanService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // Create database if it doesn't exist
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("Database created or already exists.");

                // Check if anime data already exists
                if (await _context.Anime.AnyAsync())
                {
                    _logger.LogInformation("Database already populated with anime data.");
                    return;
                }

                // Fetch and import anime data
                _logger.LogInformation("Fetching anime data from Jikan API...");
                var animeList = await _jikanService.FetchAnimeListAsync(page: 1, limit: 100);

                if (animeList.Count == 0)
                {
                    _logger.LogWarning("No anime data fetched from API.");
                    return;
                }

                _logger.LogInformation($"Fetched {animeList.Count} anime entries. Saving to database...");
                _context.Anime.AddRange(animeList);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully saved {animeList.Count} anime entries to database.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database initialization: {Message}", ex.Message);
                throw;
            }
        }
    }
}