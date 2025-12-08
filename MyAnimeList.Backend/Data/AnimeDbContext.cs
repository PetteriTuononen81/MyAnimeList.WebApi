using Microsoft.EntityFrameworkCore;
using MyAnimeList.Backend.Models;

namespace MyAnimeList.Backend.Data
{
    public class AnimeDbContext : DbContext
    {
        public AnimeDbContext(DbContextOptions<AnimeDbContext> options) : base(options)
        {
        }

        public DbSet<Anime> Anime { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Anime>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<Anime>()
                .HasIndex(a => a.MalId)
                .IsUnique();

            modelBuilder.Entity<Anime>()
                .Property(a => a.Title)
                .IsRequired();
        }
    }
}