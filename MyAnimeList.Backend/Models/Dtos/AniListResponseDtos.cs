namespace MyAnimeList.Backend.Models.Dtos;

public class AniListResponseWrapper
{
    public AniListData? Data { get; set; }
}

public class AniListData
{
    public AniListMedia? Media { get; set; }
}

public class AniListMedia
{
    public int IdMal { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<AniListTag> Tags { get; set; } = new();
}

public class AniListTag
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsGeneralSpoiler { get; set; }
    public int Rank { get; set; }
}