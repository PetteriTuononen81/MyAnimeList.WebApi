using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyAnimeList.Backend.Controllers;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Models.Dtos;
using MyAnimeList.Backend.Services;
using MyAnimeList.Tests.Fixtures;
using Xunit;

namespace MyAnimeList.Tests.Controllers
{
    public class AnimeControllerTests
    {
        private readonly AnimeDbContextFixture _fixture;
        private readonly Mock<JikanApiService> _mockJikanApiService;

        public AnimeControllerTests()
        {
            _fixture = new AnimeDbContextFixture();
            _mockJikanApiService = new Mock<JikanApiService>(new HttpClient());
        }

        #region GetAllAnime Tests

        [Fact]
        public async Task GetAllAnime_WithValidData_ReturnsOkResultWithPagination()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.GetAllAnime(page: 1, pageSize: 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);

            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);
            Assert.Equal(3, returnValue.Data.Count);
        }

        [Fact]
        public async Task GetAllAnime_WithDefaultPagination_ReturnsPaginationData()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

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
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.GetAllAnime(page: 1, pageSize: 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);

            Assert.Equal(2, returnValue.Data.Count);
            Assert.Equal(2, returnValue.Pagination.TotalPages);
        }

        [Fact]
        public async Task GetAllAnime_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.GetAllAnime();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<AnimeListResponseDto>(okResult.Value);

            Assert.Empty(returnValue.Data);
            Assert.Equal(0, returnValue.Pagination.TotalCount);
        }

        #endregion

        #region GetAnimeById Tests

        [Fact]
        public async Task GetAnimeById_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.GetAnimeById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<Anime>(okResult.Value);

            Assert.NotNull(returnValue);
            Assert.Equal(1, returnValue.Id);
            Assert.Equal("Cowboy Bebop", returnValue.Title);
        }

        [Fact]
        public async Task GetAnimeById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.GetAnimeById(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetAnimeById_WithMultipleAnime_ReturnsCorrectAnime()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.GetAnimeById(3);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<Anime>(okResult.Value);

            Assert.NotNull(returnValue);
            Assert.Equal("Attack on Titan", returnValue.Title);
            Assert.Equal(8.52, returnValue.Score);
        }

        #endregion

        #region SearchAnime Tests

        [Fact]
        public async Task SearchAnime_WithValidQuery_ReturnsMatchingResults()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SearchAnime("Bebop");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Anime>>(okResult.Value);

            Assert.NotNull(returnValue);
            Assert.Single(returnValue);
            Assert.Equal("Cowboy Bebop", returnValue.First().Title);
        }

        [Fact]
        public async Task SearchAnime_WithEmptyQuery_ReturnsBadRequest()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SearchAnime("");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Search query cannot be empty.", badRequestResult.Value);
        }

        [Fact]
        public async Task SearchAnime_WithWhitespaceQuery_ReturnsBadRequest()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SearchAnime("   ");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Search query cannot be empty.", badRequestResult.Value);
        }

        [Fact]
        public async Task SearchAnime_WithNoMatches_ReturnsEmptyList()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SearchAnime("NonexistentAnime");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Anime>>(okResult.Value);

            Assert.NotNull(returnValue);
            Assert.Empty(returnValue);
        }

        [Fact]
        public async Task SearchAnime_SearchInSynopsis_ReturnsMatchingResults()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SearchAnime("bounty");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Anime>>(okResult.Value);

            Assert.NotNull(returnValue);
            Assert.Single(returnValue);
            Assert.Equal("Cowboy Bebop", returnValue.First().Title);
        }

        [Fact]
        public async Task SearchAnime_WithCaseInsensitiveQuery_ReturnsResults()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            context.Anime.AddRange(sampleData);
            await context.SaveChangesAsync();

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SearchAnime("STEINS");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Anime>>(okResult.Value);

            Assert.NotNull(returnValue);
            Assert.Single(returnValue);
        }

        #endregion

        #region SyncAnimeData Tests

        [Fact]
        public async Task SyncAnimeData_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            
            _mockJikanApiService
                .Setup(x => x.FetchAnimeListAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SyncAnimeData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);

            var returnValue = okResult.Value as dynamic;
            Assert.NotNull(returnValue);
            Assert.Equal(3, (int)returnValue.count);
        }

        [Fact]
        public async Task SyncAnimeData_ClearsOldDataBeforeInsertingNew()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            var oldData = new List<Anime> 
            { 
                new Anime { MalId = 999, Title = "Old Anime", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow } 
            };
            context.Anime.AddRange(oldData);
            await context.SaveChangesAsync();

            var sampleData = AnimeDbContextFixture.GetSampleAnimeData();
            
            _mockJikanApiService
                .Setup(x => x.FetchAnimeListAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(sampleData);

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            await controller.SyncAnimeData();

            // Assert
            var animeCount = await context.Anime.CountAsync();
            Assert.Equal(3, animeCount);
        }

        [Fact]
        public async Task SyncAnimeData_WithEmptyResponse_ReturnsOkWithZeroCount()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            
            _mockJikanApiService
                .Setup(x => x.FetchAnimeListAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Anime>());

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SyncAnimeData();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = okResult.Value as dynamic;
            Assert.Equal(0, returnValue.count);
        }

        [Fact]
        public async Task SyncAnimeData_WithApiError_ReturnServerError()
        {
            // Arrange
            var context = _fixture.CreateDbContext();
            
            _mockJikanApiService
                .Setup(x => x.FetchAnimeListAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException("API Error"));

            var controller = new AnimeController(context, _mockJikanApiService.Object);

            // Act
            var result = await controller.SyncAnimeData();

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        #endregion
    }
}