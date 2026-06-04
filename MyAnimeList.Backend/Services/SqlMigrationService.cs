using Npgsql;
using System.Reflection;

namespace MyAnimeList.Backend.Services
{
    public interface ISqlMigrationService
    {
        Task ApplyMigrationsAsync();
    }

    public class SqlMigrationService : ISqlMigrationService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlMigrationService> _logger;

        public SqlMigrationService(IConfiguration configuration, ILogger<SqlMigrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task ApplyMigrationsAsync()
        {
            _logger.LogInformation("Starting SQL migrations...");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Ensure migration tracking table exists
                await EnsureMigrationTableExistsAsync(connection);

                // Get all migration files
                var migrationFiles = GetMigrationFiles();

                _logger.LogInformation("Found {Count} migration files", migrationFiles.Count);

                foreach (var migrationFile in migrationFiles)
                {
                    var migrationName = Path.GetFileNameWithoutExtension(migrationFile);

                    // Check if migration already applied
                    if (await IsMigrationAppliedAsync(connection, migrationName))
                    {
                        _logger.LogInformation("Migration {Migration} already applied, skipping", migrationName);
                        continue;
                    }

                    _logger.LogInformation("Applying migration: {Migration}", migrationName);

                    // Read and execute SQL file
                    var sql = await File.ReadAllTextAsync(migrationFile);

                    await using var transaction = await connection.BeginTransactionAsync();
                    try
                    {
                        await using var command = new NpgsqlCommand(sql, connection, transaction);
                        await command.ExecuteNonQueryAsync();

                        // Record migration as applied
                        await RecordMigrationAsync(connection, transaction, migrationName);

                        await transaction.CommitAsync();
                        _logger.LogInformation("Migration {Migration} applied successfully", migrationName);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Failed to apply migration {Migration}", migrationName);
                        throw;
                    }
                }

                _logger.LogInformation("All SQL migrations completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL migration");
                throw;
            }
        }

        private async Task EnsureMigrationTableExistsAsync(NpgsqlConnection connection)
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS sqlmigrations (
                    id SERIAL PRIMARY KEY,
                    migrationname TEXT NOT NULL UNIQUE,
                    appliedat TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                );";

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<bool> IsMigrationAppliedAsync(NpgsqlConnection connection, string migrationName)
        {
            var sql = "SELECT COUNT(*) FROM sqlmigrations WHERE migrationname = @MigrationName";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("MigrationName", migrationName);

            var count = (long?)await command.ExecuteScalarAsync();
            return count > 0;
        }

        private async Task RecordMigrationAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string migrationName)
        {
            var sql = "INSERT INTO sqlmigrations (migrationname, appliedat) VALUES (@MigrationName, @AppliedAt)";

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("MigrationName", migrationName);
            command.Parameters.AddWithValue("AppliedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }

        private List<string> GetMigrationFiles()
        {
            var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Database", "Migrations");

            if (!Directory.Exists(migrationsPath))
            {
                _logger.LogWarning("Migrations directory not found at {Path}", migrationsPath);
                return new List<string>();
            }

            return Directory.GetFiles(migrationsPath, "*.sql")
                .OrderBy(f => f)
                .ToList();
        }
    }
}
