#!/bin/bash
set -e

POSTGRES_CONTAINER="smarttask_postgres"
POSTGRES_USER="admin"

echo "⏳ Waiting for PostgreSQL..."
until docker exec "$POSTGRES_CONTAINER" pg_isready -U "$POSTGRES_USER" >/dev/null 2>&1; do
  sleep 2
done

echo "🗄️ Ensuring databases exist..."

docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" <<EOF
SELECT 'CREATE DATABASE "SmartTaskDB"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'SmartTaskDB')\gexec;

SELECT 'CREATE DATABASE "SmartTaskDB_Dev"'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'SmartTaskDB_Dev')\gexec;
EOF

echo "✅ Database ready"
