using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyAnimeList.Backend.Controllers;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Services;
using System.Text.Json;
using Xunit;

namespace MyAnimeList.Tests.Controllers
{
    public static class AnimeControllerTestHarness
    {
        public static AnimeController CreateSut(
            IAnimeService? animeService = null,
            IAiImportService? aiImportService = null,
            ISearchService? searchService = null,
            ILogger<AnimeController>? logger = null)
        {
            return new AnimeController(
                animeService ?? new Mock<IAnimeService>().Object,
                aiImportService ?? new Mock<IAiImportService>().Object,
                searchService ?? new Mock<ISearchService>().Object,
                logger ?? new Mock<ILogger<AnimeController>>().Object
            );
        }

        public static List<Anime> GetSampleAnimeData() => new()
        {
            new Anime { MalId = 1, Title = "One Piece" },
            new Anime { MalId = 2, Title = "Naruto" },
            new Anime { MalId = 3, Title = "Bleach" }
        };
    }

    public class AnimeControllerTests
    {
        #region GetAllAnime Tests

        [Fact]
        public async Task GetAllAnime_WithValidData_ReturnsOkResultWithAnimeList()
        {
            var sampleData = AnimeControllerTestHarness.GetSampleAnimeData();
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            var result = await controller.GetAllAnime();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);

            var returnValue = Assert.IsType<MyAnimeList.Backend.Models.Dtos.AnimeListResponseDto>(okResult.Value);
            Assert.Equal(3, returnValue.Data.Count);
        }

        [Fact]
        public async Task GetAllAnime_WithValidData_ReturnsPaginationData()
        {
            var sampleData = AnimeControllerTestHarness.GetSampleAnimeData();
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            var result = await controller.GetAllAnime(page: 1, pageSize: 20);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<MyAnimeList.Backend.Models.Dtos.AnimeListResponseDto>(okResult.Value);

            Assert.Equal(1, returnValue.Pagination.CurrentPage);
            Assert.Equal(20, returnValue.Pagination.PageSize);
            Assert.Equal(3, returnValue.Pagination.TotalCount);
        }

        [Fact]
        public async Task GetAllAnime_WithEmptyDatabase_ReturnsEmptyList()
        {
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(new List<Anime>());

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            var result = await controller.GetAllAnime();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<MyAnimeList.Backend.Models.Dtos.AnimeListResponseDto>(okResult.Value);

            Assert.Empty(returnValue.Data);
        }

        [Fact]
        public async Task GetAllAnime_CallsServiceMethod()
        {
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(new List<Anime>());

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            await controller.GetAllAnime();

            mockAnimeService.Verify(x => x.GetAllAnimeAsync(), Times.Once);
        }

        #endregion

        #region SyncAnimeData Tests

        [Fact]
        public async Task SyncAnimeData_WithValidData_ReturnsOkResult()
        {
            const int expectedCount = 3;
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(expectedCount);

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            var result = await controller.SyncAnimeData();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.True(root.TryGetProperty("count", out var countElement));

            Assert.Equal("Anime data synced successfully", messageElement.GetString());
            Assert.Equal(expectedCount, countElement.GetInt32());
        }

        [Fact]
        public async Task SyncAnimeData_WithZeroResults_ReturnsOkWithZeroCount()
        {
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(0);

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            var result = await controller.SyncAnimeData();

            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            Assert.True(root.TryGetProperty("count", out var countElement));
            Assert.Equal(0, countElement.GetInt32());
        }

        [Fact]
        public async Task SyncAnimeData_WhenServiceThrowsException_PropagatesException()
        {
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ThrowsAsync(new HttpRequestException("API Error"));

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            await Assert.ThrowsAsync<HttpRequestException>(() => controller.SyncAnimeData());
        }

        [Fact]
        public async Task SyncAnimeData_CallsServiceMethod()
        {
            var mockAnimeService = new Mock<IAnimeService>();
            mockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(10);

            var controller = AnimeControllerTestHarness.CreateSut(animeService: mockAnimeService.Object);

            await controller.SyncAnimeData();

            mockAnimeService.Verify(x => x.SyncAnimeDataAsync(), Times.Once);
        }

        #endregion
    }
}