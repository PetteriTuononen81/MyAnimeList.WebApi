using Dapper;
using Microsoft.Extensions.Configuration;
using MyAnimeList.Backend.Models;
using Npgsql;

namespace MyAnimeList.Backend.Database.Repositories
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByIdAsync(int userId);
        Task<int> CreateUserAsync(User user);
        Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiresAt);
        Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }

    public class AuthRepository : IAuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not found");
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE email = @Email",
                new { Email = email });
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE username = @Username",
                new { Username = username });
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE id = @UserId",
                new { UserId = userId });
        }

        public async Task<int> CreateUserAsync(User user)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO users (email, username, passwordhash, createdat)
                VALUES (@Email, @Username, @PasswordHash, @CreatedAt)
                RETURNING id",
                user);
        }

        public async Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiresAt)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO refreshtokens (userid, token, expiresat, createdat)
                VALUES (@UserId, @Token, @ExpiresAt, @CreatedAt)",
                new { UserId = userId, Token = refreshToken, ExpiresAt = expiresAt, CreatedAt = DateTime.UtcNow });
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                "SELECT * FROM refreshtokens WHERE token = @Token",
                new { Token = refreshToken });
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE refreshtokens
                SET revokedat = @RevokedAt
                WHERE token = @Token",
                new { RevokedAt = DateTime.UtcNow, Token = refreshToken });
        }
    }
}
