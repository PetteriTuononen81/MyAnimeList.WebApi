# Test Jikan API Sync
# This script helps you test the anime sync and verify the new titles structure

Write-Host "=== Jikan API Sync Test ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Call Jikan API directly to see raw response
Write-Host "Test 1: Fetching raw data from Jikan API..." -ForegroundColor Yellow
try {
    $jikanResponse = Invoke-RestMethod -Uri "https://api.jikan.moe/v4/anime?page=1&limit=1&order_by=score&sort=desc" -Method Get

    Write-Host "✓ Successfully fetched from Jikan API" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sample Anime Title Structure:" -ForegroundColor Cyan

    $firstAnime = $jikanResponse.data[0]
    Write-Host "  MalId: $($firstAnime.mal_id)" -ForegroundColor White
    Write-Host "  Legacy title: $($firstAnime.title)" -ForegroundColor DarkGray
    Write-Host "  Legacy title_english: $($firstAnime.title_english)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  New Titles Array:" -ForegroundColor Cyan
    foreach ($title in $firstAnime.titles) {
        Write-Host "    - Type: $($title.type), Title: $($title.title)" -ForegroundColor White
    }
    Write-Host ""
}
catch {
    Write-Host "✗ Failed to fetch from Jikan API: $_" -ForegroundColor Red
    exit 1
}

# Test 2: Check if your API is running
Write-Host "Test 2: Checking if your API is running..." -ForegroundColor Yellow
$apiUrl = "https://localhost:5001"
$httpUrl = "http://localhost:5000"

$apiRunning = $false
try {
    $null = Invoke-RestMethod -Uri "$apiUrl/swagger/index.html" -Method Get -SkipCertificateCheck -TimeoutSec 3 2>$null
    $apiRunning = $true
    $baseUrl = $apiUrl
    Write-Host "✓ API is running at $apiUrl" -ForegroundColor Green
}
catch {
    try {
        $null = Invoke-WebRequest -Uri "$httpUrl/swagger/index.html" -Method Get -TimeoutSec 3 -ErrorAction Stop
        $apiRunning = $true
        $baseUrl = $httpUrl
        Write-Host "✓ API is running at $httpUrl" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ API is not running. Please start it first with:" -ForegroundColor Red
        Write-Host "  cd MyAnimeList.Backend" -ForegroundColor White
        Write-Host "  dotnet run" -ForegroundColor White
        Write-Host ""
        Write-Host "Or press F5 in Visual Studio" -ForegroundColor White
        exit 1
    }
}

Write-Host ""

# Test 3: Trigger sync with just 1 page
Write-Host "Test 3: Triggering sync (1 page only for testing)..." -ForegroundColor Yellow
try {
    $syncResponse = Invoke-RestMethod -Uri "$baseUrl/api/anime/sync?maxPages=1" -Method Post -SkipCertificateCheck
    Write-Host "✓ Sync completed successfully!" -ForegroundColor Green
    Write-Host "  Message: $($syncResponse.message)" -ForegroundColor White
    Write-Host "  Count: $($syncResponse.count)" -ForegroundColor White
}
catch {
    Write-Host "✗ Sync failed: $_" -ForegroundColor Red
    Write-Host "  Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Check the database for titles
Write-Host "Test 4: Checking database for anime titles..." -ForegroundColor Yellow
try {
    $animeResponse = Invoke-RestMethod -Uri "$baseUrl/api/anime/paginated?page=1&pageSize=1" -Method Get -SkipCertificateCheck

    if ($animeResponse.data -and $animeResponse.data.Count -gt 0) {
        $firstAnime = $animeResponse.data[0]
        Write-Host "✓ Found anime in database" -ForegroundColor Green
        Write-Host ""
        Write-Host "Sample Anime from Database:" -ForegroundColor Cyan
        Write-Host "  Id: $($firstAnime.id)" -ForegroundColor White
        Write-Host "  MalId: $($firstAnime.malId)" -ForegroundColor White
        Write-Host "  Title: $($firstAnime.title)" -ForegroundColor White
        Write-Host "  EnglishTitle: $($firstAnime.englishTitle)" -ForegroundColor White
        Write-Host "  Score: $($firstAnime.score)" -ForegroundColor White

        # Note: The /api/anime/paginated endpoint returns Anime objects directly, not DTOs with titles
        Write-Host ""
        Write-Host "  Note: To see the titles array, check the AnimeTitles table directly" -ForegroundColor DarkGray
        Write-Host "  or use the library endpoints which include the full titles structure." -ForegroundColor DarkGray
    }
    else {
        Write-Host "✗ No anime found in database" -ForegroundColor Red
    }
}
catch {
    Write-Host "✗ Failed to fetch anime from database: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Check AnimeTitles table in your database to see the titles structure" -ForegroundColor White
Write-Host "2. Query: SELECT * FROM ""AnimeTitles"" LIMIT 10;" -ForegroundColor White
Write-Host "3. Or use Swagger UI at: $baseUrl/swagger" -ForegroundColor White
