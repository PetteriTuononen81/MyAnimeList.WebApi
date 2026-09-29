using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;

namespace MyAnimeList.Backend.Models;

public class AnimeMetadata
{
    private static readonly string[] DemographicNames = ["Shounen", "Seinen", "Shoujo", "Josei"];

    public int Id { get; set; }
    public int MalId { get; set; }
    public string? Demographic { get; set; }
    public List<string> Themes { get; set; } = new();
    public List<string> Genres { get; set; } = new();

    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;


    public static AnimeMetadata FromAniListMedia(AniListMedia media)
    {
        var demographic = media.Genres
            .FirstOrDefault(g => DemographicNames.Contains(g, StringComparer.OrdinalIgnoreCase));

        var genres = media.Genres
            .Where(g => !DemographicNames.Contains(g, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var themes = media.Tags
            .Where(t => t.Category.Equals("Theme", StringComparison.OrdinalIgnoreCase)
                        && !t.IsGeneralSpoiler
                        && t.Rank >= 60)
            .Select(t => t.Name)
            .ToList();

        return new AnimeMetadata
        {
            MalId = media.IdMal,
            Demographic = demographic,
            Genres = genres,
            Themes = themes,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }
}