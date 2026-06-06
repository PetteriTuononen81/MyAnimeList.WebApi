# Migration Refactor Summary

## What Changed

We've refactored from **EF Core Migrations** (C# classes) to **Pure SQL Migrations** (SQL files).

## Why This Is Better

✅ **Direct SQL Control** - Write exactly the SQL you want  
✅ **No Code Generation** - No need to run `dotnet ef migrations add`  
✅ **Easy to Read** - SQL files are simpler than C# migration classes  
✅ **Version Control Friendly** - Clear diffs in Git  
✅ **Flexible** - Easy to write complex migrations  
✅ **Database Agnostic** - Easy to support multiple databases in future  

## New Structure

```
MyAnimeList.Backend/
├── Database/
│   └── Migrations/
│       ├── 001_InitialSchema.sql          ← Initial database setup
│       ├── 002_ExampleFutureMigration.sql.example  ← Template
│       └── README.md                       ← Documentation
├── Services/
│   └── SqlMigrationService.cs              ← Runs migrations automatically
```

## How It Works

1. **On Startup**: `SqlMigrationService` runs automatically
2. **Reads SQL Files**: From `Database/Migrations/` directory
3. **Checks Tracking**: Looks at `sqlmigrations` table
4. **Applies New Ones**: Only runs migrations not yet applied
5. **Records**: Adds entry to `sqlmigrations` table

## Creating New Migrations

### Step 1: Create SQL File

Create `Database/Migrations/002_YourFeature.sql`:

```sql
-- Migration: 002_YourFeature
-- Description: What this migration does
-- Date: 2026-05-19

CREATE TABLE IF NOT EXISTS "YourTable" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL
);
```

### Step 2: That's It!

✅ No `dotnet ef` command needed  
✅ Just restart the app  
✅ Migration runs automatically  

## Testing Locally

```powershell
# Start app
cd MyAnimeList.Backend
dotnet run

# Migrations run automatically on startup
# Check logs to see what happened
```

## Deploying to Server

```sh
# On Linux server
git pull
sudo docker compose down -v  # Fresh start
sudo docker compose up -d    # Migrations run automatically

# Watch logs
sudo docker compose logs -f myanimelist-api
```

## Rollback Strategy

To rollback a migration, create a new migration that reverses it:

```sql
-- Migration: 003_RollbackYourFeature
-- Description: Removes the YourTable table
-- Date: 2026-05-19

DROP TABLE IF EXISTS "YourTable";

-- Remove the original migration from tracking (optional)
DELETE FROM sqlmigrations WHERE migrationname = '002_YourFeature';
```

## Viewing Applied Migrations

```sql
SELECT * FROM sqlmigrations ORDER BY appliedat;
```

## What We Deleted

- ❌ `Migrations/` folder (old EF Core migrations)
- ❌ Need to run `dotnet ef migrations add`

## What We Kept

- ✅ `Database/Migrations/` with pure SQL migration files
- ✅ `SqlMigrationService` to apply migrations automatically on startup
- ✅ Repository pattern with Dapper
- ✅ No Entity Framework Core dependency

## Benefits

1. **Simpler**: Just write SQL, no C# classes
2. **Faster**: No code generation step
3. **Clearer**: Easy to see what SQL runs
4. **Flexible**: Can write any SQL you need
5. **Portable**: SQL files can be used outside the app

## Example: Adding a Column

`Database/Migrations/002_AddAnimeRating.sql`:

```sql
-- Migration: 002_AddAnimeRating
-- Description: Adds rating column to Anime table
-- Date: 2026-05-19

ALTER TABLE "Anime" ADD COLUMN IF NOT EXISTS "Rating" TEXT;
CREATE INDEX IF NOT EXISTS "IX_Anime_Rating" ON "Anime" ("Rating");
```

That's it! No C# code, no migration generation, just SQL. 🎉

## Next Steps

1. ✅ Test locally: `dotnet run`
2. ✅ Commit changes: `git add . && git commit -m "Refactor to SQL migrations"`
3. ✅ Push: `git push`
4. ✅ Deploy: `sudo docker compose down -v && sudo docker compose up -d`

See `Database/Migrations/README.md` for full documentation.
