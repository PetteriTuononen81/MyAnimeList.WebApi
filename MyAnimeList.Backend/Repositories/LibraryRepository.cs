using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Repositories
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
        private readonly AnimeDbContext _context;

        public LibraryRepository(AnimeDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserAnime>> GetUserLibraryAsync(int userId, AnimeWatchStatus? status = null)
        {
            var query = _context.UserAnime
                .Include(ua => ua.Anime)
                    .ThenInclude(a => a.Titles)
                .Where(ua => ua.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(ua => ua.Status == status.Value);
            }

            return await query
                .OrderByDescending(ua => ua.DateUpdated)
                .ToListAsync();
        }

        public async Task<UserAnime?> GetUserAnimeAsync(int userId, int malId)
        {
            return await _context.UserAnime
                .Include(ua => ua.Anime)
                    .ThenInclude(a => a.Titles)
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.MalId == malId);
        }

        public async Task<UserAnime> AddToLibraryAsync(UserAnime userAnime)
        {
            await _context.UserAnime.AddAsync(userAnime);
            await _context.SaveChangesAsync();

            // Load the anime entity and its titles for the response
            await _context.Entry(userAnime)
                .Reference(ua => ua.Anime)
                .LoadAsync();

            if (userAnime.Anime != null)
            {
                await _context.Entry(userAnime.Anime)
                    .Collection(a => a.Titles)
                    .LoadAsync();
            }

            return userAnime;
        }

        public async Task<UserAnime> UpdateLibraryItemAsync(UserAnime userAnime)
        {
            userAnime.DateUpdated = DateTime.UtcNow;
            _context.UserAnime.Update(userAnime);
            await _context.SaveChangesAsync();

            // Ensure anime and titles are loaded
            await _context.Entry(userAnime)
                .Reference(ua => ua.Anime)
                .LoadAsync();

            if (userAnime.Anime != null)
            {
                await _context.Entry(userAnime.Anime)
                    .Collection(a => a.Titles)
                    .LoadAsync();
            }

            return userAnime;
        }

        public async Task<bool> RemoveFromLibraryAsync(int userId, int malId)
        {
            var userAnime = await _context.UserAnime
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.MalId == malId);

            if (userAnime == null)
            {
                return false;
            }

            _context.UserAnime.Remove(userAnime);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsAnimeInLibraryAsync(int userId, int malId)
        {
            return await _context.UserAnime
                .AnyAsync(ua => ua.UserId == userId && ua.MalId == malId);
        }
    }
}
