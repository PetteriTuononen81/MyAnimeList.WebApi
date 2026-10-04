using Dapper;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Database.Repositories;
using Npgsql;
using MyAnimeList.Backend.Mappers;

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
        private readonly IAnimeRepository _animeRepository;
        private readonly IAnimeMetadataService _animeMetadataService;

        public LibraryService(ILibraryRepository libraryRepository, IAnimeRepository animeRepository, IAnimeMetadataService animeMetadataService)
        {
            _libraryRepository = libraryRepository;
            _animeRepository = animeRepository;
            _animeMetadataService = animeMetadataService;
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

            var dtoTasks = userAnimes.Select(async ua =>
            {
                ua.Anime = await _animeRepository.GetByMalIdAsync(ua.MalId);
                return ua.ToDto();
            });

            return (await Task.WhenAll(dtoTasks)).ToList();
        }

        public async Task<UserAnimeDto?> AddToLibraryAsync(int userId, AddToLibraryDto dto)
        {
            if (!Enum.TryParse<AnimeWatchStatus>(dto.Status, true, out var parsedStatus))
            {
                throw new ArgumentException($"Invalid status: {dto.Status}. Valid values are: Watching, Completed, OnGoing, Dropped, PlanToWatch");
            }

            var anime = await _animeRepository.GetByMalIdAsync(dto.MalId);
            if (anime == null)
            {
                throw new ArgumentException($"Anime with MalId {dto.MalId} not found");
            }

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
            added.Anime = await _animeRepository.GetByMalIdAsync(added.MalId);

            _ = _animeMetadataService.GetOrFetchMetadataAsync(dto.MalId);

            return added.ToDto();
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
            updated.Anime = await _animeRepository.GetByMalIdAsync(updated.MalId);
            return updated.ToDto();
        }

        public async Task<bool> RemoveFromLibraryAsync(int userId, int malId)
        {
            return await _libraryRepository.RemoveFromLibraryAsync(userId, malId);
        }
    }
}
