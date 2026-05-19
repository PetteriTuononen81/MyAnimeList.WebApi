namespace MyAnimeList.Backend.Models
{
    public class AnimeTitle
    {
        public int Id { get; set; }
        public int MalId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public Anime Anime { get; set; } = null!;
    }
}
