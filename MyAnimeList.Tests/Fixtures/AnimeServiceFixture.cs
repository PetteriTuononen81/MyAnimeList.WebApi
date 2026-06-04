using Microsoft.Extensions.Logging;
using Moq;
using MyAnimeList.Backend.Database.Repositories;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Services;

namespace MyAnimeList.Tests.Fixtures
{
    /// <summary>
    /// Shared fixture for AnimeService tests.
    /// Provides mock repository, logger, and HTTP client for testing.
    /// </summary>
    public class AnimeServiceFixture : IDisposable
    {
        public Mock<IAnimeRepository> MockRepository { get; }
        public Mock<ILogger<AnimeService>> MockLogger { get; }
        public HttpClient HttpClient { get; }
        public JikanApiClient JikanClient { get; }

        public AnimeServiceFixture()
        {
            MockRepository = new Mock<IAnimeRepository>();
            MockLogger = new Mock<ILogger<AnimeService>>();
            HttpClient = new HttpClient();
            JikanClient = new JikanApiClient(HttpClient);
        }

        /// <summary>
        /// Creates an AnimeService instance with the mocked dependencies.
        /// </summary>
        public AnimeService CreateService()
        {
            return new AnimeService(MockRepository.Object, JikanClient, MockLogger.Object);
        }

        /// <summary>
        /// Returns sample anime data with various scores for ordering tests.
        /// </summary>
        public static List<Anime> GetAnimeWithVariousScores()
        {
            return new List<Anime>
            {
                new Anime { Id = 1, Title = "High Score", Score = 9.5, MalId = 1 },
                new Anime { Id = 2, Title = "No Score A", Score = null, MalId = 2 },
                new Anime { Id = 3, Title = "Medium Score", Score = 7.5, MalId = 3 },
                new Anime { Id = 4, Title = "No Score B", Score = null, MalId = 4 },
                new Anime { Id = 5, Title = "Low Score", Score = 5.0, MalId = 5 }
            };
        }

        /// <summary>
        /// Resets all mock setups.
        /// </summary>
        public void ResetMocks()
        {
            MockRepository.Reset();
            MockLogger.Reset();
        }

        public void Dispose()
        {
            HttpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
