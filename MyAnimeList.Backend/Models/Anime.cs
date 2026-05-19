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

        public ICollection<AnimeTitle> Titles { get; set; } = new List<AnimeTitle>();

        public string? GetTitleByType(string type)
        {
            return Titles.FirstOrDefault(t => t.Type.Equals(type, StringComparison.OrdinalIgnoreCase))?.Title;
        }

        public string GetDefaultTitle()
        {
            return GetTitleByType("Default") ?? Title;
        }

        public string? GetEnglishTitleFromCollection()
        {
            return GetTitleByType("English") ?? EnglishTitle;
        }

        public string? GetJapaneseTitle()
        {
            return GetTitleByType("Japanese");
        }

        public List<string> GetSynonymTitles()
        {
            return Titles.Where(t => t.Type.Equals("Synonym", StringComparison.OrdinalIgnoreCase))
                         .Select(t => t.Title)
                         .ToList();
        }

        public void Update(Anime newAnime)
        {
            Title = newAnime.Title;
            Episodes = newAnime.Episodes;
            Status = newAnime.Status;
            Score = newAnime.Score;
            Synopsis = newAnime.Synopsis;
            ImageUrl = newAnime.ImageUrl;
            Genre = newAnime.Genre;
            AiredFrom = newAnime.AiredFrom;
            AiredTo = newAnime.AiredTo;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}