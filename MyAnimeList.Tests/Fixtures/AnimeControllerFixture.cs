using Microsoft.Extensions.Logging;
using Moq;
using MyAnimeList.Backend.Controllers;
using MyAnimeList.Backend.Models;
using MyAnimeList.Backend.Services;

namespace MyAnimeList.Tests.Fixtures
{
    /// <summary>
    /// Shared fixture for AnimeController tests.
    /// Provides mock services and sample test data.
    /// </summary>
    public class AnimeControllerFixture
    {
        public Mock<IAnimeService> MockAnimeService { get; }
        public Mock<ILogger<AnimeController>> MockLogger { get; }

        public AnimeControllerFixture()
        {
            MockAnimeService = new Mock<IAnimeService>();
            MockLogger = new Mock<ILogger<AnimeController>>();
        }

        /// <summary>
        /// Returns sample anime data for testing.
        /// </summary>
        public static List<Anime> GetSampleAnimeData()
        {
            return new List<Anime>
            {
                new Anime 
                { 
                    Id = 1, 
                    MalId = 1, 
                    Title = "Cowboy Bebop", 
                    Score = 8.75, 
                    Episodes = 26 
                },
                new Anime 
                { 
                    Id = 2, 
                    MalId = 5, 
                    Title = "Fullmetal Alchemist", 
                    Score = 8.26, 
                    Episodes = 51 
                },
                new Anime 
                { 
                    Id = 3, 
                    MalId = 16498, 
                    Title = "Attack on Titan", 
                    Score = 8.52, 
                    Episodes = 25 
                }
            };
        }

        /// <summary>
        /// Resets all mock setups.
        /// Call this before each test to ensure test isolation.
        /// </summary>
        public void ResetMocks()
        {
            MockAnimeService.Reset();
            MockLogger.Reset();
        }
    }
}
