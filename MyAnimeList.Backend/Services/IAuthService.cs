using MyAnimeList.Backend.Models;
using System.Security.Claims;

namespace MyAnimeList.Backend.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(string email, string username, string password);
        Task<User?> LoginAsync(string email, string password);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        string GenerateJwtToken(User user);
        int? GetUserIdFromClaims(ClaimsPrincipal user);
    }
}
