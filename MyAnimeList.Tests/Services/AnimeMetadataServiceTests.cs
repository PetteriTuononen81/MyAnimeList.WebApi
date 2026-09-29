using Moq;
using MyAnimeList.Backend.Database.Repositories;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Response;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;
using MyAnimeList.Backend.Services.ApiClient;
using Xunit;

namespace MyAnimeList.Tests.Services;

public class AnimeMetadataServiceTests
{
    private readonly Mock<IAniListApiClient> _mockApiClient;
    private readonly Mock<IAnimeMetadataRepository> _mockRepository;
    private readonly AnimeMetadataService _service;

    public AnimeMetadataServiceTests()
    {
        _mockApiClient = new Mock<IAniListApiClient>();
        _mockRepository = new Mock<IAnimeMetadataRepository>();
        _service = new AnimeMetadataService(_mockApiClient.Object, _mockRepository.Object);
    }

    [Fact]
    public async Task GetOrFetchMetadataAsync_WhenCached_ReturnsCachedDataWithoutCallingApi()
    {
        // Arrange
        int malId = 1;
        var cachedMetadata = new AnimeMetadata { MalId = malId, Demographic = "Shounen" };

        _mockRepository
            .Setup(r => r.GetByMalIdAsync(malId))
            .ReturnsAsync(cachedMetadata);

        // Act
        var result = await _service.GetOrFetchMetadataAsync(malId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Shounen", result.Demographic);

        // Verify local DB was checked and external API was NEVER called
        _mockRepository.Verify(r => r.GetByMalIdAsync(malId), Times.Once);
        _mockApiClient.Verify(a => a.GetMediaByMalIdAsync(It.IsAny<int>()), Times.Never);
        _mockRepository.Verify(r => r.UpsertAsync(It.IsAny<AnimeMetadata>()), Times.Never);
    }

    [Fact]
    public async Task GetOrFetchMetadataAsync_WhenNotCached_FetchesFromApiAndSavesToDb()
    {
        // Arrange
        int malId = 1;
        var apiMedia = new AniListMedia
        {
            IdMal = malId,
            Genres = new List<string> { "Shounen", "Action" },
            Tags = new List<AniListTag>
            {
                new() { Name = "Superpowers", Category = "Theme", IsGeneralSpoiler = false, Rank = 80 }
            }
        };

        _mockRepository
            .Setup(r => r.GetByMalIdAsync(malId))
            .ReturnsAsync((AnimeMetadata?)null);

        _mockApiClient
            .Setup(a => a.GetMediaByMalIdAsync(malId))
            .ReturnsAsync(apiMedia);

        // Act
        var result = await _service.GetOrFetchMetadataAsync(malId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Shounen", result.Demographic);
        Assert.Contains("Action", result.Genres);
        Assert.Contains("Superpowers", result.Themes);

        // Verify API was called and data was upserted into DB
        _mockApiClient.Verify(a => a.GetMediaByMalIdAsync(malId), Times.Once);
        _mockRepository.Verify(r => r.UpsertAsync(It.IsAny<AnimeMetadata>()), Times.Once);
    }

    [Fact]
    public async Task GetUserAnalyticsAsync_CalculatesCorrectAggregates()
    {
        // Arrange
        int userId = 42;
        var userLibraryMetadata = new List<AnimeMetadata>
        {
            new()
            {
                MalId = 1,
                Demographic = "Shounen",
                Genres = new List<string> { "Action", "Comedy" },
                Themes = new List<string> { "Superpowers" }
            },
            new()
            {
                MalId = 2,
                Demographic = "Shounen",
                Genres = new List<string> { "Action", "Drama" },
                Themes = new List<string> { "Superpowers", "School" }
            }
        };

        _mockRepository
            .Setup(r => r.GetMetadataForUserLibraryAsync(userId))
            .ReturnsAsync(userLibraryMetadata);

        // Act
        var analytics = await _service.GetUserAnalyticsAsync(userId);

        // Assert
        Assert.NotNull(analytics);

        // Demographics
        Assert.Equal(2, analytics.Demographics["Shounen"]);

        // Top Genres
        Assert.Equal(2, analytics.TopGenres["Action"]);
        Assert.Equal(1, analytics.TopGenres["Comedy"]);

        // Top Themes
        Assert.Equal(2, analytics.TopThemes["Superpowers"]);
        Assert.Equal(1, analytics.TopThemes["School"]);
    }
}