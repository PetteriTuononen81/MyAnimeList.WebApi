namespace MyAnimeList.Backend.Models
{
    public class UserAnime
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MalId { get; set; }
        public AnimeWatchStatus Status { get; set; }
        public int? UserScore { get; set; }
        public string? Notes { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public Anime Anime { get; set; } = null!;
    }
}
