#!/bin/bash
set -euo pipefail

DB_HOST="${DB_HOST:-fig-db}"
DB_PORT="${DB_PORT:-5432}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
FIG_DB_NAME="${FIG_DB_NAME:-fig}"
FIG_USER_NAME="${FIG_USER_NAME:-fig_login}"
FIG_DB_PASSWORD="${FIG_DB_PASSWORD:?FIG_DB_PASSWORD is required}"
FIG_API_HOST="${FIG_API_HOST:-fig-db}"
FIG_API_PORT="${FIG_API_PORT:-5432}"
CONNECTION_ENV_FILE="${CONNECTION_ENV_FILE:-/fig-config/connection.env}"
MAX_WAIT_ATTEMPTS="${MAX_WAIT_ATTEMPTS:-60}"

export PGPASSWORD="$POSTGRES_PASSWORD"

escape_npgsql_value() {
  # Quote for Npgsql: wrap in single quotes and double any embedded quotes.
  printf "'%s'" "$(printf '%s' "$1" | sed "s/'/''/g")"
}

quote_shell() {
  # Wrap in single quotes for bash source; encode embedded quotes as '\''
  printf "%s" "$1" | sed "s/'/'\\\\''/g; s/^/'/; s/\$/'/"
}

write_connection_env() {
  local password_literal connection_string
  password_literal="$(escape_npgsql_value "$FIG_DB_PASSWORD")"
  connection_string="Host=${FIG_API_HOST};Port=${FIG_API_PORT};Database=${FIG_DB_NAME};Username=${FIG_USER_NAME};Password=${password_literal}"
  mkdir -p "$(dirname "$CONNECTION_ENV_FILE")"
  # Shell-safe export so values containing ';' / quotes survive bash sourcing.
  printf 'export ApiSettings__DbConnectionString=%s\n' "$(quote_shell "$connection_string")" > "$CONNECTION_ENV_FILE"
}

echo "Waiting for PostgreSQL at ${DB_HOST}:${DB_PORT}..."
attempt=1
until psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -c '\q' >/dev/null 2>&1; do
  if [ "$attempt" -ge "$MAX_WAIT_ATTEMPTS" ]; then
    echo "Timed out waiting for PostgreSQL after ${MAX_WAIT_ATTEMPTS} attempts." >&2
    echo "If you changed POSTGRES_PASSWORD against an existing volume, reset the volume or use the original password." >&2
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -c '\q' || true
    exit 1
  fi
  attempt=$((attempt + 1))
  sleep 1
done
echo "PostgreSQL is available."

ROLE_EXISTS=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -tAc \
  -v fig_user="$FIG_USER_NAME" \
  -c "SELECT 1 FROM pg_roles WHERE rolname = :'fig_user'")
if [ "$ROLE_EXISTS" = "1" ]; then
  echo "Role '${FIG_USER_NAME}' already exists. Updating password."
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
    -v fig_user="$FIG_USER_NAME" \
    -v fig_pwd="$FIG_DB_PASSWORD" \
    -c "ALTER ROLE :\"fig_user\" WITH LOGIN PASSWORD :'fig_pwd'"
else
  echo "Creating role '${FIG_USER_NAME}'..."
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
    -v fig_user="$FIG_USER_NAME" \
    -v fig_pwd="$FIG_DB_PASSWORD" \
    -c "CREATE ROLE :\"fig_user\" LOGIN PASSWORD :'fig_pwd'"
fi

DB_EXISTS=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -tAc \
  -v fig_db="$FIG_DB_NAME" \
  -c "SELECT 1 FROM pg_database WHERE datname = :'fig_db'")
if [ "$DB_EXISTS" = "1" ]; then
  echo "Database '${FIG_DB_NAME}' already exists. Skipping database creation."
else
  echo "Creating database '${FIG_DB_NAME}'..."
  psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
    -v fig_db="$FIG_DB_NAME" \
    -v fig_user="$FIG_USER_NAME" \
    -c "CREATE DATABASE :\"fig_db\" OWNER :\"fig_user\""
fi

echo "Granting privileges on '${FIG_DB_NAME}' to '${FIG_USER_NAME}'..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$POSTGRES_USER" -d "$FIG_DB_NAME" -v ON_ERROR_STOP=1 \
  -v fig_user="$FIG_USER_NAME" <<EOF
GRANT ALL ON SCHEMA public TO :"fig_user";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO :"fig_user";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO :"fig_user";
EOF

write_connection_env
echo "Wrote Npgsql connection settings to ${CONNECTION_ENV_FILE}."
echo "PostgreSQL database '${FIG_DB_NAME}' is ready for Fig."
