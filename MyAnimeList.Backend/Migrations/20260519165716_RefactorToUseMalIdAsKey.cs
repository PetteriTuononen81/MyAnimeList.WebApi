using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyAnimeList.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToUseMalIdAsKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAnime_Anime_AnimeId",
                table: "UserAnime");

            migrationBuilder.DropIndex(
                name: "IX_UserAnime_AnimeId",
                table: "UserAnime");

            migrationBuilder.DropIndex(
                name: "IX_UserAnime_UserId_AnimeId",
                table: "UserAnime");

            migrationBuilder.DropTable(
                name: "Titles");

            // Add new MalId column to UserAnime (nullable temporarily)
            migrationBuilder.AddColumn<int>(
                name: "MalId",
                table: "UserAnime",
                type: "integer",
                nullable: true);

            // Populate MalId from Anime table using the existing AnimeId
            migrationBuilder.Sql(@"
                UPDATE ""UserAnime"" ua
                SET ""MalId"" = a.""MalId""
                FROM ""Anime"" a
                WHERE ua.""AnimeId"" = a.""Id""
            ");

            // Make MalId NOT NULL now that it's populated
            migrationBuilder.AlterColumn<int>(
                name: "MalId",
                table: "UserAnime",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Drop the old AnimeId column (this will also drop any remaining indexes on it)
            migrationBuilder.DropColumn(
                name: "AnimeId",
                table: "UserAnime");

            // Add unique constraint on Anime.MalId
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Anime_MalId",
                table: "Anime",
                column: "MalId");

            // Create new AnimeTitles table
            migrationBuilder.CreateTable(
                name: "AnimeTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MalId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimeTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnimeTitles_Anime_MalId",
                        column: x => x.MalId,
                        principalTable: "Anime",
                        principalColumn: "MalId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_UserAnime_MalId",
                table: "UserAnime",
                column: "MalId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnime_UserId_MalId",
                table: "UserAnime",
                columns: new[] { "UserId", "MalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnimeTitles_MalId_Type",
                table: "AnimeTitles",
                columns: new[] { "MalId", "Type" });

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_UserAnime_Anime_MalId",
                table: "UserAnime",
                column: "MalId",
                principalTable: "Anime",
                principalColumn: "MalId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAnime_Anime_MalId",
                table: "UserAnime");

            migrationBuilder.DropTable(
                name: "AnimeTitles");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Anime_MalId",
                table: "Anime");

            migrationBuilder.DropIndex(
                name: "IX_UserAnime_MalId",
                table: "UserAnime");

            migrationBuilder.DropIndex(
                name: "IX_UserAnime_UserId_MalId",
                table: "UserAnime");

            // Add AnimeId column back (nullable temporarily)
            migrationBuilder.AddColumn<int>(
                name: "AnimeId",
                table: "UserAnime",
                type: "integer",
                nullable: true);

            // Restore AnimeId from Anime table using MalId
            migrationBuilder.Sql(@"
                UPDATE ""UserAnime"" ua
                SET ""AnimeId"" = a.""Id""
                FROM ""Anime"" a
                WHERE ua.""MalId"" = a.""MalId""
            ");

            // Make AnimeId NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "AnimeId",
                table: "UserAnime",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Drop MalId column
            migrationBuilder.DropColumn(
                name: "MalId",
                table: "UserAnime");

            // Recreate old Titles table
            migrationBuilder.CreateTable(
                name: "Titles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnimeId = table.Column<int>(type: "integer", nullable: false),
                    TitleText = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Titles_Anime_AnimeId",
                        column: x => x.AnimeId,
                        principalTable: "Anime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_UserAnime_AnimeId",
                table: "UserAnime",
                column: "AnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnime_UserId_AnimeId",
                table: "UserAnime",
                columns: new[] { "UserId", "AnimeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Titles_AnimeId_Type",
                table: "Titles",
                columns: new[] { "AnimeId", "Type" });

            // Add foreign key back
            migrationBuilder.AddForeignKey(
                name: "FK_UserAnime_Anime_AnimeId",
                table: "UserAnime",
                column: "AnimeId",
                principalTable: "Anime",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
