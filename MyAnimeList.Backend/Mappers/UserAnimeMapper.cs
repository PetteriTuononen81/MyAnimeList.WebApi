namespace MyAnimeList.Backend.Mappers;

using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;

public static class UserAnimeMapper
{
    public static UserAnimeDto ToDto(this UserAnime userAnime)
    {
        if (userAnime == null) return null!;

        return new UserAnimeDto
        {
            Id = userAnime.Id,
            UserId = userAnime.UserId,
            MalId = userAnime.MalId,
            Status = userAnime.Status.ToString(),
            UserScore = userAnime.UserScore,
            Notes = userAnime.Notes,
            DateAdded = userAnime.DateAdded,
            DateUpdated = userAnime.DateUpdated,
            Anime = userAnime.Anime?.ToDto()
        };
    }

    public static AnimeDto ToDto(this Anime anime)
    {
        if (anime == null) return null!;

        return new AnimeDto
        {
            Id = anime.Id,
            MalId = anime.MalId,
            Title = anime.Title,
            EnglishTitle = anime.EnglishTitle,
            Synopsis = anime.Synopsis,
            Episodes = anime.Episodes,
            Status = anime.Status,
            Score = anime.Score,
            ImageUrl = anime.ImageUrl,
            Genre = anime.Genre,
            Titles = anime.Titles?.Select(t => new TitleDto
            {
                Type = t.Type,
                Title = t.Title
            }).ToList()
        };
    }
}