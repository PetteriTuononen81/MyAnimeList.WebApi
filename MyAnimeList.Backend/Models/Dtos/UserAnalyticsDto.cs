namespace MyAnimeList.Backend.Models.Dtos;

public class UserAnalyticsDto
{
    public int TotalCompleted { get; set; }
    public int PlanToWatch { get; set; }
    public double TotalEpisodesWatched { get; set; }
    public string? RecentlyCompletedTitle { get; set; }

    public Dictionary<string, int> Demographics { get; set; } = new();
    public Dictionary<string, int> TopGenres { get; set; } = new();
    public Dictionary<string, int> TopThemes { get; set; } = new();
    public Dictionary<string, int> TopStudios { get; set; } = new();
}
