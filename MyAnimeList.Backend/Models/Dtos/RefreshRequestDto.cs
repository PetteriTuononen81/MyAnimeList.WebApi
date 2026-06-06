using System.ComponentModel.DataAnnotations;

namespace MyAnimeList.Backend.Models.Dtos
{
    public class RefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
