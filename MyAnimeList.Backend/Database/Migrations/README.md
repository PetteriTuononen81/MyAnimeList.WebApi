# SQL Migrations

This directory contains pure SQL migration files for the MyAnimeList database.

## How It Works

1. **Automatic Application**: Migrations run automatically on application startup
2. **Tracking**: Applied migrations are tracked in the `__SqlMigrations` table
3. **Order**: Migrations are applied in alphabetical/numerical order
4. **Idempotent**: Migrations are only applied once (tracked by filename)

## Naming Convention

```
XXX_DescriptiveName.sql
```

- `XXX` = 3-digit sequential number (001, 002, 003, etc.)
- `DescriptiveName` = Clear description of what the migration does
- Always use `.sql` extension

## Examples

- `001_InitialSchema.sql` - Creates initial tables
- `002_AddUserProfileTable.sql` - Adds new user profile feature
- `003_AlterAnimeAddRating.sql` - Adds rating column to Anime table

## Creating a New Migration

### Example: Adding a new column

Create `Database/Migrations/002_AddAnimeRatingColumn.sql`:

```sql
-- Migration: 002_AddAnimeRatingColumn
-- Description: Adds a rating column to the Anime table
-- Date: 2026-05-19

ALTER TABLE "Anime" ADD COLUMN IF NOT EXISTS "Rating" TEXT;
CREATE INDEX IF NOT EXISTS "IX_Anime_Rating" ON "Anime" ("Rating");
```

### Example: Creating a new table

Create `Database/Migrations/003_CreateReviewsTable.sql`:

```sql
-- Migration: 003_CreateReviewsTable
-- Description: Creates table for user anime reviews
-- Date: 2026-05-19

CREATE TABLE IF NOT EXISTS "Reviews" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "MalId" INTEGER NOT NULL,
    "ReviewText" TEXT NOT NULL,
    "Rating" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Reviews_Users_UserId" FOREIGN KEY ("UserId") 
        REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Reviews_Anime_MalId" FOREIGN KEY ("MalId") 
        REFERENCES "Anime" ("MalId") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Reviews_UserId" ON "Reviews" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Reviews_MalId" ON "Reviews" ("MalId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Reviews_UserId_MalId" ON "Reviews" ("UserId", "MalId");
```

## Best Practices

1. **Use IF NOT EXISTS**: Always use `IF NOT EXISTS` for tables, indexes, and constraints
2. **Use IF EXISTS**: Always use `IF EXISTS` for DROP statements
3. **Add Comments**: Include migration name, description, and date at the top
4. **Test Locally**: Test migration on local database before deploying
5. **Use Transactions**: Complex migrations should use transactions
6. **Idempotent**: Write migrations so they can be run multiple times safely

## Viewing Applied Migrations

```sql
SELECT * FROM "__SqlMigrations" ORDER BY "AppliedAt";
```

## Manual Migration Application

If you need to apply migrations manually:

```sh
# Connect to database
docker exec -it myanimelist-postgres psql -U postgres -d myanimelist

# Run migration
\i /path/to/migration.sql

# Verify
SELECT * FROM "__SqlMigrations";
```

## Rollback

To rollback a migration, you need to:

1. Write a new migration that reverses the changes
2. Or manually execute SQL to undo changes
3. Delete the entry from `__SqlMigrations` table

Example rollback migration: `004_RollbackRatingColumn.sql`:

```sql
-- Migration: 004_RollbackRatingColumn
-- Description: Removes the rating column from Anime table
-- Date: 2026-05-19

ALTER TABLE "Anime" DROP COLUMN IF EXISTS "Rating";
DROP INDEX IF EXISTS "IX_Anime_Rating";

-- Remove the original migration from tracking
DELETE FROM "__SqlMigrations" WHERE "MigrationName" = '002_AddAnimeRatingColumn';
```

## Troubleshooting

### Migration Not Running
- Check SQL syntax
- Check logs: `docker compose logs -f myanimelist-api`
- Verify file is in `Database/Migrations/` directory
- Ensure filename follows naming convention

### Migration Already Applied
- Check `__SqlMigrations` table
- If you want to re-run, delete the entry: `DELETE FROM "__SqlMigrations" WHERE "MigrationName" = 'XXX_Name';`

### Fresh Start
```sh
sudo docker compose down -v  # Deletes all data
sudo docker compose up -d     # Migrations run automatically
```
