using Dapper;
using Microsoft.Extensions.Configuration;
using MyAnimeList.Backend.Models;
using Npgsql;

namespace MyAnimeList.Backend.Database.Repositories
{
    public interface IAnimeRepository
    {
        Task<List<Anime>> GetAllAsync();
        Task<Anime?> GetByMalIdAsync(int malId);
        Task<bool> ExistsAsync(int malId);
        Task AddAsync(Anime anime);
        Task AddRangeAsync(IEnumerable<Anime> animes);
        Task UpdateAsync(Anime anime);
    }

    public class AnimeRepository : IAnimeRepository
    {
        private readonly string _connectionString;

        public AnimeRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not found");
        }

        public async Task<List<Anime>> GetAllAsync()
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var animes = await connection.QueryAsync<Anime>(
                @"SELECT *
                FROM anime
                ORDER BY score DESC NULLS LAST");
            return animes.ToList();
        }

        public async Task<Anime?> GetByMalIdAsync(int malId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<Anime>(
                @"SELECT *
                FROM anime
                WHERE malid = @MalId",
                new { MalId = malId });
        }

        public async Task<bool> ExistsAsync(int malId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM anime WHERE malid = @MalId",
                new { MalId = malId });
            return count > 0;
        }

        public async Task AddAsync(Anime anime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            anime.Id = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO anime 
                (malid, title, englishtitle, synopsis, episodes, status, 
                 score, imageurl, genre, airedfrom, airedto)
                VALUES 
                (@MalId, @Title, @EnglishTitle, @Synopsis, @Episodes, @Status, 
                 @Score, @ImageUrl, @Genre, @AiredFrom, @AiredTo)
                RETURNING id",
                anime);

            // Insert titles if they exist
            if (anime.Titles?.Any() == true)
            {
                await InsertTitlesAsync(connection, anime.MalId, anime.Titles);
            }
        }

        public async Task AddRangeAsync(IEnumerable<Anime> animes)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                foreach (var anime in animes)
                {
                    await AddAsync(anime);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Anime anime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.ExecuteAsync(
                @"UPDATE anime
                SET title = @Title,
                    englishtitle = @EnglishTitle,
                    synopsis = @Synopsis,
                    episodes = @Episodes,
                    status = @Status,
                    score = @Score,
                    imageurl = @ImageUrl,
                    genre = @Genre,
                    airedfrom = @AiredFrom,
                    airedto = @AiredTo
                WHERE malid = @MalId",
                anime);

            // Update titles if they exist
            if (anime.Titles?.Any() == true)
            {
                await InsertTitlesAsync(connection, anime.MalId, anime.Titles);
            }
        }

        private async Task InsertTitlesAsync(NpgsqlConnection connection, int malId, ICollection<AnimeTitle> titles)
        {
            // Delete existing titles for this anime
            var deleteSql = @"DELETE FROM animetitles WHERE malid = @MalId";
            await connection.ExecuteAsync(deleteSql, new { MalId = malId });

            // Insert new titles
            var insertSql = @"
                INSERT INTO animetitles (malid, type, title)
                VALUES (@MalId, @Type, @Title)";

            foreach (var title in titles)
            {
                await connection.ExecuteAsync(insertSql, new { MalId = malId, title.Type, title.Title });
            }
        }
    }
}