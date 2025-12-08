using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Data;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Tests.Fixtures
{
    public class AnimeDbContextFixture
    {
        public AnimeDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AnimeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new AnimeDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public static List<Anime> GetSampleAnimeData()
        {
            return new List<Anime>
            {
                new Anime
                {
                    Id = 1,
                    MalId = 5,
                    Title = "Cowboy Bebop",
                    EnglishTitle = "Cowboy Bebop",
                    Synopsis = "A group of bounty hunters travels the galaxy.",
                    Episodes = 26,
                    Status = "Finished Airing",
                    Score = 8.76,
                    ImageUrl = "https://example.com/cowboy-bebop.jpg",
                    Genre = "Action, Adventure, Sci-Fi",
                    AiredFrom = new DateTime(1998, 4, 3),
                    AiredTo = new DateTime(1999, 4, 24),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Anime
                {
                    Id = 2,
                    MalId = 9253,
                    Title = "Steins;Gate",
                    EnglishTitle = "Steins;Gate",
                    Synopsis = "A group of friends discover a way to send messages back in time.",
                    Episodes = 24,
                    Status = "Finished Airing",
                    Score = 9.09,
                    ImageUrl = "https://example.com/steins-gate.jpg",
                    Genre = "Sci-Fi, Thriller",
                    AiredFrom = new DateTime(2011, 4, 9),
                    AiredTo = new DateTime(2011, 9, 14),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Anime
                {
                    Id = 3,
                    MalId = 11757,
                    Title = "Attack on Titan",
                    EnglishTitle = "Attack on Titan",
                    Synopsis = "Humanity fights back against the titans.",
                    Episodes = 139,
                    Status = "Finished Airing",
                    Score = 8.52,
                    ImageUrl = "https://example.com/attack-on-titan.jpg",
                    Genre = "Action, Adventure, Dark Fantasy",
                    AiredFrom = new DateTime(2013, 4, 7),
                    AiredTo = new DateTime(2023, 10, 4),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }
    }
}