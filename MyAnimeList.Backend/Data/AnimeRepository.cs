using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Repositories
{
    public interface IAnimeRepository
    {
        Task<List<Anime>> GetAllAsync();
        Task AddAsync(Anime anime);
        Task AddRangeAsync(IEnumerable<Anime> animes);
        Task UpdateAsync(Anime anime);
        Task SaveChangesAsync();
    }

    public class AnimeRepository : IAnimeRepository
    {
        private readonly AnimeDbContext _context;

        public AnimeRepository(AnimeDbContext context)
        {
            _context = context;
        }

        public async Task<List<Anime>> GetAllAsync()
        {
            return await _context.Anime
                .OrderByDescending(a => a.Score)
                .ToListAsync();
        }

        public async Task AddAsync(Anime anime)
        {
            await _context.Anime.AddAsync(anime);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Anime> animes)
        {
            await _context.Anime.AddRangeAsync(animes);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Anime anime)
        {
            _context.Anime.Update(anime);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}