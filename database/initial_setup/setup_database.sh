#!/usr/bin/env bash
# =============================================================================
# setup_database.sh
# One-time setup: creates the workshopadmin database and workshopadmin_app user.
#
# Must be run as a PostgreSQL superuser (e.g., postgres).
#
# Usage:
#   ./setup_database.sh [OPTIONS]
#
# Options:
#   --password <pw>   Password for workshopadmin_app (prompted if omitted)
#   --host <host>     PostgreSQL host (default: localhost)
#   --port <port>     PostgreSQL port (default: 5432)
#   --help            Show this help message
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------
DB_NAME="workshopadmin"
DB_USER="workshopadmin_app"
PG_HOST="localhost"
PG_PORT="5432"
PASSWORD=""

# ---------------------------------------------------------------------------
# Parse arguments
# ---------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --password) PASSWORD="$2"; shift 2 ;;
        --host)     PG_HOST="$2"; shift 2 ;;
        --port)     PG_PORT="$2"; shift 2 ;;
        --help)
            head -14 "$0" | grep '^#' | sed 's/^# \?//'
            exit 0
            ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# Prompt for password if not provided
if [[ -z "$PASSWORD" ]]; then
    read -rsp "Enter password for $DB_USER: " PASSWORD
    echo
    if [[ -z "$PASSWORD" ]]; then
        echo "ERROR: Password cannot be empty."
        exit 1
    fi
fi

PSQL="psql -h $PG_HOST -p $PG_PORT"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

# ---------------------------------------------------------------------------
# Create user
# ---------------------------------------------------------------------------
log "Checking if user '$DB_USER' exists..."
USER_EXISTS=$($PSQL -t -A -c "SELECT 1 FROM pg_roles WHERE rolname = '$DB_USER';" 2>/dev/null || echo "")

if [[ "$USER_EXISTS" == "1" ]]; then
    log "User '$DB_USER' already exists. Updating password..."
    $PSQL -c "ALTER USER $DB_USER WITH PASSWORD '$PASSWORD';" >/dev/null
else
    log "Creating user '$DB_USER'..."
    $PSQL -c "CREATE USER $DB_USER WITH PASSWORD '$PASSWORD';" >/dev/null
fi

# ---------------------------------------------------------------------------
# Create database
# ---------------------------------------------------------------------------
log "Checking if database '$DB_NAME' exists..."
DB_EXISTS=$($PSQL -t -A -c "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME';" 2>/dev/null || echo "")

if [[ "$DB_EXISTS" == "1" ]]; then
    log "Database '$DB_NAME' already exists. Skipping creation."
else
    log "Creating database '$DB_NAME'..."
    $PSQL -c "CREATE DATABASE $DB_NAME OWNER $DB_USER ENCODING 'UTF8' LC_COLLATE 'en_US.UTF-8' LC_CTYPE 'en_US.UTF-8' TEMPLATE template0;" >/dev/null
fi

# ---------------------------------------------------------------------------
# Configure database
# ---------------------------------------------------------------------------
DB_PSQL="psql -h $PG_HOST -p $PG_PORT -d $DB_NAME"

log "Configuring database..."

# Grant all privileges on the database to the app user
$DB_PSQL -c "GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;" >/dev/null

# Grant schema usage and creation
$DB_PSQL -c "GRANT ALL ON SCHEMA public TO $DB_USER;" >/dev/null

# Set default privileges so workshopadmin_app owns future objects
$DB_PSQL -c "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO $DB_USER;" >/dev/null
$DB_PSQL -c "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO $DB_USER;" >/dev/null
$DB_PSQL -c "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO $DB_USER;" >/dev/null

# Allow the app user to create schemas (needed for migration schema)
$DB_PSQL -c "GRANT CREATE ON DATABASE $DB_NAME TO $DB_USER;" >/dev/null

log "Setup complete."
echo ""
echo "=============================="
echo "  Database: $DB_NAME"
echo "  User:     $DB_USER"
echo "  Host:     $PG_HOST"
echo "  Port:     $PG_PORT"
echo "=============================="
echo ""
echo "Next steps:"
echo "  1. Copy .env.example to .env and fill in the connection parameters"
echo "  2. Run ./run_migrations.sh to apply migrations"
