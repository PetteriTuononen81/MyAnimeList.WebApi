using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAnimeList.Backend.Controllers;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Services;
using MyAnimeList.Tests.Fixtures;
using System.Text.Json;
using Xunit;

namespace MyAnimeList.Tests.Controllers
{
    public class AnimeControllerTests : IClassFixture<AnimeControllerFixture>
    {
        private readonly AnimeControllerFixture _fixture;
        private Mock<IAnimeService> MockAnimeService => _fixture.MockAnimeService;

        public AnimeControllerTests(AnimeControllerFixture fixture)
        {
            _fixture = fixture;
            _fixture.ResetMocks(); // Reset before each test
        }

        private static List<Anime> GetSampleAnimeData() => AnimeControllerFixture.GetSampleAnimeData();

        #region GetAllAnime Tests

        [Fact]
        public async Task GetAllAnime_WithValidData_ReturnsOkResultWithAnimeList()
        {
            // Arrange
            var sampleData = GetSampleAnimeData();
            MockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(MockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);

            var returnValue = Assert.IsType<MyAnimeList.Backend.Models.Dtos.AnimeListResponseDto>(okResult.Value);
            Assert.Equal(3, returnValue.Data.Count);
        }

        [Fact]
        public async Task GetAllAnime_WithValidData_ReturnsPaginationData()
        {
            // Arrange
            var sampleData = GetSampleAnimeData();
            MockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(MockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime(page: 1, pageSize: 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<MyAnimeList.Backend.Models.Dtos.AnimeListResponseDto>(okResult.Value);

            Assert.Equal(1, returnValue.Pagination.CurrentPage);
            Assert.Equal(20, returnValue.Pagination.PageSize);
            Assert.Equal(3, returnValue.Pagination.TotalCount);
        }

        [Fact]
        public async Task GetAllAnime_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            MockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(new List<Anime>());

            var controller = new AnimeController(MockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<MyAnimeList.Backend.Models.Dtos.AnimeListResponseDto>(okResult.Value);

            Assert.Empty(returnValue.Data);
        }

        [Fact]
        public async Task GetAllAnime_CallsServiceMethod()
        {
            // Arrange
            MockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(new List<Anime>());

            var controller = new AnimeController(MockAnimeService.Object);

            // Act
            await controller.GetAllAnime();

            // Assert
            MockAnimeService.Verify(x => x.GetAllAnimeAsync(), Times.Once);
        }

        #endregion

        #region SyncAnimeData Tests

        [Fact]
        public async Task SyncAnimeData_WithValidData_ReturnsOkResult()
        {
            // Arrange
            const int expectedCount = 3;
            MockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(expectedCount);

            var controller = new AnimeController(MockAnimeService.Object);

            // Act
            var result = await controller.SyncAnimeData();

            // Assert
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
            // Arrange
            MockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(0);

            var controller = new AnimeController(MockAnimeService.Object);

            // Act
            var result = await controller.SyncAnimeData();

            // Assert
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
            // Arrange
            MockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ThrowsAsync(new HttpRequestException("API Error"));

            var controller = new AnimeController(MockAnimeService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => controller.SyncAnimeData());
        }

        [Fact]
        public async Task SyncAnimeData_CallsServiceMethod()
        {
            // Arrange
            MockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(10);

            var controller = new AnimeController(MockAnimeService.Object);

            // Act
            await controller.SyncAnimeData();

            // Assert
            MockAnimeService.Verify(x => x.SyncAnimeDataAsync(), Times.Once);
        }

        #endregion
    }
}
