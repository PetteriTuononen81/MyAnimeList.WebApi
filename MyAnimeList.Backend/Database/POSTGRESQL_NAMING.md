# PostgreSQL Naming Convention

## Important: PostgreSQL Converts Identifiers to Lowercase!

### ⚠️ **Key Concept**

PostgreSQL **automatically converts all unquoted identifiers to lowercase**.

---

## What This Means

### If you write this in SQL:
```sql
CREATE TABLE UserAnime (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER,
    MalId INTEGER
);
```

### PostgreSQL actually creates:
```sql
-- Stored internally as:
useranime (
    id SERIAL PRIMARY KEY,
    userid INTEGER,
    malid INTEGER
)
```

All table and column names are **automatically lowercased**!

---

## The Two Options

### Option 1: Use Quotes (Preserve PascalCase)
```sql
CREATE TABLE "UserAnime" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER,
    "MalId" INTEGER
);
```
✅ Preserves exact casing  
❌ Requires quotes in **every query forever**  
❌ SQL becomes messy: `SELECT "Id" FROM "UserAnime"`

### Option 2: Accept Lowercase (No Quotes)
```sql
CREATE TABLE useranime (
    id SERIAL PRIMARY KEY,
    userid INTEGER,
    malid INTEGER
);
```
✅ Clean SQL: `SELECT id FROM useranime`  
✅ Standard PostgreSQL convention  
✅ No quotes needed anywhere  
✅ **Dapper automatically maps to PascalCase C# models**

---

## Our Project Uses Lowercase (Option 2)

### SQL Migration (lowercase):
```sql
CREATE TABLE useranime (
    id SERIAL PRIMARY KEY,
    userid INTEGER,
    malid INTEGER
);
```

### C# Model (PascalCase):
```csharp
public class UserAnime
{
    public int Id { get; set; }        // Maps to: id
    public int UserId { get; set; }    // Maps to: userid
    public int MalId { get; set; }     // Maps to: malid
}
```

### C# Query (lowercase in SQL, but models stay PascalCase):
```csharp
var sql = "SELECT id, userid, malid FROM useranime WHERE userid = @UserId";
var result = await connection.QueryAsync<UserAnime>(sql, new { UserId = 123 });
// Dapper automatically maps: id → Id, userid → UserId, malid → MalId
```

---

## Dapper Mapping is Case-Insensitive

Dapper automatically handles the mapping:

| Database Column (lowercase) | C# Property (PascalCase) | Dapper Maps? |
|-----------------------------|--------------------------|--------------|
| `id`                        | `Id`                     | ✅ Yes       |
| `userid`                    | `UserId`                 | ✅ Yes       |
| `malid`                     | `MalId`                  | ✅ Yes       |
| `createdat`                 | `CreatedAt`              | ✅ Yes       |

**You get clean SQL AND clean C# code!** 🎉

---

## Benefits of Lowercase

1. ✅ **No quotes needed** - `FROM useranime` vs `FROM "UserAnime"`
2. ✅ **Cleaner SQL** - Much easier to read and write
3. ✅ **Standard PostgreSQL** - What 90% of PostgreSQL users do
4. ✅ **C# models stay PascalCase** - No changes needed!
5. ✅ **Case-insensitive** - Can write `USERANIME`, `useranime`, or `UserAnime` in queries (all work!)

---

## Migration Strategy

Our migrations create tables with lowercase names from the start:

```sql
-- Migration: 001_InitialSchema.sql
CREATE TABLE IF NOT EXISTS anime (
    id SERIAL PRIMARY KEY,
    malid INTEGER NOT NULL UNIQUE,
    title TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS useranime (
    id SERIAL PRIMARY KEY,
    userid INTEGER NOT NULL,
    malid INTEGER NOT NULL,
    status INTEGER NOT NULL
);
```

**Result:** Clean SQL everywhere, C# models unchanged! 🚀

---

## References

- [PostgreSQL Documentation: Identifiers and Key Words](https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS)
- [Dapper Documentation](https://github.com/DapperLib/Dapper)

---

**TL;DR:** PostgreSQL converts everything to lowercase unless you use quotes. We use lowercase for clean SQL, and Dapper automatically maps to PascalCase C# properties. Best of both worlds! ✨
