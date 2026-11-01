#!/bin/bash
set -euo pipefail

DB_HOST="${DB_HOST:-fig-db}"
DB_PORT="${DB_PORT:-5432}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
FIG_DB_NAME="${FIG_DB_NAME:-fig}"
FIG_USER_NAME="${FIG_USER_NAME:-fig_login}"
FIG_DB_PASSWORD="${FIG_DB_PASSWORD:?FIG_DB_PASSWORD is required}"

export PGPASSWORD="$POSTGRES_PASSWORD"

echo "Waiting for PostgreSQL at ${DB_HOST}:${DB_PORT}..."
until psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -c '\q' >/dev/null 2>&1; do
  sleep 1
done
echo "PostgreSQL is available."

ROLE_EXISTS=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -tAc \
  "SELECT 1 FROM pg_roles WHERE rolname = '${FIG_USER_NAME}'")
if [ "$ROLE_EXISTS" = "1" ]; then
  echo "Role '${FIG_USER_NAME}' already exists. Skipping role creation."
else
  echo "Creating role '${FIG_USER_NAME}'..."
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
    -c "CREATE ROLE \"${FIG_USER_NAME}\" LOGIN PASSWORD '${FIG_DB_PASSWORD}';"
fi

DB_EXISTS=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -tAc \
  "SELECT 1 FROM pg_database WHERE datname = '${FIG_DB_NAME}'")
if [ "$DB_EXISTS" = "1" ]; then
  echo "Database '${FIG_DB_NAME}' already exists. Skipping database creation."
else
  echo "Creating database '${FIG_DB_NAME}'..."
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
    -c "CREATE DATABASE \"${FIG_DB_NAME}\" OWNER \"${FIG_USER_NAME}\";"
fi

echo "Granting privileges on '${FIG_DB_NAME}' to '${FIG_USER_NAME}'..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d "$FIG_DB_NAME" -v ON_ERROR_STOP=1 <<EOF
GRANT ALL ON SCHEMA public TO "${FIG_USER_NAME}";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO "${FIG_USER_NAME}";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO "${FIG_USER_NAME}";
EOF

echo "PostgreSQL database '${FIG_DB_NAME}' is ready for Fig."
