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

            var token = _authService.GenerateJwtToken(user);

            var response = new AuthResponseDto
            {
                Token = token,
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

            var token = _authService.GenerateJwtToken(user);

            var response = new AuthResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username
                }
            };

            return Ok(response);
        }
    }
}
