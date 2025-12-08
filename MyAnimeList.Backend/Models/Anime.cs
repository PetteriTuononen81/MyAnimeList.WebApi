namespace MyAnimeList.Backend.Models
{
    public class Anime
    {
        public int Id { get; set; }
        public int MalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? EnglishTitle { get; set; }
        public string? Synopsis { get; set; }
        public int Episodes { get; set; }
        public string? Status { get; set; }
        public DateTime? AiredFrom { get; set; }
        public DateTime? AiredTo { get; set; }
        public double? Score { get; set; }
        public string? ImageUrl { get; set; }
        public string? Genre { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}