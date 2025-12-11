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
        /// Gets all anime from PostgreSQL database with pagination
        /// Does NOT call external API - only returns cached data
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<AnimeListResponseDto>> GetAllAnime([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var allAnime = await _animeService.GetAllAnimeAsync();
            var paginatedAnime = allAnime
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new AnimeListResponseDto
            {
                Data = paginatedAnime,
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
        /// NOT called by the Android app
        /// </summary>
        [HttpPost("sync")]
        public async Task<ActionResult> SyncAnimeData()
        {
            var count = await _animeService.SyncAnimeDataAsync();
            return Ok(new { message = "Anime data synced successfully", count = count });
        }
    }
}