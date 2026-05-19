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
        private readonly ILogger<LibraryController> _logger;

        public LibraryController(ILibraryService libraryService, IAuthService authService, ILogger<LibraryController> logger)
        {
            _libraryService = libraryService;
            _authService = authService;
            _logger = logger;
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
                _logger.LogWarning("GET /api/library - Unauthorized request: User not authenticated");
                return Unauthorized(new { message = "User not authenticated" });
            }

            _logger.LogInformation("GET /api/library - UserId: {UserId}, Status filter: {Status}", userId.Value, status ?? "none");

            try
            {
                var library = await _libraryService.GetUserLibraryAsync(userId.Value, status);
                _logger.LogInformation("GET /api/library - UserId: {UserId}, Returned {Count} items", userId.Value, library.Count);
                return Ok(library);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("GET /api/library - UserId: {UserId}, Invalid status filter: {Error}", userId.Value, ex.Message);
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

                _logger.LogWarning("POST /api/library - Validation failed: {Errors}", string.Join(", ", errors));
                return BadRequest(new { message = "Validation failed", errors });
            }

            var userId = _authService.GetUserIdFromClaims(User);
            if (userId == null)
            {
                _logger.LogWarning("POST /api/library - Unauthorized request: User not authenticated");
                return Unauthorized(new { message = "User not authenticated" });
            }

            _logger.LogInformation("POST /api/library - UserId: {UserId}, MalId: {MalId}, Status: {Status}, Score: {Score}", 
                userId.Value, dto.MalId, dto.Status, dto.UserScore?.ToString() ?? "none");

            try
            {
                var result = await _libraryService.AddToLibraryAsync(userId.Value, dto);
                if (result == null)
                {
                    _logger.LogError("POST /api/library - UserId: {UserId}, Failed to add MalId: {MalId}", userId.Value, dto.MalId);
                    return BadRequest(new { message = "Failed to add anime to library" });
                }

                _logger.LogInformation("POST /api/library - UserId: {UserId}, Successfully added MalId: {MalId}", userId.Value, dto.MalId);
                return CreatedAtAction(nameof(GetLibrary), new { }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("POST /api/library - UserId: {UserId}, ArgumentException: {Error}", userId.Value, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("POST /api/library - UserId: {UserId}, Conflict: {Error}", userId.Value, ex.Message);
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update an anime entry in user's library (status, score, notes)
        /// </summary>
        [HttpPut("{malId}")]
        public async Task<ActionResult<UserAnimeDto>> UpdateLibraryItem(int malId, [FromBody] UpdateLibraryDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("PUT /api/library/{MalId} - Validation failed: {Errors}", malId, string.Join(", ", errors));
                return BadRequest(new { message = "Validation failed", errors });
            }

            var userId = _authService.GetUserIdFromClaims(User);
            if (userId == null)
            {
                _logger.LogWarning("PUT /api/library/{MalId} - Unauthorized request: User not authenticated", malId);
                return Unauthorized(new { message = "User not authenticated" });
            }

            _logger.LogInformation("PUT /api/library/{MalId} - UserId: {UserId}, Status: {Status}, Score: {Score}, Notes: {HasNotes}", 
                malId, userId.Value, dto.Status ?? "unchanged", dto.UserScore?.ToString() ?? "unchanged", dto.Notes != null ? "provided" : "none");

            try
            {
                var result = await _libraryService.UpdateLibraryItemAsync(userId.Value, malId, dto);
                if (result == null)
                {
                    _logger.LogWarning("PUT /api/library/{MalId} - UserId: {UserId}, Anime not found in library", malId, userId.Value);
                    return NotFound(new { message = $"Anime with MalId {malId} not found in your library" });
                }

                _logger.LogInformation("PUT /api/library/{MalId} - UserId: {UserId}, Successfully updated", malId, userId.Value);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("PUT /api/library/{MalId} - UserId: {UserId}, ArgumentException: {Error}", malId, userId.Value, ex.Message);
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
                _logger.LogWarning("DELETE /api/library/{AnimeId} - Unauthorized request: User not authenticated", animeId);
                return Unauthorized(new { message = "User not authenticated" });
            }

            _logger.LogInformation("DELETE /api/library/{AnimeId} - UserId: {UserId}", animeId, userId.Value);

            var result = await _libraryService.RemoveFromLibraryAsync(userId.Value, animeId);
            if (!result)
            {
                _logger.LogWarning("DELETE /api/library/{AnimeId} - UserId: {UserId}, Anime not found in library", animeId, userId.Value);
                return NotFound(new { message = $"Anime with ID {animeId} not found in your library" });
            }

            _logger.LogInformation("DELETE /api/library/{AnimeId} - UserId: {UserId}, Successfully removed", animeId, userId.Value);
            return NoContent();
        }
    }
}
