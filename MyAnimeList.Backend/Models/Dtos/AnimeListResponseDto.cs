namespace MyAnimeList.Backend.Models.Dtos
{
    public class AnimeListResponseDto
    {
        public List<Anime> Data { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }
}