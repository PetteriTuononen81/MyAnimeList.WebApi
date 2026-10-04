namespace MyAnimeList.Backend.Models.Response
{
    public class AnimeApiResponse
    {
        public List<Anime> Data { get; set; } = new();
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
