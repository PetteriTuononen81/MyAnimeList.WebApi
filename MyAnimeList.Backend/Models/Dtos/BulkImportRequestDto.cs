namespace MyAnimeList.Backend.Models.Dtos
{
    public class BulkImportRequestDto
    {
        public string RawText { get; set; } = string.Empty;
    }

    public class AnimeImportDto
    {
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "plan_to_watch";
        public double? Score { get; set; }
    }
}
