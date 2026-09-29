namespace MyAnimeList.Backend.Models.Dtos;

public class UserAnalyticsDto
{
    public Dictionary<string, int> Demographics { get; set; } = new();
    public Dictionary<string, int> TopGenres { get; set; } = new();
    public Dictionary<string, int> TopThemes { get; set; } = new();
}
