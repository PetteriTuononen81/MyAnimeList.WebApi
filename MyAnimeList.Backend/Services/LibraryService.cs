using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Repositories;
using MyAnimeList.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace MyAnimeList.Backend.Services
{
    public interface ILibraryService
    {
        Task<List<UserAnimeDto>> GetUserLibraryAsync(int userId, string? statusFilter = null);
        Task<UserAnimeDto?> AddToLibraryAsync(int userId, AddToLibraryDto dto);
        Task<UserAnimeDto?> UpdateLibraryItemAsync(int userId, int malId, UpdateLibraryDto dto);
        Task<bool> RemoveFromLibraryAsync(int userId, int malId);
    }

    public class LibraryService : ILibraryService
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly AnimeDbContext _context;

        public LibraryService(ILibraryRepository libraryRepository, AnimeDbContext context)
        {
            _libraryRepository = libraryRepository;
            _context = context;
        }

        public async Task<List<UserAnimeDto>> GetUserLibraryAsync(int userId, string? statusFilter = null)
        {
            AnimeWatchStatus? status = null;

            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (!Enum.TryParse<AnimeWatchStatus>(statusFilter, true, out var parsedStatus))
                {
                    throw new ArgumentException($"Invalid status: {statusFilter}. Valid values are: Watching, Completed, OnGoing, Dropped, PlanToWatch");
                }
                status = parsedStatus;
            }

            var userAnimes = await _libraryRepository.GetUserLibraryAsync(userId, status);

            return userAnimes.Select(ua => MapToDto(ua)).ToList();
        }

        public async Task<UserAnimeDto?> AddToLibraryAsync(int userId, AddToLibraryDto dto)
        {
            // Validate status
            if (!Enum.TryParse<AnimeWatchStatus>(dto.Status, true, out var parsedStatus))
            {
                throw new ArgumentException($"Invalid status: {dto.Status}. Valid values are: Watching, Completed, OnGoing, Dropped, PlanToWatch");
            }

            // Check if anime exists by MalId
            var anime = await _context.Anime.FirstOrDefaultAsync(a => a.MalId == dto.MalId);
            if (anime == null)
            {
                throw new ArgumentException($"Anime with MalId {dto.MalId} not found");
            }

            // Check if already in library
            var existing = await _libraryRepository.IsAnimeInLibraryAsync(userId, dto.MalId);
            if (existing)
            {
                throw new InvalidOperationException($"Anime is already in your library");
            }

            var userAnime = new UserAnime
            {
                UserId = userId,
                MalId = dto.MalId,
                Status = parsedStatus,
                UserScore = dto.UserScore,
                Notes = dto.Notes,
                DateAdded = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            };

            var added = await _libraryRepository.AddToLibraryAsync(userAnime);
            return MapToDto(added);
        }

        public async Task<UserAnimeDto?> UpdateLibraryItemAsync(int userId, int malId, UpdateLibraryDto dto)
        {
            var userAnime = await _libraryRepository.GetUserAnimeAsync(userId, malId);

            if (userAnime == null)
            {
                return null;
            }

            // Update status if provided
            if (!string.IsNullOrEmpty(dto.Status))
            {
                if (!Enum.TryParse<AnimeWatchStatus>(dto.Status, true, out var parsedStatus))
                {
                    throw new ArgumentException($"Invalid status: {dto.Status}. Valid values are: Watching, Completed, OnGoing, Dropped, PlanToWatch");
                }
                userAnime.Status = parsedStatus;
            }

            // Update score if provided
            if (dto.UserScore.HasValue)
            {
                userAnime.UserScore = dto.UserScore;
            }

            // Update notes (can be set to null)
            if (dto.Notes != null)
            {
                userAnime.Notes = dto.Notes;
            }

            var updated = await _libraryRepository.UpdateLibraryItemAsync(userAnime);
            return MapToDto(updated);
        }

        public async Task<bool> RemoveFromLibraryAsync(int userId, int malId)
        {
            return await _libraryRepository.RemoveFromLibraryAsync(userId, malId);
        }

        private UserAnimeDto MapToDto(UserAnime userAnime)
        {
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
                Anime = userAnime.Anime != null ? new AnimeDto
                {
                    Id = userAnime.Anime.Id,
                    MalId = userAnime.Anime.MalId,
                    Title = userAnime.Anime.Title,
                    EnglishTitle = userAnime.Anime.EnglishTitle,
                    Synopsis = userAnime.Anime.Synopsis,
                    Episodes = userAnime.Anime.Episodes,
                    Status = userAnime.Anime.Status,
                    Score = userAnime.Anime.Score,
                    ImageUrl = userAnime.Anime.ImageUrl,
                    Genre = userAnime.Anime.Genre,
                    Titles = userAnime.Anime.Titles?.Select(t => new TitleDto
                    {
                        Type = t.Type,
                        Title = t.Title
                    }).ToList()
                } : null
            };
        }
    }
}
