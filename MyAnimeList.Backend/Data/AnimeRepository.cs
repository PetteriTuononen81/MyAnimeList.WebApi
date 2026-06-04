using Dapper;
using Microsoft.Extensions.Configuration;
using MyAnimeList.Backend.Models;
using Npgsql;

namespace MyAnimeList.Backend.Repositories
{
    public interface IAnimeRepository
    {
        Task<List<Anime>> GetAllAsync();
        Task<Anime?> GetByMalIdAsync(int malId);
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

            var sql = @"
                SELECT *
                FROM anime
                ORDER BY score DESC NULLS LAST";

            var animes = await connection.QueryAsync<Anime>(sql);
            return animes.ToList();
        }

        public async Task<Anime?> GetByMalIdAsync(int malId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT *
                FROM anime
                WHERE malid = @MalId";

            return await connection.QueryFirstOrDefaultAsync<Anime>(sql, new { MalId = malId });
        }

        public async Task AddAsync(Anime anime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                INSERT INTO anime 
                (malid, title, englishtitle, japanesetitle, imageurl, 
                 synopsis, type, episodes, status, score, 
                 popularity, rank, startdate, enddate)
                VALUES 
                (@MalId, @Title, @EnglishTitle, @JapaneseTitle, @ImageUrl, 
                 @Synopsis, @Type, @Episodes, @Status, @Score, 
                 @Popularity, @Rank, @StartDate, @EndDate)
                ON CONFLICT (malid) DO UPDATE SET
                    title = EXCLUDED.title,
                    englishtitle = EXCLUDED.englishtitle,
                    japanesetitle = EXCLUDED.japanesetitle,
                    imageurl = EXCLUDED.imageurl,
                    synopsis = EXCLUDED.synopsis,
                    type = EXCLUDED.type,
                    episodes = EXCLUDED.episodes,
                    status = EXCLUDED.status,
                    score = EXCLUDED.score,
                    popularity = EXCLUDED.popularity,
                    rank = EXCLUDED.rank,
                    startdate = EXCLUDED.startdate,
                    enddate = EXCLUDED.enddate
                RETURNING id";

            anime.Id = await connection.ExecuteScalarAsync<int>(sql, anime);

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

            var sql = @"
                UPDATE anime
                SET title = @Title,
                    englishtitle = @EnglishTitle,
                    japanesetitle = @JapaneseTitle,
                    imageurl = @ImageUrl,
                    synopsis = @Synopsis,
                    type = @Type,
                    episodes = @Episodes,
                    status = @Status,
                    score = @Score,
                    popularity = @Popularity,
                    rank = @Rank,
                    startdate = @StartDate,
                    enddate = @EndDate
                WHERE malid = @MalId";

            await connection.ExecuteAsync(sql, anime);
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