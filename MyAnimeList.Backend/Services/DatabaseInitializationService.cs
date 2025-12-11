using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;

namespace MyAnimeList.Backend.Services
{
    public class DatabaseInitializationService
    {
        private readonly AnimeDbContext _context;
        private readonly JikanApiClient _jikanService;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(AnimeDbContext context, JikanApiClient jikanService, ILogger<DatabaseInitializationService> logger)
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

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database initialization: {Message}", ex.Message);
                throw;
            }
        }
    }
}