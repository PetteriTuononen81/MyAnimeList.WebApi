using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;

namespace MyAnimeList.Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnimeMetadataService _animeMetadataService;
    private readonly IAuthService _authService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnimeMetadataService animeMetadataService,
        IAuthService authService,
        ILogger<AnalyticsController> logger)
    {
        _animeMetadataService = animeMetadataService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get library analytics (demographics, top genres, and top themes) for the authenticated user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserAnalyticsDto>> GetUserAnalytics()
    {
        var userId = _authService.GetUserIdFromClaims(User);
        if (userId == null)
        {
            _logger.LogWarning("GET /api/analytics - Unauthorized request: User not authenticated");
            return Unauthorized(new { message = "User not authenticated" });
        }

        _logger.LogInformation("GET /api/analytics - Fetching analytics for UserId: {UserId}", userId.Value);

        var analytics = await _animeMetadataService.GetUserAnalyticsAsync(userId.Value);
        return Ok(analytics);
    }
}