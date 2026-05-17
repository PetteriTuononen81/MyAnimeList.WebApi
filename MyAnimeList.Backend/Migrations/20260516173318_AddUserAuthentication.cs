using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyAnimeList.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Anime table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Anime"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""MalId"" INTEGER NOT NULL,
                    ""Title"" TEXT NOT NULL,
                    ""EnglishTitle"" TEXT,
                    ""Synopsis"" TEXT,
                    ""Episodes"" INTEGER NOT NULL,
                    ""Status"" TEXT,
                    ""AiredFrom"" TIMESTAMP WITH TIME ZONE,
                    ""AiredTo"" TIMESTAMP WITH TIME ZONE,
                    ""Score"" DOUBLE PRECISION,
                    ""ImageUrl"" TEXT,
                    ""Genre"" TEXT,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""UpdatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL
                );
            ");

            // Create Users table only if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Email"" TEXT NOT NULL,
                    ""Username"" TEXT NOT NULL,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL
                );
            ");

            // Create indexes only if they don't exist
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Anime_MalId"" ON ""Anime"" (""MalId"");
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Users"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Anime"";");
        }
    }
}
