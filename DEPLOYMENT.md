# Deployment Guide for MyAnimeList API on Ubuntu Server

## Prerequisites
- Ubuntu 24.04 Server
- Docker and Docker Compose installed
- PostgreSQL running in Docker (handled by docker-compose)

## Step 1: Install Docker and Docker Compose on Ubuntu

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
sudo apt install -y docker.io docker-compose-v2

# Add your user to docker group (optional, allows running docker without sudo)
sudo usermod -aG docker $USER
newgrp docker

# Verify installation
docker --version
docker compose version
```

## Step 2: Transfer Project to Ubuntu Server

On your Windows machine:
```powershell
# Publish the project
dotnet publish -c Release -o ./publish

# Transfer to server using SCP
scp -r ./publish petteri@your_server_ip:/home/petteri/myanimelist-app/
scp docker-compose.yml petteri@your_server_ip:/home/petteri/myanimelist-app/
scp -r ./MyAnimeList.Backend petteri@your_server_ip:/home/petteri/myanimelist-app/
scp -r ./scripts petteri@your_server_ip:/home/petteri/myanimelist-app/
```

Or use Git (better option):
```bash
# On Ubuntu
cd /home/petteri
git clone your_repository_url myanimelist-app
cd myanimelist-app
```

## Step 3: Deploy with Docker Compose

```bash
# Navigate to project directory
cd /home/petteri/myanimelist-app

# Build and start containers
sudo docker compose up -d

# Verify containers are running
sudo docker compose ps

# View logs
sudo docker compose logs -f api
```

## Step 4: Set Up Monthly Cron Job

```bash
# Copy the sync script to the system
sudo cp scripts/monthly-sync.sh /usr/local/bin/myanimelist-sync
sudo chmod +x /usr/local/bin/myanimelist-sync

# Create log file
sudo touch /var/log/myanimelist-sync.log
sudo chmod 666 /var/log/myanimelist-sync.log

# Edit crontab
sudo crontab -e

# Add this line to run on the 1st of every month at 2 AM:
0 2 1 * * /usr/local/bin/myanimelist-sync

# Verify cron job
sudo crontab -l
```

## Step 5: Set Up Nginx Reverse Proxy (Optional)

```bash
# Install Nginx
sudo apt install -y nginx

# Create Nginx config
sudo nano /etc/nginx/sites-available/myanimelist
```

Add this configuration:
```nginx
server {
    listen 80;
    server_name your_domain_or_ip;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Then enable it:
```bash
sudo ln -s /etc/nginx/sites-available/myanimelist /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

## Useful Commands

```bash
# View running containers
docker compose ps

# View logs
docker compose logs -f api
docker compose logs -f postgres

# Restart containers
docker compose restart

# Stop containers
docker compose down

# Remove containers and volumes
docker compose down -v

# Execute command in container
docker compose exec api dotnet MyAnimeList.Backend.dll

# Access PostgreSQL
docker compose exec postgres psql -U animeuser -d anime
```

## Troubleshooting

### Container won't start
```bash
# Check logs
docker compose logs api

# Rebuild containers
docker compose down
docker compose up -d --build
```

### Database connection issues
```bash
# Verify PostgreSQL is running and accessible
docker compose exec postgres psql -U animeuser -d anime -c "SELECT 1;"
```

### API is not responding
```bash
# Check if port 5000 is open
sudo lsof -i :5000

# Check firewall
sudo ufw status
sudo ufw allow 5000/tcp
```

## Security Notes

?? **Important**: 
- Change `your_secure_password` in `docker-compose.yml` and `appsettings.Production.json` to a strong password
- Consider using environment variables or secrets management for sensitive data
- Use HTTPS in production (add SSL certificate with Nginx)
- Limit API access with firewall rules or authentication
