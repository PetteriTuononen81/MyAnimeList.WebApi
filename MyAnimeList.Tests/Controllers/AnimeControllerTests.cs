using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAnimeList.Backend.Controllers;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;
using MyAnimeList.Tests.Fixtures;
using System.Text.Json;
using Xunit;

namespace MyAnimeList.Tests.Controllers
{
    public class AnimeControllerTests
    {
        private readonly Mock<IAnimeService> _mockAnimeService;

        public AnimeControllerTests()
        {
            _mockAnimeService = new Mock<IAnimeService>();
        }

        #region GetAllAnime Tests

        [Fact]
        public async Task GetAllAnime_WithValidData_ReturnsOkResultWithPagination()
        {
            // Arrange
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            _mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime(page: 1, pageSize: 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);

            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);
            Assert.Equal(3, returnValue.Data.Count);
            Assert.Equal(3, returnValue.Pagination.TotalCount);
        }

        [Fact]
        public async Task GetAllAnime_WithDefaultPagination_ReturnsPaginationData()
        {
            // Arrange
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            _mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);

            Assert.Equal(1, returnValue.Pagination.CurrentPage);
            Assert.Equal(20, returnValue.Pagination.PageSize);
            Assert.Equal(3, returnValue.Pagination.TotalCount);
            Assert.Equal(1, returnValue.Pagination.TotalPages);
        }

        [Fact]
        public async Task GetAllAnime_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            _mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime(page: 1, pageSize: 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);

            Assert.Equal(2, returnValue.Data.Count);
            Assert.Equal(2, returnValue.Pagination.TotalPages);
        }

        [Fact]
        public async Task GetAllAnime_WithPaginationPage2_ReturnsSecondPage()
        {
            // Arrange
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            _mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime(page: 2, pageSize: 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);

            Assert.Single(returnValue.Data);
            Assert.Equal(2, returnValue.Pagination.CurrentPage);
        }

        [Fact]
        public async Task GetAllAnime_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            _mockAnimeService
                .Setup(x => x.GetAllAnimeAsync())
                .ReturnsAsync(new List<Anime>());

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act
            var result = await controller.GetAllAnime();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);

            Assert.Empty(returnValue.Data);
            Assert.Equal(0, returnValue.Pagination.TotalCount);
        }

        #endregion

        #region SyncAnimeData Tests

        [Fact]
        public async Task SyncAnimeData_WithValidData_ReturnsOkResult()
        {
            // Arrange
            const int expectedCount = 3;
            _mockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(expectedCount);

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act
            var result = await controller.SyncAnimeData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Serialize to JSON and deserialize to properly access properties
            var json = JsonSerializer.Serialize(okResult.Value);
            using (var jsonDoc = JsonDocument.Parse(json))
            {
                var root = jsonDoc.RootElement;
                Assert.True(root.TryGetProperty("message", out var messageElement));
                Assert.True(root.TryGetProperty("count", out var countElement));

                Assert.Equal("Anime data synced successfully", messageElement.GetString());
                Assert.Equal(expectedCount, countElement.GetInt32());
            }
        }

        [Fact]
        public async Task SyncAnimeData_WithZeroResults_ReturnsOkWithZeroCount()
        {
            // Arrange
            _mockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ReturnsAsync(0);

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act
            var result = await controller.SyncAnimeData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var json = JsonSerializer.Serialize(okResult.Value);
            using (var jsonDoc = JsonDocument.Parse(json))
            {
                var root = jsonDoc.RootElement;
                Assert.True(root.TryGetProperty("count", out var countElement));
                Assert.Equal(0, countElement.GetInt32());
            }
        }

        [Fact]
        public async Task SyncAnimeData_WhenServiceThrowsException_PropagatesException()
        {
            // Arrange
            _mockAnimeService
                .Setup(x => x.SyncAnimeDataAsync())
                .ThrowsAsync(new HttpRequestException("API Error"));

            var controller = new AnimeController(_mockAnimeService.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => controller.SyncAnimeData());
        }

        #endregion
    }
}