-- Migration: 001_InitialSchema
-- Description: Creates initial database schema with Anime, Users, and UserAnime tables
-- Date: 2026-05-19

-- Create Anime table
CREATE TABLE IF NOT EXISTS "Anime" (
    "Id" SERIAL PRIMARY KEY,
    "MalId" INTEGER NOT NULL UNIQUE,
    "Title" TEXT NOT NULL,
    "EnglishTitle" TEXT,
    "Synopsis" TEXT,
    "Episodes" INTEGER NOT NULL DEFAULT 0,
    "Status" TEXT,
    "AiredFrom" TIMESTAMP,
    "AiredTo" TIMESTAMP,
    "Score" DOUBLE PRECISION,
    "ImageUrl" TEXT,
    "Genre" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "IX_Anime_MalId" ON "Anime" ("MalId");

-- Create Users table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" TEXT NOT NULL UNIQUE,
    "Email" TEXT NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");

-- Create UserAnime table (using MalId from the start)
CREATE TABLE IF NOT EXISTS "UserAnime" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "MalId" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "UserScore" INTEGER,
    "Notes" TEXT,
    "DateAdded" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DateUpdated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_UserAnime_Users_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserAnime_Anime_MalId" FOREIGN KEY ("MalId") 
        REFERENCES "Anime" ("MalId") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_UserAnime_UserId" ON "UserAnime" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserAnime_MalId" ON "UserAnime" ("MalId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAnime_UserId_MalId" ON "UserAnime" ("UserId", "MalId");

-- Create AnimeTitles table
CREATE TABLE IF NOT EXISTS "AnimeTitles" (
    "Id" SERIAL PRIMARY KEY,
    "MalId" INTEGER NOT NULL,
    "Type" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    CONSTRAINT "FK_AnimeTitles_Anime_MalId" FOREIGN KEY ("MalId") 
        REFERENCES "Anime" ("MalId") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AnimeTitles_MalId_Type" ON "AnimeTitles" ("MalId", "Type");

-- Create migration tracking table
CREATE TABLE IF NOT EXISTS "__SqlMigrations" (
    "Id" SERIAL PRIMARY KEY,
    "MigrationName" TEXT NOT NULL UNIQUE,
    "AppliedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
