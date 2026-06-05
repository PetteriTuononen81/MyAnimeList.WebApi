using Dapper;
using Microsoft.Extensions.Configuration;
using MyAnimeList.Backend.Models;
using Npgsql;

namespace MyAnimeList.Backend.Database.Repositories
{
    public interface ILibraryRepository
    {
        Task<List<UserAnime>> GetUserLibraryAsync(int userId, AnimeWatchStatus? status = null);
        Task<UserAnime?> GetUserAnimeAsync(int userId, int malId);
        Task<UserAnime> AddToLibraryAsync(UserAnime userAnime);
        Task<UserAnime> UpdateLibraryItemAsync(UserAnime userAnime);
        Task<bool> RemoveFromLibraryAsync(int userId, int malId);
        Task<bool> IsAnimeInLibraryAsync(int userId, int malId);
    }

    public class LibraryRepository : ILibraryRepository
    {
        private readonly string _connectionString;

        public LibraryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not found");
        }

        public async Task<List<UserAnime>> GetUserLibraryAsync
            (int userId, AnimeWatchStatus? status = null)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var result = await connection.QueryAsync<UserAnime>(
                "SELECT * FROM useranime WHERE userid = @UserId" +
                (status.HasValue ? " AND status = @Status" : "") +
                " ORDER BY dateupdated DESC",
                new { UserId = userId, Status = status });

            return result.ToList();
        }

        public async Task<UserAnime?> GetUserAnimeAsync(int userId, int malId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<UserAnime>(
                "SELECT * FROM useranime WHERE userid = @UserId AND malid = @MalId",
                new { UserId = userId, MalId = malId });
        }

        public async Task<UserAnime> AddToLibraryAsync(UserAnime userAnime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            userAnime.Id = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO useranime 
                (userid, malid, status, userscore, notes, dateadded, dateupdated)
                VALUES 
                (@UserId, @MalId, @Status, @UserScore, @Notes, @DateAdded, @DateUpdated)
                RETURNING id",
                userAnime);

            // Load the anime and titles for response
            return (await GetUserAnimeAsync(userAnime.UserId, userAnime.MalId))!;
        }

        public async Task<UserAnime> UpdateLibraryItemAsync(UserAnime userAnime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            userAnime.DateUpdated = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"UPDATE useranime
                SET status = @Status, 
                    userscore = @UserScore, 
                    notes = @Notes,
                    dateupdated = @DateUpdated
                WHERE userid = @UserId AND malid = @MalId",
                userAnime);

            // Load the updated anime and titles for response
            return (await GetUserAnimeAsync(userAnime.UserId, userAnime.MalId))!;
        }

        public async Task<bool> RemoveFromLibraryAsync(int userId, int malId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var rowsAffected = await connection.ExecuteAsync(
                @"DELETE FROM useranime
                WHERE userid = @UserId AND malid = @MalId",
                new { UserId = userId, MalId = malId });
            return rowsAffected > 0;
        }

        public async Task<bool> IsAnimeInLibraryAsync(int userId, int malId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var count = await connection.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1) 
                FROM useranime 
                WHERE userid = @UserId AND malid = @MalId",
                new { UserId = userId, MalId = malId });
            return count > 0;
        }
    }
}
