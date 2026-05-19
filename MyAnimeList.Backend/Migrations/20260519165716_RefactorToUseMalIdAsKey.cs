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

            migrationBuilder.DropTable(
                name: "Titles");

            migrationBuilder.RenameColumn(
                name: "AnimeId",
                table: "UserAnime",
                newName: "MalId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAnime_UserId_AnimeId",
                table: "UserAnime",
                newName: "IX_UserAnime_UserId_MalId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAnime_AnimeId",
                table: "UserAnime",
                newName: "IX_UserAnime_MalId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Anime_MalId",
                table: "Anime",
                column: "MalId");

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

            migrationBuilder.CreateIndex(
                name: "IX_AnimeTitles_MalId_Type",
                table: "AnimeTitles",
                columns: new[] { "MalId", "Type" });

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

            migrationBuilder.RenameColumn(
                name: "MalId",
                table: "UserAnime",
                newName: "AnimeId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAnime_UserId_MalId",
                table: "UserAnime",
                newName: "IX_UserAnime_UserId_AnimeId");

            migrationBuilder.RenameIndex(
                name: "IX_UserAnime_MalId",
                table: "UserAnime",
                newName: "IX_UserAnime_AnimeId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Titles_AnimeId_Type",
                table: "Titles",
                columns: new[] { "AnimeId", "Type" });

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
