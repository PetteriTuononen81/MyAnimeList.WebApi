using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyAnimeList.Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly AnimeDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AnimeDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User?> RegisterAsync(string email, string username, string password)
        {
            if (await GetUserByEmailAsync(email) != null)
                return null;

            if (await GetUserByUsernameAsync(username) != null)
                return null;

            var passwordHash = HashPassword(password);

            var user = new User
            {
                Email = email,
                Username = username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
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
