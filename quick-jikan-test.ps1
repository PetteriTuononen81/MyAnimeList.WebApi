# Quick test to see Jikan API raw response
Write-Host "Fetching sample anime from Jikan API..." -ForegroundColor Cyan

$response = Invoke-RestMethod -Uri "https://api.jikan.moe/v4/anime?page=1&limit=2&order_by=score&sort=desc"

foreach ($anime in $response.data) {
    Write-Host ""
    Write-Host "===========================================" -ForegroundColor Yellow
    Write-Host "MalId: $($anime.mal_id)" -ForegroundColor White
    Write-Host ""
    Write-Host "Legacy Properties (deprecated):" -ForegroundColor DarkGray
    Write-Host "  title: $($anime.title)"
    Write-Host "  title_english: $($anime.title_english)"
    Write-Host "  title_japanese: $($anime.title_japanese)"
    Write-Host ""
    Write-Host "New Titles Array:" -ForegroundColor Green
    foreach ($title in $anime.titles) {
        Write-Host "  [$($title.type)]: $($title.title)" -ForegroundColor Cyan
    }
    Write-Host ""
}

Write-Host "===========================================" -ForegroundColor Yellow
