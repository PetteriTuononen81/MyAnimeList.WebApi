using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Repositories
{
    public interface ILibraryRepository
    {
        Task<List<UserAnime>> GetUserLibraryAsync(int userId, AnimeWatchStatus? status = null);
        Task<UserAnime?> GetUserAnimeAsync(int userId, int animeId);
        Task<UserAnime> AddToLibraryAsync(UserAnime userAnime);
        Task<UserAnime> UpdateLibraryItemAsync(UserAnime userAnime);
        Task<bool> RemoveFromLibraryAsync(int userId, int animeId);
        Task<bool> IsAnimeInLibraryAsync(int userId, int animeId);
    }

    public class LibraryRepository : ILibraryRepository
    {
        private readonly AnimeDbContext _context;

        public LibraryRepository(AnimeDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserAnime>> GetUserLibraryAsync(int userId, AnimeWatchStatus? status = null)
        {
            var query = _context.UserAnime
                .Include(ua => ua.Anime)
                .Where(ua => ua.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(ua => ua.Status == status.Value);
            }

            return await query
                .OrderByDescending(ua => ua.DateUpdated)
                .ToListAsync();
        }

        public async Task<UserAnime?> GetUserAnimeAsync(int userId, int animeId)
        {
            return await _context.UserAnime
                .Include(ua => ua.Anime)
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AnimeId == animeId);
        }

        public async Task<UserAnime> AddToLibraryAsync(UserAnime userAnime)
        {
            await _context.UserAnime.AddAsync(userAnime);
            await _context.SaveChangesAsync();

            // Load the anime entity for the response
            await _context.Entry(userAnime)
                .Reference(ua => ua.Anime)
                .LoadAsync();

            return userAnime;
        }

        public async Task<UserAnime> UpdateLibraryItemAsync(UserAnime userAnime)
        {
            userAnime.DateUpdated = DateTime.UtcNow;
            _context.UserAnime.Update(userAnime);
            await _context.SaveChangesAsync();

            // Ensure anime is loaded
            await _context.Entry(userAnime)
                .Reference(ua => ua.Anime)
                .LoadAsync();

            return userAnime;
        }

        public async Task<bool> RemoveFromLibraryAsync(int userId, int animeId)
        {
            var userAnime = await _context.UserAnime
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AnimeId == animeId);

            if (userAnime == null)
            {
                return false;
            }

            _context.UserAnime.Remove(userAnime);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsAnimeInLibraryAsync(int userId, int animeId)
        {
            return await _context.UserAnime
                .AnyAsync(ua => ua.UserId == userId && ua.AnimeId == animeId);
        }
    }
}
