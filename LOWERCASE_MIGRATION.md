# Lowercase Table/Column Names Migration ✅

## Problem Solved

**Before (ugly with double quotes):**
```sql
SELECT ua."Id", ua."UserId", ua."MalId" 
FROM "UserAnime" ua
INNER JOIN "Anime" a ON ua."MalId" = a."MalId"
WHERE ua."UserId" = @UserId
```

**After (clean, no quotes needed):**
```sql
SELECT ua.id, ua.userid, ua.malid 
FROM useranime ua
INNER JOIN anime a ON ua.malid = a.malid
WHERE ua.userid = @UserId
```

---

## What Changed

### ✅ **New Migration Created:**
- **`002_ConvertToLowercaseNames.sql`** - Renames all tables and columns to lowercase
  - Tables: `Anime` → `anime`, `Users` → `users`, `UserAnime` → `useranime`, `AnimeTitles` → `animetitles`
  - All columns renamed to lowercase (e.g., `MalId` → `malid`, `UserId` → `userid`)

### ✅ **All Repositories Updated:**
1. **`LibraryRepository.cs`** - All 6 methods updated with clean SQL
2. **`AnimeRepository.cs`** - All 5 methods updated with clean SQL
3. **`AuthService.cs`** - All 3 SQL queries updated

---

## How PostgreSQL Naming Works

### **Quoted Identifiers (PascalCase):**
```sql
CREATE TABLE "UserAnime" (
    "UserId" INTEGER
);
-- Must ALWAYS quote when using:
SELECT * FROM "UserAnime" WHERE "UserId" = 1;
```

### **Unquoted Identifiers (lowercase):**
```sql
CREATE TABLE useranime (
    userid INTEGER
);
-- No quotes needed:
SELECT * FROM useranime WHERE userid = 1;
```

PostgreSQL **converts unquoted identifiers to lowercase automatically**, so:
- `SELECT * FROM UserAnime` → PostgreSQL looks for `useranime`
- `SELECT * FROM useranime` → Works!
- `SELECT * FROM USERANIME` → Also works! (converted to `useranime`)

---

## Migration Steps

### **First Deployment (Current Database):**
1. Migration `001_InitialSchema.sql` already applied ✅
2. Migration `002_ConvertToLowercaseNames.sql` will run and rename everything
3. All new queries use lowercase (no quotes needed!)

### **Fresh Deployment:**
- Could simplify `001_InitialSchema.sql` to use lowercase from the start
- But keeping it as-is ensures compatibility with existing databases

---

## Example Queries (Before vs After)

### **LibraryRepository - GetUserLibraryAsync:**

**Before:**
```csharp
var sql = @"
    SELECT ua.""Id"", ua.""UserId"", ua.""MalId""
    FROM ""UserAnime"" ua
    INNER JOIN ""Anime"" a ON ua.""MalId"" = a.""MalId""
    WHERE ua.""UserId"" = @UserId";
```

**After:**
```csharp
var sql = @"
    SELECT ua.id, ua.userid, ua.malid
    FROM useranime ua
    INNER JOIN anime a ON ua.malid = a.malid
    WHERE ua.userid = @UserId";
```

### **AnimeRepository - AddAsync (UPSERT):**

**Before:**
```csharp
var sql = @"
    INSERT INTO ""Anime"" (""MalId"", ""Title"")
    VALUES (@MalId, @Title)
    ON CONFLICT (""MalId"") DO UPDATE SET
        ""Title"" = EXCLUDED.""Title""";
```

**After:**
```csharp
var sql = @"
    INSERT INTO anime (malid, title)
    VALUES (@MalId, @Title)
    ON CONFLICT (malid) DO UPDATE SET
        title = EXCLUDED.title";
```

---

## Testing

### **Run Tests:**
```sh
cd MyAnimeList.Tests
dotnet test --filter "FullyQualifiedName~MigrationFileTests"
```

### **Deploy to Server:**
```sh
git add .
git commit -m "Convert to lowercase table/column names for cleaner SQL"
git push

# On server:
git pull
sudo docker compose down -v  # Fresh start
sudo docker compose up -d
```

The `002_ConvertToLowercaseNames.sql` migration will automatically rename everything on first run!

---

## Benefits

1. ✅ **Cleaner SQL** - No more double quotes everywhere
2. ✅ **Easier to Read** - `FROM anime` vs `FROM "Anime"`
3. ✅ **Less Typing** - Shorter queries
4. ✅ **Standard PostgreSQL Convention** - Most PostgreSQL users prefer lowercase
5. ✅ **Case-Insensitive** - Can write `SELECT * FROM ANIME` or `anime` (both work!)

---

## Verification Checklist

- [x] Migration 002 created
- [x] All LibraryRepository queries updated
- [x] All AnimeRepository queries updated
- [x] All AuthService queries updated
- [x] Build succeeds
- [ ] Test locally (will auto-apply migration)
- [ ] Deploy to server
- [ ] Verify API endpoints work

---

## 🎉 Summary

Your SQL queries are now **clean and readable**! No more ugly `""` double quotes everywhere. PostgreSQL naming conventions followed. 🚀
