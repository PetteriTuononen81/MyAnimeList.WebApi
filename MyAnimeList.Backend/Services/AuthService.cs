using Microsoft.IdentityModel.Tokens;
using MyAnimeList.Backend.Database.Repositories;
using MyAnimeList.Backend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyAnimeList.Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration, IAuthRepository authRepository)
        {
            _configuration = configuration;
            _authRepository = authRepository;
        }

        public async Task<User?> RegisterAsync(string email, string username, string password)
        {
            if (await _authRepository.GetUserByEmailAsync(email) != null)
                return null;

            if (await _authRepository.GetUserByUsernameAsync(username) != null)
                return null;

            var passwordHash = HashPassword(password);

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            user.Id = await _authRepository.CreateUserAsync(user);
            return user;
        }

        public async Task<User?> LoginAsync(string emailOrUsername, string password)
        {
            // Try to find user by email first, then by username
            var user = await GetUserByEmailAsync(emailOrUsername);
            if (user == null)
            {
                user = await GetUserByUsernameAsync(emailOrUsername);
            }

            if (user == null)
                return null;

            if (!VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            return _authRepository.GetUserByEmailAsync(email);
        }

        public Task<User?> GetUserByUsernameAsync(string username)
        {
            return _authRepository.GetUserByUsernameAsync(username);
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"] ?? "MyAnimeList.Backend";
            var audience = jwtSettings["Audience"] ?? "MyAnimeList.Frontend";
            var expiryInMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public Task<User?> GetUserByIdAsync(int userId)
        {
            return _authRepository.GetUserByIdAsync(userId);
        }

        public async Task<(string Token, string RefreshToken)> GenerateAuthTokensAsync(User user)
        {
            var token = GenerateJwtToken(user);
            var (refreshToken, refreshTokenExpiry) = CreateRefreshTokenWithExpiry();

            await _authRepository.SaveRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);
            return (token, refreshToken);
        }

        public async Task<(string Token, string RefreshToken, User User)?> RefreshTokensAsync(string refreshToken)
        {
            var storedToken = await _authRepository.GetRefreshTokenAsync(refreshToken);
            if (storedToken == null || storedToken.RevokedAt != null || storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            var user = await _authRepository.GetUserByIdAsync(storedToken.UserId);
            if (user == null)
            {
                return null;
            }

            // Rotate refresh token on refresh
            await _authRepository.RevokeRefreshTokenAsync(storedToken.Token);

            var token = GenerateJwtToken(user);
            var (newRefreshToken, refreshTokenExpiry) = CreateRefreshTokenWithExpiry();

            await _authRepository.SaveRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiry);
            return (token, newRefreshToken, user);
        }

        private (string RefreshToken, DateTime ExpiresAt) CreateRefreshTokenWithExpiry()
        {
            var refreshToken = GenerateRefreshToken();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var refreshExpiryDays = int.Parse(jwtSettings["RefreshTokenExpiryInDays"] ?? "7");
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshExpiryDays);
            return (refreshToken, refreshTokenExpiry);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }


        public int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        private static string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = hmac.ComputeHash(passwordBytes);
            var key = hmac.Key;

            var combined = new byte[key.Length + hashBytes.Length];
            Buffer.BlockCopy(key, 0, combined, 0, key.Length);
            Buffer.BlockCopy(hashBytes, 0, combined, key.Length, hashBytes.Length);

            return Convert.ToBase64String(combined);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var hashBytes = Convert.FromBase64String(storedHash);

            var key = new byte[128];
            Buffer.BlockCopy(hashBytes, 0, key, 0, 128);

            using var hmac = new HMACSHA512(key);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var computedHash = hmac.ComputeHash(passwordBytes);

            var storedPasswordHash = new byte[hashBytes.Length - 128];
            Buffer.BlockCopy(hashBytes, 128, storedPasswordHash, 0, storedPasswordHash.Length);

            return computedHash.SequenceEqual(storedPasswordHash);
        }
    }
}
