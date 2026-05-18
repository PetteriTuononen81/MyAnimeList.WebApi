using System.ComponentModel.DataAnnotations;

namespace MyAnimeList.Backend.Models.Dtos
{
    public class UpdateLibraryDto
    {
        public string? Status { get; set; }

        [Range(1, 10, ErrorMessage = "Score must be between 1 and 10")]
        public int? UserScore { get; set; }

        public string? Notes { get; set; }
    }
}
