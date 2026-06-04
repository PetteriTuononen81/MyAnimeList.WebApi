using System.Text.RegularExpressions;
using Xunit;

namespace MyAnimeList.Tests.Migrations
{
    public class MigrationFileTests
    {
        private readonly string _migrationsPath;

        public MigrationFileTests()
        {
            // Get path to migrations folder
            var projectRoot = Directory.GetCurrentDirectory();
            while (!Directory.Exists(Path.Combine(projectRoot, "MyAnimeList.Backend")))
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName 
                    ?? throw new InvalidOperationException("Could not find project root");
            }

            _migrationsPath = Path.Combine(projectRoot, "MyAnimeList.Backend", "Database", "Migrations");
        }

        [Fact]
        public void MigrationsDirectory_Exists()
        {
            Assert.True(Directory.Exists(_migrationsPath), 
                $"Migrations directory not found at {_migrationsPath}");
        }

        [Fact]
        public void MigrationFiles_ExistAndFollowNamingConvention()
        {
            var sqlFiles = Directory.GetFiles(_migrationsPath, "*.sql");

            Assert.NotEmpty(sqlFiles);

            foreach (var file in sqlFiles)
            {
                var fileName = Path.GetFileName(file);

                // Should match pattern: XXX_DescriptiveName.sql
                var regex = new Regex(@"^\d{3}_[A-Za-z]+\.sql$");
                Assert.True(regex.IsMatch(fileName), 
                    $"File '{fileName}' doesn't follow naming convention: XXX_DescriptiveName.sql");
            }
        }

        [Fact]
        public void MigrationFiles_AreSequentiallyNumbered()
        {
            var sqlFiles = Directory.GetFiles(_migrationsPath, "*.sql")
                .Select(f => Path.GetFileName(f))
                .OrderBy(f => f)
                .ToList();

            Assert.NotEmpty(sqlFiles);

            // Extract numbers and verify they're sequential
            var numbers = sqlFiles
                .Select(f => int.Parse(f.Substring(0, 3)))
                .ToList();

            for (int i = 0; i < numbers.Count; i++)
            {
                Assert.Equal(i + 1, numbers[i]);
            }
        }

        [Fact]
        public void MigrationFiles_ContainRequiredHeaders()
        {
            var sqlFiles = Directory.GetFiles(_migrationsPath, "*.sql");

            Assert.NotEmpty(sqlFiles);

            foreach (var file in sqlFiles)
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);

                // Check for required comment headers
                Assert.True(content.Contains("-- Migration:"), 
                    $"File '{fileName}' missing '-- Migration:' header");
                Assert.True(content.Contains("-- Description:"), 
                    $"File '{fileName}' missing '-- Description:' header");
                Assert.True(content.Contains("-- Date:"), 
                    $"File '{fileName}' missing '-- Date:' header");
            }
        }

        [Fact]
        public void MigrationFiles_UseSafeIdempotentSQL()
        {
            var sqlFiles = Directory.GetFiles(_migrationsPath, "*.sql");

            Assert.NotEmpty(sqlFiles);

            foreach (var file in sqlFiles)
            {
                var content = File.ReadAllText(file).ToUpper();
                var fileName = Path.GetFileName(file);

                // Should use IF NOT EXISTS for CREATE statements
                if (content.Contains("CREATE TABLE"))
                {
                    var createTablePattern = @"CREATE TABLE (?!IF NOT EXISTS)""?\w+""?";
                    var unsafeCreates = Regex.Matches(content, createTablePattern);

                    Assert.True(unsafeCreates.Count == 0 || content.Contains("IF NOT EXISTS"), 
                        $"File '{fileName}' has CREATE TABLE without IF NOT EXISTS");
                }

                // Should use IF EXISTS for DROP statements (if any)
                if (content.Contains("DROP TABLE") && !content.Contains("DROP TABLE IF EXISTS"))
                {
                    Assert.Fail($"File '{fileName}' has DROP TABLE without IF EXISTS");
                }
            }
        }

        [Fact]
        public void MigrationFiles_HaveValidSQL()
        {
            var sqlFiles = Directory.GetFiles(_migrationsPath, "*.sql");

            Assert.NotEmpty(sqlFiles);

            foreach (var file in sqlFiles)
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);

                // Basic SQL syntax checks - should have statements ending with semicolons
                var semicolonCount = content.Count(c => c == ';');
                Assert.True(semicolonCount > 0, 
                    $"File '{fileName}' should contain at least one statement ending with ;");

                // Should not be empty
                Assert.True(content.Trim().Length > 0, 
                    $"File '{fileName}' should not be empty");
            }
        }

        [Fact]
        public void InitialMigration_CreatesAllRequiredTables()
        {
            var initialMigration = Path.Combine(_migrationsPath, "001_InitialSchema.sql");

            Assert.True(File.Exists(initialMigration), "001_InitialSchema.sql not found");

            var content = File.ReadAllText(initialMigration).ToLower();

            // Required tables (lowercase - PostgreSQL standard)
            var requiredTables = new[] { "anime", "users", "useranime", "animetitles" };

            foreach (var table in requiredTables)
            {
                Assert.True(content.Contains("create table"), 
                    $"File should contain CREATE TABLE statement");
                Assert.True(content.Contains($"{table}"), 
                    $"File should contain table {table}");
            }
        }

        [Fact]
        public void InitialMigration_CreatesIndexes()
        {
            var initialMigration = Path.Combine(_migrationsPath, "001_InitialSchema.sql");

            Assert.True(File.Exists(initialMigration));

            var content = File.ReadAllText(initialMigration).ToLower();

            // Should create indexes (lowercase ix_ prefix for PostgreSQL)
            Assert.True(content.Contains("create index"), 
                "File should contain CREATE INDEX statements");
            Assert.True(content.Contains("ix_"), 
                "Indexes should follow ix_ naming convention (lowercase)");
        }

        [Fact]
        public void InitialMigration_CreatesForeignKeys()
        {
            var initialMigration = Path.Combine(_migrationsPath, "001_InitialSchema.sql");

            Assert.True(File.Exists(initialMigration));

            var content = File.ReadAllText(initialMigration).ToLower();

            // Should create foreign keys
            Assert.True(content.Contains("foreign key"), 
                "File should contain FOREIGN KEY constraints");
            Assert.True(content.Contains("references"), 
                "File should contain REFERENCES clauses");
        }

        [Fact]
        public void README_ExistsInMigrationsFolder()
        {
            var readmePath = Path.Combine(_migrationsPath, "README.md");
            Assert.True(File.Exists(readmePath), "README.md not found in migrations folder");
        }
    }
}
