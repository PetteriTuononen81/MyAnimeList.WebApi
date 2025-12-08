#!/bin/bash

# MyAnimeList Monthly Sync Script
# This script calls the anime API endpoint to fetch and store anime data

LOG_FILE="/var/log/myanimelist-sync.log"
API_URL="http://localhost:5000/api/anime/sync"

# Log function
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "$LOG_FILE"
}

log "Starting monthly anime sync..."

# Call the API endpoint
response=$(curl -s -w "\n%{http_code}" -X POST "$API_URL" \
    -H "Content-Type: application/json" \
    -d '{}')

http_code=$(echo "$response" | tail -n 1)
body=$(echo "$response" | head -n -1)

if [ "$http_code" -eq 200 ]; then
    log "Successfully completed anime sync. HTTP Status: $http_code"
else
    log "Error during anime sync. HTTP Status: $http_code. Response: $body"
fi

log "Monthly anime sync completed."
