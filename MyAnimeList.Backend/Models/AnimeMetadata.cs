using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;

namespace MyAnimeList.Backend.Models;

public class AnimeMetadata
{
    private static readonly string[] DemographicNames = ["Shounen", "Seinen", "Shoujo", "Josei"];

    public int Id { get; set; }
    public int MalId { get; set; }
    public string[] Genres { get; set; } = Array.Empty<string>();
    public string[] Themes { get; set; } = Array.Empty<string>();
    public string Demographic { get; set; } = string.Empty;

    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;


    public static AnimeMetadata FromAniListMedia(AniListMedia media)
    {
        var demographic = media.Tags?
            .FirstOrDefault(t => t.Category.Equals("Demographic", StringComparison.OrdinalIgnoreCase))
            ?.Name;

        var genres = media.Genres?.ToArray() ?? Array.Empty<string>();

        var themes = media.Tags?
            .Where(t => t.Category.StartsWith("Theme", StringComparison.OrdinalIgnoreCase)
                        && !t.IsGeneralSpoiler
                        && t.Rank >= 60)
            .Select(t => t.Name)
            .ToArray() ?? Array.Empty<string>();

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