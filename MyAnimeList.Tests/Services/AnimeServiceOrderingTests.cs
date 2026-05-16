using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Repositories;
using MyAnimeList.Backend.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MyAnimeList.Tests.Services
{
    public class AnimeServiceOrderingTests
    {
        [Fact]
        public async Task GetAnimePaginatedAsync_NullScores_ShouldAppearLast()
        {
            // Arrange
            var mockRepo = new Mock<IAnimeRepository>();
            var httpClient = new HttpClient();
            var jikanClient = new JikanApiClient(httpClient);
            var mockLogger = new Mock<ILogger<AnimeService>>();

            var testData = new List<Anime>
            {
                new Anime { Id = 1, Title = "High Score", Score = 9.5, MalId = 1 },
                new Anime { Id = 2, Title = "No Score A", Score = null, MalId = 2 },
                new Anime { Id = 3, Title = "Medium Score", Score = 7.5, MalId = 3 },
                new Anime { Id = 4, Title = "No Score B", Score = null, MalId = 4 },
                new Anime { Id = 5, Title = "Low Score", Score = 5.0, MalId = 5 }
            };

            mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(testData);

            var service = new AnimeService(mockRepo.Object, jikanClient, mockLogger.Object);

            // Act
            var (result, totalCount) = await service.GetAnimePaginatedAsync(page: 1, pageSize: 10);

            // Assert
            Assert.Equal(5, totalCount);
            Assert.Equal(5, result.Count);

            // Check ordering: scored anime first (descending), then null scores (alphabetically)
            Assert.Equal("High Score", result[0].Title);
            Assert.Equal(9.5, result[0].Score);

            Assert.Equal("Medium Score", result[1].Title);
            Assert.Equal(7.5, result[1].Score);

            Assert.Equal("Low Score", result[2].Title);
            Assert.Equal(5.0, result[2].Score);

            Assert.Equal("No Score A", result[3].Title);
            Assert.Null(result[3].Score);

            Assert.Equal("No Score B", result[4].Title);
            Assert.Null(result[4].Score);
        }
    }
}