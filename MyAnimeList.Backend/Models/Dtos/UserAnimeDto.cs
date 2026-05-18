using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Models.Dtos
{
    public class UserAnimeDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AnimeId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? UserScore { get; set; }
        public string? Notes { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
        public AnimeDto? Anime { get; set; }
    }

    public class AnimeDto
    {
        public int Id { get; set; }
        public int MalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? EnglishTitle { get; set; }
        public string? Synopsis { get; set; }
        public int Episodes { get; set; }
        public string? Status { get; set; }
        public double? Score { get; set; }
        public string? ImageUrl { get; set; }
        public string? Genre { get; set; }
    }
}
