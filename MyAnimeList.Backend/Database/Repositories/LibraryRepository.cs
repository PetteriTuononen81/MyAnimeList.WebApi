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

        public async Task<List<UserAnime>> GetUserLibraryAsync(int userId, AnimeWatchStatus? status = null)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var userAnimeDict = new Dictionary<int, UserAnime>();

            var result = await connection.QueryAsync<UserAnime, Anime, AnimeTitle, UserAnime>(
                @"SELECT 
                    ua.*,
                    a.id AS animeid, a.malid AS animemalid, a.title, a.englishtitle, 
                    a.japanesetitle, a.imageurl, a.synopsis, a.type, 
                    a.episodes, a.status AS animestatus, a.score AS animescore, 
                    a.popularity, a.rank, a.startdate AS animestartdate, 
                    a.enddate AS animeenddate,
                    t.id AS titleid, t.malid AS titlemalid, t.type AS titletype, 
                    t.title AS titletext
                FROM useranime ua
                INNER JOIN anime a ON ua.malid = a.malid
                LEFT JOIN animetitles t ON a.malid = t.malid
                WHERE ua.userid = @UserId" +
                (status.HasValue ? " AND ua.status = @Status" : "") +
                " ORDER BY ua.dateupdated DESC",
                (userAnime, anime, title) =>
                {
                    if (!userAnimeDict.TryGetValue(userAnime.Id, out var userAnimeEntry))
                    {
                        userAnimeEntry = userAnime;
                        userAnimeEntry.Anime = anime;
                        anime.Titles = new List<AnimeTitle>();
                        userAnimeDict.Add(userAnime.Id, userAnimeEntry);
                    }

                    if (title != null)
                    {
                        userAnimeEntry.Anime!.Titles.Add(title);
                    }

                    return userAnimeEntry;
                },
                new { UserId = userId, Status = status },
                splitOn: "animeid,titleid"
            );

            return userAnimeDict.Values.ToList();
        }

        public async Task<UserAnime?> GetUserAnimeAsync(int userId, int malId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            UserAnime? userAnime = null;

            await connection.QueryAsync<UserAnime, Anime, AnimeTitle, UserAnime>(
                @"SELECT 
                    ua.*,
                    a.id AS animeid, a.malid AS animemalid, a.title, a.englishtitle, 
                    a.japanesetitle, a.imageurl, a.synopsis, a.type, 
                    a.episodes, a.status AS animestatus, a.score AS animescore, 
                    a.popularity, a.rank, a.startdate AS animestartdate, 
                    a.enddate AS animeenddate,
                    t.id AS titleid, t.malid AS titlemalid, t.type AS titletype, 
                    t.title AS titletext
                FROM useranime ua
                INNER JOIN anime a ON ua.malid = a.malid
                LEFT JOIN animetitles t ON a.malid = t.malid
                WHERE ua.userid = @UserId AND ua.malid = @MalId",
                (ua, anime, title) =>
                {
                    if (userAnime == null)
                    {
                        userAnime = ua;
                        userAnime.Anime = anime;
                        anime.Titles = new List<AnimeTitle>();
                    }

                    if (title != null)
                    {
                        userAnime.Anime!.Titles.Add(title);
                    }

                    return userAnime;
                },
                new { UserId = userId, MalId = malId },
                splitOn: "animeid,titleid"
            );

            return userAnime;
        }

        public async Task<UserAnime> AddToLibraryAsync(UserAnime userAnime)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            userAnime.Id = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO useranime 
                (userid, malid, status, score, episodeswatched, 
                 startdate, finishdate, dateadded, dateupdated)
                VALUES 
                (@UserId, @MalId, @Status, @Score, @EpisodesWatched, 
                 @StartDate, @FinishDate, @DateAdded, @DateUpdated)
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
                    score = @Score, 
                    episodeswatched = @EpisodesWatched,
                    startdate = @StartDate, 
                    finishdate = @FinishDate, 
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
