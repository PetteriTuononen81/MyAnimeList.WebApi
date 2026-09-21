namespace MyAnimeList.Backend.Models.Response
{
    public class BulkImportCandidateResponse
    {
        public Anime? Anime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? Score { get; set; }
        public string? Notes { get; set; }
    }
}
