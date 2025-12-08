using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;

namespace MyAnimeList.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimeController : ControllerBase
    {
        private readonly AnimeDbContext _context;
        private readonly JikanApiService _jikanApiService;

        public AnimeController(AnimeDbContext context, JikanApiService jikanApiService)
        {
            _context = context;
            _jikanApiService = jikanApiService;
        }

        [HttpGet]
        public async Task<ActionResult<AnimeListResponseDto>> GetAllAnime([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var totalCount = await _context.Anime.CountAsync();
            var anime = await _context.Anime
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new AnimeListResponseDto
            {
                Data = anime,
                Pagination = new PaginationDto
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Anime>> GetAnimeById(int id)
        {
            var anime = await _context.Anime.FirstOrDefaultAsync(a => a.Id == id);
            if (anime == null)
                return NotFound();

            return Ok(anime);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Anime>>> SearchAnime([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty.");

            var normalizedQuery = query.ToLower();
            var results = await _context.Anime
                .Where(a => a.Title.ToLower().Contains(normalizedQuery) || (a.Synopsis != null && a.Synopsis.ToLower().Contains(normalizedQuery)))
                .Take(50)
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost("sync")]
        public async Task<ActionResult<object>> SyncAnimeData()
        {
            try
            {
                var animeList = await _jikanApiService.FetchAnimeListAsync(page: 1, limit: 25);

                if (!animeList.Any())
                    return Ok(new { message = "No anime data fetched from Jikan API", count = 0 });

                // Clear existing data
                _context.Anime.RemoveRange(_context.Anime);
                await _context.SaveChangesAsync();

                // Add new data
                await _context.Anime.AddRangeAsync(animeList);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Anime data synced successfully", count = animeList.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to sync anime data: {ex.Message}" });
            }
        }
    }
}