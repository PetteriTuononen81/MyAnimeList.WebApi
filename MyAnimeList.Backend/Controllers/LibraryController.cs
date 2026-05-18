using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;

namespace MyAnimeList.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LibraryController : ControllerBase
    {
        private readonly ILibraryService _libraryService;
        private readonly IAuthService _authService;

        public LibraryController(ILibraryService libraryService, IAuthService authService)
        {
            _libraryService = libraryService;
            _authService = authService;
        }

        /// <summary>
        /// Get user's anime library, optionally filtered by watch status
        /// </summary>
        /// <param name="status">Optional filter: Watching, Completed, OnGoing, Dropped, PlanToWatch</param>
        [HttpGet]
        public async Task<ActionResult<List<UserAnimeDto>>> GetLibrary([FromQuery] string? status = null)
        {
            var userId = _authService.GetUserIdFromClaims(User);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var library = await _libraryService.GetUserLibraryAsync(userId.Value, status);
                return Ok(library);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Add an anime to user's library
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserAnimeDto>> AddToLibrary([FromBody] AddToLibraryDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Validation failed", errors });
            }

            var userId = _authService.GetUserIdFromClaims(User);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var result = await _libraryService.AddToLibraryAsync(userId.Value, dto);
                if (result == null)
                {
                    return BadRequest(new { message = "Failed to add anime to library" });
                }

                return CreatedAtAction(nameof(GetLibrary), new { }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an anime entry in user's library (status, score, notes)
        /// </summary>
        [HttpPut("{animeId}")]
        public async Task<ActionResult<UserAnimeDto>> UpdateLibraryItem(int animeId, [FromBody] UpdateLibraryDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Validation failed", errors });
            }

            var userId = _authService.GetUserIdFromClaims(User);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var result = await _libraryService.UpdateLibraryItemAsync(userId.Value, animeId, dto);
                if (result == null)
                {
                    return NotFound(new { message = $"Anime with ID {animeId} not found in your library" });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove an anime from user's library
        /// </summary>
        [HttpDelete("{animeId}")]
        public async Task<ActionResult> RemoveFromLibrary(int animeId)
        {
            var userId = _authService.GetUserIdFromClaims(User);
            if (userId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var result = await _libraryService.RemoveFromLibraryAsync(userId.Value, animeId);
            if (!result)
            {
                return NotFound(new { message = $"Anime with ID {animeId} not found in your library" });
            }

            return NoContent();
        }
    }
}
