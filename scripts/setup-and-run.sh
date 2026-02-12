#!/bin/bash
set -euo pipefail

cd ~/Desktop/Smart_Task_Management_API

# ------------------------------------------------------------
# 0. Load environment variables
# ------------------------------------------------------------
export ASPNETCORE_ENVIRONMENT=Development
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

# ------------------------------------------------------------
# 1. Full cleanup – stop everything, remove volumes
# ------------------------------------------------------------
echo "1. Stopping existing containers and removing volumes..."
docker compose down -v

# ------------------------------------------------------------
# 2. Pre‑flight port check – ensure 5000 is free
# ------------------------------------------------------------
echo "🔍 Checking if port 5000 is available..."
if ss -tuln | grep -q ":5000 "; then
    echo "❌ Port 5000 is still in use after cleanup. Free it manually:"
    sudo lsof -i :5000 || true
    exit 1
fi
echo "✅ Port 5000 is free."

# ------------------------------------------------------------
# 3. Remove all existing migration files
# ------------------------------------------------------------
echo "2. Cleaning up old migration files..."
find src/SmartTaskManagement.Infrastructure/Data/Migrations -type f -name "*.cs" -delete 2>/dev/null || true
find src/SmartTaskManagement.Infrastructure/Data/Migrations -type f -name "*.designer.cs" -delete 2>/dev/null || true

# ------------------------------------------------------------
# 4. Prepare PostgreSQL data directory
# ------------------------------------------------------------
echo "3. Creating data directory with proper permissions..."
sudo rm -rf postgres-data 2>/dev/null || true
mkdir -p postgres-data
sudo chown -R 1000:1000 postgres-data

# ------------------------------------------------------------
# 5. Start only PostgreSQL
# ------------------------------------------------------------
echo "4. Starting PostgreSQL..."
docker compose up -d postgres

# ------------------------------------------------------------
# 6. Wait for PostgreSQL to be ready
# ------------------------------------------------------------
echo "5. Waiting for PostgreSQL to be ready..."
for i in {1..30}; do
    if docker exec smarttaskmanagement-db pg_isready -U postgres > /dev/null 2>&1; then
        echo "✅ PostgreSQL is ready!"
        
        echo "5a. Ensuring Hangfire database exists..."
        if docker exec smarttaskmanagement-db psql -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'Hangfire'" | grep -q 1; then
            echo "✅ Hangfire database already exists."
        else
            echo "Creating Hangfire database..."
            docker exec smarttaskmanagement-db psql -U postgres -c "CREATE DATABASE Hangfire;"
            echo "✅ Hangfire database created."
        fi
        break
    fi
    echo "⏳ Waiting for PostgreSQL... ($i/30)"
    sleep 2
done

if ! docker exec smarttaskmanagement-db pg_isready -U postgres > /dev/null 2>&1; then
    echo "❌ PostgreSQL failed to start. Checking logs..."
    docker logs smarttaskmanagement-db
    exit 1
fi

# ------------------------------------------------------------
# 7. Build the solution
# ------------------------------------------------------------
echo "6. Building the project..."
dotnet build

# ------------------------------------------------------------
# 8. Remove any leftover migrations
# ------------------------------------------------------------
echo "7. Removing old migrations (if any)..."
dotnet ef migrations remove \
    --project src/SmartTaskManagement.Infrastructure \
    --startup-project src/SmartTaskManagement.API \
    --force 2>/dev/null || true

# ------------------------------------------------------------
# 9. Create a fresh migration
# ------------------------------------------------------------
echo "8. Creating new migration 'InitialCreate'..."
dotnet ef migrations add InitialCreate \
    --project src/SmartTaskManagement.Infrastructure \
    --startup-project src/SmartTaskManagement.API \
    --output-dir Data/Migrations

# ------------------------------------------------------------
# 10. Check for accidental SQL Server syntax (cosmetic)
# ------------------------------------------------------------
echo "9. Checking generated migration for SQL Server artifacts..."
if grep -r "\\[.*\\]" --include="*.cs" src/SmartTaskManagement.Infrastructure/Data/Migrations/ 2>/dev/null | grep -v "DbContext\|Migration\|Table\|Column"; then
    echo "⚠️  Warning: Found possible SQL Server syntax in migration files. Review configurations."
fi

# ------------------------------------------------------------
# 11. Apply migration to database
# ------------------------------------------------------------
echo "10. Updating database schema..."
dotnet ef database update \
    --project src/SmartTaskManagement.Infrastructure \
    --startup-project src/SmartTaskManagement.API

# ------------------------------------------------------------
# 12. Verify main application tables
# ------------------------------------------------------------
echo "11. Verifying database tables..."
docker exec smarttaskmanagement-db psql -U postgres -d SmartTaskManagement -c "\dt"

# ------------------------------------------------------------
# 13. Start full stack (API, Seq)
# ------------------------------------------------------------
echo "12. Starting full Docker Compose stack..."
docker compose up -d

# ------------------------------------------------------------
# 14. Wait for port 5000 to be bound on the host
# ------------------------------------------------------------
echo "12a. Waiting for port 5000 to be bound..."
for i in {1..30}; do
    if ss -tuln | grep -q ":5000 "; then
        echo "✅ Port 5000 is bound."
        break
    fi
    echo "⏳ Waiting for port binding... ($i/30)"
    sleep 1
done

if ! ss -tuln | grep -q ":5000 "; then
    echo "❌ Port 5000 never bound. Check if another process is using it:"
    sudo lsof -i :5000 || true
    exit 1
fi

# ------------------------------------------------------------
# 15. Verify Docker port mapping
# ------------------------------------------------------------
echo "12b. Verifying Docker port mapping..."
docker port smarttaskmanagement-api || {
    echo "❌ Port mapping failed. Container may have crashed."
    docker logs smarttaskmanagement-api --tail 50
    exit 1
}

# ------------------------------------------------------------
# 16. Wait for API health endpoint – FORCE IPv4
# ------------------------------------------------------------
echo "13. Waiting for API to become healthy (using IPv4)..."
HEALTHY=false
for i in {1..30}; do
    HTTP_STATUS=$(curl -s -o /tmp/health_response -w "%{http_code}" http://127.0.0.1:5000/health || echo "000")
    if [ "$HTTP_STATUS" -eq 200 ]; then
        if grep -q "Healthy" /tmp/health_response; then
            echo "✅ API is healthy!"
            HEALTHY=true
            break
        else
            echo "⏳ API returned 200 but status is not Healthy yet (attempt $i/30)"
            cat /tmp/health_response
        fi
    else
        echo "⏳ Waiting for API health... ($i/30) [HTTP $HTTP_STATUS]"
    fi
    sleep 2
done

if [ "$HEALTHY" = false ]; then
    echo "❌ API failed to become healthy. Last response (HTTP $HTTP_STATUS):"
    [ -f /tmp/health_response ] && cat /tmp/health_response
    echo ""
    echo "📋 Container logs (last 50 lines):"
    docker logs smarttaskmanagement-api --tail 50
    exit 1
fi

# ------------------------------------------------------------
# 17. Verify Swagger UI is accessible – FOLLOW REDIRECTS
# ------------------------------------------------------------
echo "14. Testing Swagger UI (IPv4, follow redirects)..."
SWAGGER_STATUS=$(curl -L -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5000/swagger || echo "000")
if [[ "$SWAGGER_STATUS" -eq 200 || "$SWAGGER_STATUS" -eq 401 || "$SWAGGER_STATUS" -eq 403 ]]; then
    echo "✅ Swagger UI is accessible (HTTP $SWAGGER_STATUS)."
else
    echo "❌ Swagger UI not responding (HTTP $SWAGGER_STATUS)."
    docker logs smarttaskmanagement-api --tail 50
    exit 1
fi

# ------------------------------------------------------------
# 18. Success output – use 127.0.0.1 for reliable access
# ------------------------------------------------------------
echo ""
echo "✅✅✅ SETUP COMPLETE – APPLICATION IS RUNNING ✅✅✅"
echo ""
echo "   🔹 API (IPv4):     http://127.0.0.1:5000"
echo "   🔹 Swagger UI:     http://127.0.0.1:5000/swagger"
echo "   🔹 Hangfire:       http://127.0.0.1:5000/hangfire (admin only)"
echo "   🔹 Seq logs:       http://localhost:8081"
echo ""
echo "⚠️  Note: If 'localhost' does not work, use '127.0.0.1' instead."
echo ""