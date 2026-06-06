using Microsoft.AspNetCore.Mvc;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;

namespace MyAnimeList.Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Validation failed", errors });
            }

            var user = await _authService.RegisterAsync(request.Email, request.Username, request.Password);

            if (user == null)
            {
                return BadRequest(new { message = "User with this email or username already exists" });
            }

            var tokens = await _authService.GenerateAuthTokensAsync(user);

            var response = new AuthResponseDto
            {
                Token = tokens.Token,
                RefreshToken = tokens.RefreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username
                }
            };

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Validation failed", errors });
            }

            var user = await _authService.LoginAsync(request.EmailOrUsername, request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var tokens = await _authService.GenerateAuthTokensAsync(user);

            var response = new AuthResponseDto
            {
                Token = tokens.Token,
                RefreshToken = tokens.RefreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username
                }
            };

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new { message = "Validation failed", errors });
            }

            var tokens = await _authService.RefreshTokensAsync(request.RefreshToken);
            if (tokens == null)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            var response = new AuthResponseDto
            {
                Token = tokens.Value.Token,
                RefreshToken = tokens.Value.RefreshToken,
                User = new UserDto
                {
                    Id = tokens.Value.User.Id,
                    Email = tokens.Value.User.Email,
                    Username = tokens.Value.User.Username
                }
            };

            return Ok(response);
        }
    }
}
