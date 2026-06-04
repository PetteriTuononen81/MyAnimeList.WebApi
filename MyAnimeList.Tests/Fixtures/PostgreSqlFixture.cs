using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace MyAnimeList.Tests.Fixtures
{
    /// <summary>
    /// Shared PostgreSQL test container fixture.
    /// Creates a single container instance that is reused across all tests in the test class.
    /// </summary>
    public class PostgreSqlFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer;

        public string ConnectionString { get; private set; } = string.Empty;

        public PostgreSqlFixture()
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();
            ConnectionString = _postgresContainer.GetConnectionString();
        }

        public async Task DisposeAsync()
        {
            await _postgresContainer.DisposeAsync();
        }

        /// <summary>
        /// Cleans all tables in the database for test isolation.
        /// Call this in test setup if you need a clean database for each test.
        /// </summary>
        public async Task CleanDatabaseAsync()
        {
            await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Drop all tables (if they exist)
            var dropTables = @"
                DROP TABLE IF EXISTS animetitles CASCADE;
                DROP TABLE IF EXISTS useranime CASCADE;
                DROP TABLE IF EXISTS anime CASCADE;
                DROP TABLE IF EXISTS users CASCADE;
                DROP TABLE IF EXISTS sqlmigrations CASCADE;
            ";

            await using var command = new Npgsql.NpgsqlCommand(dropTables, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
