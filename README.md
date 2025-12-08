# MyAnimeList API

A .NET 10 ASP.NET Core API that fetches and manages anime data from the Jikan API, stores it in PostgreSQL, and automatically syncs data monthly.

## Features

- ?? **Jikan API Integration** - Fetches top-rated anime data
- ??? **PostgreSQL Database** - Persistent data storage
- ?? **Monthly Auto-Sync** - Automated scheduled anime data updates via cron job
- ?? **Docker Support** - Easy deployment with Docker Compose
- ?? **Search & Pagination** - Search anime by title/synopsis with paginated results
- ? **Comprehensive Tests** - Full test coverage with xUnit and Moq

## API Endpoints

### Get All Anime
```
GET /api/anime?page=1&pageSize=20
```
Returns paginated list of anime with pagination metadata.

### Get Anime by ID
```
GET /api/anime/{id}
```
Returns a specific anime by ID.

### Search Anime
```
GET /api/anime/search?query=cowboy
```
Searches anime by title or synopsis (case-insensitive).

### Sync Anime Data
```
POST /api/anime/sync
```
Manually triggers anime data synchronization from Jikan API. Called monthly by cron job.

## Technology Stack

- **.NET 10** - Latest long-term support framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **PostgreSQL** - Database
- **Docker & Docker Compose** - Containerization
- **xUnit** - Testing framework
- **Moq** - Mocking library

## Project Structure

```
MyAnimeList.WebApi/
??? MyAnimeList.Backend/
?   ??? Controllers/
?   ?   ??? AnimeController.cs
?   ??? Services/
?   ?   ??? JikanApiService.cs
?   ?   ??? DatabaseInitializationService.cs
?   ??? Models/
?   ?   ??? Anime.cs
?   ?   ??? Dtos/
?   ?       ??? AnimeListResponseDto.cs
?   ?       ??? PaginationDto.cs
?   ??? Data/
?   ?   ??? AnimeDbContext.cs
?   ??? Program.cs
?   ??? appsettings.json
?   ??? appsettings.Production.json
?   ??? Dockerfile
??? MyAnimeList.Tests/
?   ??? Controllers/
?   ?   ??? AnimeControllerTests.cs
?   ??? Fixtures/
?       ??? AnimeDbContextFixture.cs
??? docker-compose.yml
??? scripts/
?   ??? monthly-sync.sh
??? README.md
```

## Quick Start

### Prerequisites

- Docker and Docker Compose (for deployment)
- .NET 10 SDK (for development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/MyAnimeList.WebApi.git
   cd MyAnimeList.WebApi
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

3. **Run tests**
   ```bash
   dotnet test
   ```

4. **Run the application**
   ```bash
   dotnet run --project MyAnimeList.Backend
   ```

   The API will be available at `http://localhost:5000`

### Docker Deployment

1. **Update configuration**
   - Edit `docker-compose.yml` and change `your_secure_password` to a strong password
   - Edit `MyAnimeList.Backend/appsettings.Production.json` with the same password

2. **Deploy**
   ```bash
   docker compose up -d
   ```

3. **Verify**
   ```bash
   docker compose ps
   curl http://localhost:5000/api/anime
   ```

4. **Set up monthly sync**
   ```bash
   sudo cp scripts/monthly-sync.sh /usr/local/bin/myanimelist-sync
   sudo chmod +x /usr/local/bin/myanimelist-sync
   sudo crontab -e
   # Add: 0 2 1 * * /usr/local/bin/myanimelist-sync
   ```

## Configuration

### appsettings.json (Development)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=anime;Username=animeuser;Password=your_password;"
  }
}
```

### appsettings.Production.json
Used when `ASPNETCORE_ENVIRONMENT=Production` is set.

### Environment Variables in Docker
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__DefaultConnection` - Automatically set in docker-compose.yml

## Testing

Run all tests:
```bash
dotnet test
```

Run specific test:
```bash
dotnet test --filter "AnimeControllerTests"
```

Test includes:
- ? Pagination tests
- ? Search functionality
- ? Database operations
- ? Sync endpoint
- ? Error handling

## Monthly Sync Schedule

The cron job runs at **2:00 AM on the 1st of every month** and:
1. Calls the `/api/anime/sync` endpoint
2. Fetches top 25 anime from Jikan API
3. Updates PostgreSQL database
4. Logs results to `/var/log/myanimelist-sync.log`

### View Sync Logs
```bash
tail -f /var/log/myanimelist-sync.log
```

## Database Schema

### Anime Table
```sql
CREATE TABLE "Anime" (
    "Id" SERIAL PRIMARY KEY,
    "MalId" INTEGER UNIQUE,
    "Title" VARCHAR(500) NOT NULL,
    "Episodes" INTEGER,
    "Status" VARCHAR(50),
    "Score" DOUBLE PRECISION,
    "Synopsis" TEXT,
    "ImageUrl" VARCHAR(1000),
    "Genre" VARCHAR(500),
    "AiredFrom" TIMESTAMP,
    "AiredTo" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP NOT NULL
);
```

## Troubleshooting

### Container Issues
```bash
# Check logs
docker compose logs -f api

# Rebuild
docker compose down
docker compose up -d --build
```

### Database Connection
```bash
# Test connection
docker compose exec postgres psql -U animeuser -d anime -c "SELECT 1;"

# View tables
docker compose exec postgres psql -U animeuser -d anime -c "\dt"
```

### Cron Job Not Running
```bash
# Check cron status
sudo systemctl status cron

# View cron logs
sudo journalctl -u cron -f

# Test script manually
/usr/local/bin/myanimelist-sync
```

## Security Notes

?? **Before deployment:**
- Change default passwords in configuration files
- Use strong, unique passwords
- Consider using secrets management (AWS Secrets Manager, Azure Key Vault, etc.)
- Set up HTTPS with reverse proxy (Nginx)
- Restrict API access with authentication/authorization if needed

## Contributing

1. Create a feature branch
2. Make your changes
3. Run tests to ensure everything passes
4. Create a pull request

## License

MIT License - feel free to use this project for your needs.

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review Docker logs: `docker compose logs`
3. Check cron logs: `sudo journalctl -u cron`
4. Review the QUICKSTART.md and DEPLOYMENT.md guides

## References

- [Jikan API Documentation](https://jikan.moe/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Docker Documentation](https://docs.docker.com/)
