# MyAnimeList API - Quick Start Guide

## What's Ready for Deployment

? **Backend API** - .NET 10 ASP.NET Core API
? **Database** - PostgreSQL support configured
? **Monthly Sync** - Automated anime data synchronization endpoint
? **Docker Setup** - Docker Compose for easy deployment
? **Tests** - Full test coverage with Moq mocking

## Key Endpoints

- `GET /api/anime` - Get paginated list of anime
- `GET /api/anime/{id}` - Get specific anime by ID
- `GET /api/anime/search?query=...` - Search anime by title or synopsis
- `POST /api/anime/sync` - Trigger monthly anime data sync (called by cron job)

## Environment Setup

### Windows Development (Already Done)
```powershell
# Added Npgsql.EntityFrameworkCore.PostgreSQL package
# Changed from SQL Server to PostgreSQL in Program.cs
# Updated appsettings.json with PostgreSQL connection string
# Created appsettings.Production.json for Ubuntu deployment
# Created docker-compose.yml for easy deployment
# Added sync endpoint to AnimeController
# Updated all tests to work with new dependencies
```

### Ubuntu Server Deployment

#### Step 1: Install Docker
```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker $USER
newgrp docker
```

#### Step 2: Transfer Project
```bash
# Option A: Using Git (Recommended)
cd /home/petteri
git clone your_repository_url myanimelist-app
cd myanimelist-app

# Option B: Using SCP
scp -r MyAnimeList.WebApi petteri@your_server_ip:/home/petteri/myanimelist-app
```

#### Step 3: Deploy with Docker Compose
```bash
cd /home/petteri/myanimelist-app
sudo docker compose up -d

# Verify deployment
sudo docker compose ps
sudo docker compose logs -f api
```

#### Step 4: Set Up Monthly Cron Job
```bash
# Copy sync script
sudo cp scripts/monthly-sync.sh /usr/local/bin/myanimelist-sync
sudo chmod +x /usr/local/bin/myanimelist-sync

# Create log file
sudo touch /var/log/myanimelist-sync.log
sudo chmod 666 /var/log/myanimelist-sync.log

# Edit crontab (runs at 2 AM on the 1st of each month)
sudo crontab -e
# Add: 0 2 1 * * /usr/local/bin/myanimelist-sync
```

## Configuration Files

### docker-compose.yml
- Runs PostgreSQL 16 Alpine (lightweight)
- Runs your API on port 5000
- Auto-connects API to database
- Persists data in `postgres_data` volume
- Health checks enabled

### appsettings.Production.json
- Connection string uses Docker service name `postgres`
- Detailed errors disabled for security
- Ready for production deployment

### scripts/monthly-sync.sh
- Calls `/api/anime/sync` endpoint
- Logs results to `/var/log/myanimelist-sync.log`
- Runs monthly via cron

## Important: Update Passwords!

?? **SECURITY**: Before deployment, change the default password in:
1. `docker-compose.yml` - Change `your_secure_password`
2. `appsettings.Production.json` - Change `your_secure_password`

```bash
# Generate a secure password
openssl rand -base64 32
```

## Testing Before Deployment

### Run Tests Locally
```powershell
dotnet test
```

### Test Sync Endpoint Locally
```powershell
# Start the app
dotnet run --project MyAnimeList.Backend

# In another terminal, trigger sync
curl -X POST http://localhost:5000/api/anime/sync
```

## Post-Deployment Verification

```bash
# Check containers
docker compose ps

# Check logs
docker compose logs api

# Test API
curl http://localhost:5000/api/anime

# Test sync endpoint
curl -X POST http://localhost:5000/api/anime/sync

# View database
docker compose exec postgres psql -U animelistuser -d animelistdb -c "SELECT COUNT(*) FROM \"Anime\";"

# Check cron logs
sudo tail -f /var/log/myanimelist-sync.log
```

## Troubleshooting

### API won't connect to database
```bash
# Check if PostgreSQL is healthy
docker compose exec postgres pg_isready -U animelistuser

# Check container logs
docker compose logs postgres
```

### Cron job not running
```bash
# Verify cron job
sudo crontab -l

# Check system logs
sudo journalctl -u cron -f

# Test script manually
/usr/local/bin/myanimelist-sync
cat /var/log/myanimelist-sync.log
```

### Rebuild and restart
```bash
docker compose down
docker compose up -d --build
```

## Next Steps

1. ? Run `dotnet build` - Already done!
2. ? Run `dotnet test` - Ready to run
3. ?? Update passwords in config files
4. ?? Deploy to Ubuntu using Docker Compose
5. ?? Verify cron job is working
6. ?? Consider setting up HTTPS with Nginx

## Files Modified/Created

- `MyAnimeList.Backend/Program.cs` - Changed to use PostgreSQL
- `MyAnimeList.Backend/appsettings.json` - PostgreSQL connection string
- `MyAnimeList.Backend/appsettings.Production.json` - NEW
- `MyAnimeList.Backend/Controllers/AnimeController.cs` - Added sync endpoint
- `MyAnimeList.Tests/Controllers/AnimeControllerTests.cs` - Updated tests
- `docker-compose.yml` - NEW
- `scripts/monthly-sync.sh` - NEW
- `DEPLOYMENT.md` - Detailed deployment guide
- `QUICKSTART.md` - This file

## Support

If you encounter issues:
1. Check the Docker logs: `docker compose logs -f`
2. Verify PostgreSQL is running: `docker compose ps`
3. Check firewall: `sudo ufw status`
4. Review the full DEPLOYMENT.md guide
