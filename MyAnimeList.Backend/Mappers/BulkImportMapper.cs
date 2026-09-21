namespace MyAnimeList.Backend.Mappers;

using MyAnimeList.Backend.Dtos;
using MyAnimeList.Backend.Models; // Adjust to your Anime entity namespace
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Models.Response;

public static class BulkImportMapper
{
    public static BulkImportCandidateResponse ToCandidateResponse(this AnimeImportDto dto, Anime? matchedAnime)
    {
        return new BulkImportCandidateResponse
        {
            Anime = matchedAnime,
            Status = dto.Status,
            Score = (int?)dto.Score,
            Notes = dto.Notes
        };
    }
}