using Microsoft.AspNetCore.Mvc;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;

namespace MyAnimeList.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnimeController : ControllerBase
    {
        private readonly IAnimeService _animeService;

        public AnimeController(IAnimeService animeService)
        {
            _animeService = animeService;
        }

        /// <summary>
        /// Gets paginated anime from PostgreSQL database
        /// Used for infinite scroll - returns anime in pages
        /// </summary>
        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedAnimeResponseDto>> GetAnimePaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (animes, totalCount) = await _animeService.GetAnimePaginatedAsync(page, pageSize);

            var response = new PaginatedAnimeResponseDto
            {
                Data = animes,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = page < (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }

        /// <summary>
        /// Search anime by title, english title, or genre with pagination
        /// Used for infinite scroll search results
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<PaginatedAnimeResponseDto>> SearchAnime(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Search query cannot be empty" });
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (animes, totalCount) = await _animeService.SearchAnimeAsync(query, page, pageSize);

            var response = new PaginatedAnimeResponseDto
            {
                Data = animes,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasNextPage = page < (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets all anime from PostgreSQL database (for app initial load or backup)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<AnimeListResponseDto>> GetAllAnime(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var allAnime = await _animeService.GetAllAnimeAsync();

            // Apply pagination HERE
            var paginatedAnime = allAnime
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new AnimeListResponseDto
            {
                Data = paginatedAnime,  // Only send paginated data
                Pagination = new PaginationDto
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = allAnime.Count,
                    TotalPages = (int)Math.Ceiling(allAnime.Count / (double)pageSize)
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// Syncs anime data from Jikan API to PostgreSQL database
        /// ONLY called by cron job (monthly)
        /// </summary>
        [HttpPost("sync")]
        public async Task<ActionResult> SyncAnimeData([FromQuery] int? maxPages = null)
        {
            var count = await _animeService.SyncAnimeDataAsync(maxPages);
            return Ok(new { message = "Anime data synced successfully", count = count });
        }
    }
}