using System.ComponentModel.DataAnnotations;

namespace MyAnimeList.Backend.Models.Dtos
{
    public class AddToLibraryDto
    {
        [Required]
        public int AnimeId { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "Score must be between 1 and 10")]
        public int? UserScore { get; set; }

        public string? Notes { get; set; }
    }
}
