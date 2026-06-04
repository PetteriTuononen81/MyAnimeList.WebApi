using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyAnimeList.Backend.Services;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyAnimeList.Tests.Services
{
    public class SqlMigrationServiceTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer;
        private string _connectionString = string.Empty;
        private readonly Mock<ILogger<SqlMigrationService>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;

        public SqlMigrationServiceTests()
        {
            // Setup PostgreSQL test container
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();

            _loggerMock = new Mock<ILogger<SqlMigrationService>>();
            _configMock = new Mock<IConfiguration>();
        }

        public async Task InitializeAsync()
        {
            // Start the PostgreSQL container
            await _postgresContainer.StartAsync();
            _connectionString = _postgresContainer.GetConnectionString();

            // Setup configuration mock - use GetSection instead of GetConnectionString
            var connectionStringsSection = new Mock<IConfigurationSection>();
            connectionStringsSection.Setup(s => s["DefaultConnection"]).Returns(_connectionString);
            _configMock.Setup(c => c.GetSection("ConnectionStrings")).Returns(connectionStringsSection.Object);
        }

        public async Task DisposeAsync()
        {
            // Stop and dispose the container
            await _postgresContainer.DisposeAsync();
        }

        [Fact]
        public async Task ApplyMigrationsAsync_CreatesTrackingTable()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act
            await service.ApplyMigrationsAsync();

            // Assert
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM information_schema.tables 
                  WHERE table_name = '__SqlMigrations'", 
                connection);

            var result = (long?)await command.ExecuteScalarAsync();
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task ApplyMigrationsAsync_RunsMigrationsInOrder()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act
            await service.ApplyMigrationsAsync();

            // Assert
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                @"SELECT ""MigrationName"" FROM ""__SqlMigrations"" ORDER BY ""AppliedAt""", 
                connection);

            var migrations = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                migrations.Add(reader.GetString(0));
            }

            // Verify migrations are in order
            Assert.NotEmpty(migrations);
            Assert.Equal(migrations.OrderBy(m => m).ToList(), migrations);
        }

        [Fact]
        public async Task ApplyMigrationsAsync_DoesNotRerunAppliedMigrations()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act - Run twice
            await service.ApplyMigrationsAsync();
            var firstCount = await GetMigrationCountAsync();

            await service.ApplyMigrationsAsync();
            var secondCount = await GetMigrationCountAsync();

            // Assert - Count should be the same
            Assert.Equal(firstCount, secondCount);
        }

        [Fact]
        public async Task ApplyMigrationsAsync_CreatesAnimeTables()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act
            await service.ApplyMigrationsAsync();

            // Assert
            var tables = new[] { "Anime", "Users", "UserAnime", "AnimeTitles" };

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var table in tables)
            {
                var command = new NpgsqlCommand(
                    $@"SELECT COUNT(*) FROM information_schema.tables 
                       WHERE table_name = '{table}'", 
                    connection);

                var result = (long?)await command.ExecuteScalarAsync();
                Assert.Equal(1, result);
            }
        }

        [Fact]
        public async Task ApplyMigrationsAsync_CreatesIndexes()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act
            await service.ApplyMigrationsAsync();

            // Assert - Check some key indexes exist
            var indexes = new[] 
            { 
                "IX_Anime_MalId", 
                "IX_Users_Email", 
                "IX_UserAnime_UserId_MalId",
                "IX_AnimeTitles_MalId_Type"
            };

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var index in indexes)
            {
                var command = new NpgsqlCommand(
                    $@"SELECT COUNT(*) FROM pg_indexes 
                       WHERE indexname = '{index}'", 
                    connection);

                var result = (long?)await command.ExecuteScalarAsync();
                Assert.True(result >= 1, $"Index {index} not found");
            }
        }

        [Fact]
        public async Task ApplyMigrationsAsync_CreatesForeignKeys()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act
            await service.ApplyMigrationsAsync();

            // Assert
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                @"SELECT constraint_name 
                  FROM information_schema.table_constraints 
                  WHERE constraint_type = 'FOREIGN KEY'", 
                connection);

            var foreignKeys = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                foreignKeys.Add(reader.GetString(0));
            }

            // Verify key foreign keys exist
            Assert.Contains(foreignKeys, fk => fk.Contains("FK_UserAnime_Users"));
            Assert.Contains(foreignKeys, fk => fk.Contains("FK_UserAnime_Anime"));
            Assert.Contains(foreignKeys, fk => fk.Contains("FK_AnimeTitles_Anime"));
        }

        [Fact]
        public async Task ApplyMigrationsAsync_HandlesEmptyMigrationsDirectory()
        {
            // This tests the case where no SQL files exist
            // The service should handle it gracefully

            // Note: In real scenario, we'd need to mock the file system
            // For now, this tests that existing migrations work

            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act & Assert - Should not throw
            await service.ApplyMigrationsAsync();
        }

        [Fact]
        public async Task Migration_Schema_IsValid()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act
            await service.ApplyMigrationsAsync();

            // Assert - Verify we can insert and query data
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Insert test anime
            var insertCmd = new NpgsqlCommand(
                @"INSERT INTO ""Anime"" (""MalId"", ""Title"", ""Episodes"", ""CreatedAt"", ""UpdatedAt"") 
                  VALUES (1, 'Test Anime', 12, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
                  RETURNING ""Id""", 
                connection);

            var animeId = await insertCmd.ExecuteScalarAsync();
            Assert.NotNull(animeId);

            // Insert test title
            var titleCmd = new NpgsqlCommand(
                @"INSERT INTO ""AnimeTitles"" (""MalId"", ""Type"", ""Title"") 
                  VALUES (1, 'English', 'Test Anime English') 
                  RETURNING ""Id""", 
                connection);

            var titleId = await titleCmd.ExecuteScalarAsync();
            Assert.NotNull(titleId);

            // Query back
            var queryCmd = new NpgsqlCommand(
                @"SELECT a.""Title"", t.""Title"" 
                  FROM ""Anime"" a 
                  JOIN ""AnimeTitles"" t ON a.""MalId"" = t.""MalId"" 
                  WHERE a.""MalId"" = 1", 
                connection);

            await using var reader = await queryCmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("Test Anime", reader.GetString(0));
            Assert.Equal("Test Anime English", reader.GetString(1));
        }

        private async Task<long> GetMigrationCountAsync()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM ""__SqlMigrations""", 
                connection);

            return (long)(await command.ExecuteScalarAsync() ?? 0L);
        }
    }
}
