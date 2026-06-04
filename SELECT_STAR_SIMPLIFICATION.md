# SELECT * Usage - Simplified Queries ✅

## What Changed

Simplified all queries to use `SELECT *` instead of listing every column manually.

---

## Before (Tedious) ❌

```csharp
var sql = @"
    SELECT id, malid, title, englishtitle, japanesetitle, 
           imageurl, synopsis, type, episodes, status, 
           score, popularity, rank, startdate, enddate
    FROM anime
    WHERE malid = @MalId";
```

**Problem:** 
- Must type every column name
- Easy to forget columns
- Hard to maintain when adding new columns

---

## After (Clean) ✅

```csharp
var sql = "SELECT * FROM anime WHERE malid = @MalId";
```

**Benefits:**
- ✅ Much shorter and cleaner
- ✅ Automatically includes all columns
- ✅ Adding new columns to table? Queries automatically include them!
- ✅ Dapper maps everything to your C# model properties

---

## Updated Queries

### **AnimeRepository:**
```csharp
// GetAllAsync
SELECT * FROM anime ORDER BY score DESC NULLS LAST

// GetByMalIdAsync
SELECT * FROM anime WHERE malid = @MalId
```

### **LibraryRepository:**
```csharp
// GetUserLibraryAsync & GetUserAnimeAsync
SELECT 
    ua.*,                    -- All useranime columns
    a.id AS animeid,         -- Anime columns with aliases (for Dapper mapping)
    a.malid AS animemalid,
    a.title, a.englishtitle, ...
FROM useranime ua
INNER JOIN anime a ON ua.malid = a.malid
LEFT JOIN animetitles t ON a.malid = t.malid
```

### **AuthService:**
```csharp
// GetUserByEmailAsync
SELECT * FROM users WHERE email = @Email

// GetUserByUsernameAsync
SELECT * FROM users WHERE username = @Username
```

---

## When to Use SELECT *

### ✅ **Use SELECT * when:**
1. You need all columns from a table
2. The table is small/medium (not hundreds of columns)
3. You're mapping to a C# model that has all properties

### ⚠️ **Be careful with SELECT * when:**
1. Joining multiple tables (use aliases like `ua.*`, `a.*`)
2. Very large tables with many columns you don't need
3. Performance-critical queries (but measure first - often not a problem!)

---

## How Dapper Handles SELECT *

```csharp
// Database columns (lowercase):
id, malid, title, score, ...

// C# Model (PascalCase):
public class Anime
{
    public int Id { get; set; }
    public int MalId { get; set; }
    public string Title { get; set; }
    public double? Score { get; set; }
}

// Dapper automatically maps:
id → Id
malid → MalId  
title → Title
score → Score
```

**Dapper is case-insensitive and matches by name!** ✨

---

## Result

**Much cleaner, more maintainable SQL queries!**

- Before: ~15 lines of column names
- After: 1-2 lines with `SELECT *`

✅ **Build successful**  
✅ **All functionality unchanged**  
✅ **Queries much easier to read and maintain**

---

## Example: Adding a New Column

**Scenario:** You add a `genre` column to the `anime` table.

### Before (with explicit columns):
```sql
-- Migration:
ALTER TABLE anime ADD COLUMN genre TEXT;

-- Problem: Must update every query!
SELECT id, malid, title, englishtitle, ..., genre  -- ← Add here
FROM anime
```

### After (with SELECT *):
```sql
-- Migration:
ALTER TABLE anime ADD COLUMN genre TEXT;

-- Queries automatically include it!
SELECT * FROM anime  -- ✅ Already includes genre!
```

Just add the property to your C# model:
```csharp
public class Anime
{
    // ... existing properties
    public string? Genre { get; set; }  // ← Add here, done!
}
```

**That's it! No query changes needed!** 🎉

---

## Performance Note

**"But isn't SELECT * slower?"**

In practice, **rarely matters** for applications like this:
- PostgreSQL is very fast
- Network overhead usually dominates
- Dapper only maps columns that exist in your model

**Premature optimization is the root of all evil!**  
If you have performance issues later, **measure first**, then optimize specific queries.

For now: **Clean, maintainable code > micro-optimizations** ✅
