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

                // Check and apply pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Database migrations applied successfully.");
                }
                else
                {
                    _logger.LogInformation("Database is up to date. No migrations needed.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database initialization: {Message}", ex.Message);
                throw;
            }
        }
    }
}