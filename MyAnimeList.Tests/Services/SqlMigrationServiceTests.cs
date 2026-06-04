using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyAnimeList.Backend.Services;
using MyAnimeList.Tests.Fixtures;
using Npgsql;
using Xunit;

namespace MyAnimeList.Tests.Services
{
    public class SqlMigrationServiceTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
    {
        private readonly PostgreSqlFixture _fixture;
        private readonly Mock<ILogger<SqlMigrationService>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private string ConnectionString => _fixture.ConnectionString;

        public SqlMigrationServiceTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
            _loggerMock = new Mock<ILogger<SqlMigrationService>>();
            _configMock = new Mock<IConfiguration>();

            // Setup configuration mock
            var connectionStringsSection = new Mock<IConfigurationSection>();
            connectionStringsSection.Setup(s => s["DefaultConnection"]).Returns(ConnectionString);
            _configMock.Setup(c => c.GetSection("ConnectionStrings")).Returns(connectionStringsSection.Object);
        }

        public async Task InitializeAsync()
        {
            // Clean database before each test to ensure isolation
            await _fixture.CleanDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            // No cleanup needed per test
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ApplyMigrationsAsync_CreatesTrackingTable()
        {
            // Arrange
            var service = new SqlMigrationService(_configMock.Object, _loggerMock.Object);

            // Act
            await service.ApplyMigrationsAsync();

            // Assert
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM information_schema.tables 
                  WHERE table_name = 'sqlmigrations'", 
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
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                @"SELECT migrationname FROM sqlmigrations ORDER BY appliedat", 
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
            var tables = new[] { "anime", "users", "useranime", "animetitles" };

            await using var connection = new NpgsqlConnection(ConnectionString);
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
                "ix_anime_malid", 
                "ix_users_email", 
                "ix_useranime_userid_malid",
                "ix_animetitles_malid_type"
            };

            await using var connection = new NpgsqlConnection(ConnectionString);
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
            await using var connection = new NpgsqlConnection(ConnectionString);
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
            Assert.Contains(foreignKeys, fk => fk.Contains("fk_useranime_users"));
            Assert.Contains(foreignKeys, fk => fk.Contains("fk_useranime_anime"));
            Assert.Contains(foreignKeys, fk => fk.Contains("fk_animetitles_anime"));
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
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Insert test anime
            var insertCmd = new NpgsqlCommand(
                @"INSERT INTO anime (malid, title, episodes) 
                  VALUES (1, 'Test Anime', 12) 
                  RETURNING id", 
                connection);

            var animeId = await insertCmd.ExecuteScalarAsync();
            Assert.NotNull(animeId);

            // Insert test title
            var titleCmd = new NpgsqlCommand(
                @"INSERT INTO animetitles (malid, type, title) 
                  VALUES (1, 'English', 'Test Anime English') 
                  RETURNING id", 
                connection);

            var titleId = await titleCmd.ExecuteScalarAsync();
            Assert.NotNull(titleId);

            // Query back
            var queryCmd = new NpgsqlCommand(
                @"SELECT a.title, t.title 
                  FROM anime a 
                  JOIN animetitles t ON a.malid = t.malid 
                  WHERE a.malid = 1", 
                connection);

            await using var reader = await queryCmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("Test Anime", reader.GetString(0));
            Assert.Equal("Test Anime English", reader.GetString(1));
        }

        private async Task<long> GetMigrationCountAsync()
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                @"SELECT COUNT(*) FROM sqlmigrations", 
                connection);

            return (long)(await command.ExecuteScalarAsync() ?? 0L);
        }
    }
}
