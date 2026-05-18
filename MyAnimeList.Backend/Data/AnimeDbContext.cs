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
        public DbSet<User> Users { get; set; }
        public DbSet<UserAnime> UserAnime { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Anime>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.MalId).IsUnique();
                entity.Property(a => a.Title).IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Email).IsRequired();
                entity.Property(u => u.Username).IsRequired();
                entity.Property(u => u.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<UserAnime>(entity =>
            {
                entity.HasKey(ua => ua.Id);
                entity.HasIndex(ua => new { ua.UserId, ua.AnimeId }).IsUnique();

                entity.HasOne(ua => ua.User)
                    .WithMany()
                    .HasForeignKey(ua => ua.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ua => ua.Anime)
                    .WithMany()
                    .HasForeignKey(ua => ua.AnimeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ua => ua.Status)
                    .HasConversion<int>();
            });
        }
    }
}