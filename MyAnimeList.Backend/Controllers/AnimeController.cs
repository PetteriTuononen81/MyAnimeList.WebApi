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
        /// Gets all anime from PostgreSQL database
        /// Does NOT call external API - only returns cached data
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Anime>>> GetAllAnime()
        {
            var allAnime = await _animeService.GetAllAnimeAsync();
            
            var sortedAnime = allAnime
                .OrderByDescending(a => a.Score)
                .ThenBy(a => a.Title)
                .ToList();

            return Ok(sortedAnime);
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