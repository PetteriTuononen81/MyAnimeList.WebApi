namespace MyAnimeList.Backend.Mappers;

using MyAnimeList.Backend.Models; 
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Models.Response;

public static class BulkImportMapper
{
    public static BulkImportCandidateResponse ToCandidateResponse(this AnimeImportDto dto, Anime? matchedAnime)
    {
        return new BulkImportCandidateResponse
        {
            Anime = matchedAnime,
            InputTitle = dto.Title,
            Status = dto.Status,
            Score = (int?)dto.Score,
            Notes = dto.Notes
        };
    }
}