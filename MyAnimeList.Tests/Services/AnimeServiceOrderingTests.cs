using Moq;
using MyAnimeList.Backend.Models;
using MyAnimeList.Tests.Fixtures;
using Xunit;

namespace MyAnimeList.Tests.Services
{
    public class AnimeServiceOrderingTests : IClassFixture<AnimeServiceFixture>
    {
        private readonly AnimeServiceFixture _fixture;

        public AnimeServiceOrderingTests(AnimeServiceFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetAnimePaginatedAsync_NullScores_ShouldAppearLast()
        {
            // Arrange
            _fixture.ResetMocks();

            var testData = AnimeServiceFixture.GetAnimeWithVariousScores();
            _fixture.MockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(testData);

            var service = _fixture.CreateService();

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