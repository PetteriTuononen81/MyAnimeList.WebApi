# Jikan API v4 Titles Update

## Overview
This update implements support for the new Jikan API v4 titles structure, which provides a more flexible way to handle multiple title types for anime.

## What Changed

### Old Structure (Deprecated)
```json
{
  "title": "Kenja no Deshi wo Nanoru Kenja",
  "title_english": "She Professed Herself Pupil of the Wise Man",
  "title_japanese": "賢者の弟子を名乗る賢者",
  "title_synonyms": ["Alternative Title 1", "Alternative Title 2"]
}
```

### New Structure
```json
{
  "titles": [
    {
      "type": "Default",
      "title": "Kenja no Deshi wo Nanoru Kenja"
    },
    {
      "type": "Japanese",
      "title": "賢者の弟子を名乗る賢者"
    },
    {
      "type": "English",
      "title": "She Professed Herself Pupil of the Wise Man"
    },
    {
      "type": "Synonym",
      "title": "Alternative Title"
    }
  ]
}
```

## Database Changes

### New Table: `Titles`
- `Id` (Primary Key)
- `Type` (Required) - e.g., "Default", "English", "Japanese", "Synonym"
- `TitleText` (Required) - The actual title text
- `AnimeId` (Foreign Key) - References the Anime table

### Migration
Run the following command to apply the database migration:
```bash
cd MyAnimeList.Backend
dotnet ef database update
```

## Code Changes

### 1. New Model: `Title.cs`
- Created a new `Title` entity to represent individual titles
- Includes a relationship to `Anime` entity

### 2. Updated `Anime.cs`
- Added `Titles` collection property
- Added helper methods:
  - `GetTitleByType(string type)` - Get a specific title by type
  - `GetDefaultTitle()` - Get the default title
  - `GetEnglishTitleFromCollection()` - Get the English title
  - `GetJapaneseTitle()` - Get the Japanese title
  - `GetSynonymTitles()` - Get all synonym titles as a list

### 3. Updated `JikanApiClient.cs`
- Updated `ParseAnimeFromJson()` to parse the new titles array
- Populates both legacy properties (`Title`, `EnglishTitle`) and the new `Titles` collection
- Ensures backward compatibility

### 4. Updated `AnimeDbContext.cs`
- Added `DbSet<Title>` for the Titles table
- Configured the relationship between Anime and Titles
- Added cascade delete behavior

### 5. Updated DTOs
- Added `TitleDto` class
- Updated `AnimeDto` to include optional `Titles` collection
- Maintained backward compatibility with existing `Title` and `EnglishTitle` properties

### 6. Updated `LibraryService.cs` & `LibraryRepository.cs`
- Updated mapping to include titles in the response
- Modified queries to eagerly load the Titles collection

### 7. Added Helper Method in `JsonPropertyHelper.cs`
- `GetTitlesArray()` - Extracts and parses the titles array from JSON

## API Response Example

When fetching anime from the library, the response now includes the titles array:

```json
{
  "id": 1,
  "userId": 123,
  "animeId": 456,
  "status": "Watching",
  "anime": {
    "id": 456,
    "malId": 50348,
    "title": "Kenja no Deshi wo Nanoru Kenja",
    "englishTitle": "She Professed Herself Pupil of the Wise Man",
    "titles": [
      {
        "type": "Default",
        "title": "Kenja no Deshi wo Nanoru Kenja"
      },
      {
        "type": "Japanese",
        "title": "賢者の弟子を名乗る賢者"
      },
      {
        "type": "English",
        "title": "She Professed Herself Pupil of the Wise Man"
      }
    ]
  }
}
```

## Backward Compatibility

The implementation maintains full backward compatibility:
- Legacy `title`, `title_english`, `title_japanese`, and `title_synonyms` properties are still supported
- Existing `Title` and `EnglishTitle` properties on the `Anime` model are preserved
- The new titles array is optional in DTOs, so existing clients won't break

## Usage Examples

### Get a specific title type
```csharp
var anime = await _animeRepository.GetByIdAsync(animeId);
var englishTitle = anime.GetTitleByType("English");
var japaneseTitle = anime.GetJapaneseTitle();
```

### Get all synonym titles
```csharp
var synonyms = anime.GetSynonymTitles();
foreach (var synonym in synonyms)
{
    Console.WriteLine($"Synonym: {synonym}");
}
```

### Search by any title type
The search functionality automatically searches through the legacy `Title` and `EnglishTitle` properties. Future enhancements could extend this to search through all title types.

## Next Steps

1. **Apply the migration**: Run `dotnet ef database update` in the MyAnimeList.Backend directory
2. **Re-sync anime data**: Run the sync endpoint to populate the new Titles table
3. **Update frontend**: Update your frontend application to display titles from the new structure
4. **Consider deprecating legacy properties**: In a future version, you might want to remove the legacy `EnglishTitle` property once all clients are updated

## Testing

After applying the migration, test the following:
1. Sync anime data from Jikan API
2. Verify titles are stored in the database
3. Check library endpoints return titles correctly
4. Test search functionality still works
5. Verify backward compatibility with existing clients
