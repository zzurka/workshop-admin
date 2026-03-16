#!/usr/bin/env bash
# =============================================================================
# setup_database.sh
# One-time setup: creates the workshopadmin database, admin user (owner),
# and app user (limited privileges for the backend).
#
# Must be run as a PostgreSQL superuser (e.g., postgres).
#
# Users created:
#   workshopadmin_admin  - database owner, runs migrations (CREATE/ALTER/DROP)
#   workshopadmin_app    - backend user (SELECT, INSERT, UPDATE, DELETE only)
#
# Usage:
#   ./setup_database.sh [OPTIONS]
#
# Options:
#   --admin-password <pw>   Password for workshopadmin_admin (prompted if omitted)
#   --app-password <pw>     Password for workshopadmin_app (prompted if omitted)
#   --superuser <user>      PostgreSQL superuser to connect as (default: postgres)
#   --host <host>           PostgreSQL host (default: localhost)
#   --port <port>           PostgreSQL port (default: 5432)
#   --help                  Show this help message
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------
DB_NAME="workshopadmin"
ADMIN_USER="workshopadmin_admin"
APP_USER="workshopadmin_app"
PG_SUPERUSER="postgres"
PG_HOST="localhost"
PG_PORT="5432"
ADMIN_PASSWORD=""
APP_PASSWORD=""

# ---------------------------------------------------------------------------
# Parse arguments
# ---------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --admin-password) ADMIN_PASSWORD="$2"; shift 2 ;;
        --app-password)   APP_PASSWORD="$2"; shift 2 ;;
        --superuser)      PG_SUPERUSER="$2"; shift 2 ;;
        --host)           PG_HOST="$2"; shift 2 ;;
        --port)           PG_PORT="$2"; shift 2 ;;
        --help)
            head -22 "$0" | grep '^#' | sed 's/^# \?//'
            exit 0
            ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# Prompt for passwords if not provided
if [[ -z "$ADMIN_PASSWORD" ]]; then
    read -rsp "Enter password for $ADMIN_USER: " ADMIN_PASSWORD
    echo
    if [[ -z "$ADMIN_PASSWORD" ]]; then
        echo "ERROR: Password cannot be empty."
        exit 1
    fi
fi

if [[ -z "$APP_PASSWORD" ]]; then
    read -rsp "Enter password for $APP_USER: " APP_PASSWORD
    echo
    if [[ -z "$APP_PASSWORD" ]]; then
        echo "ERROR: Password cannot be empty."
        exit 1
    fi
fi

PSQL="psql -h $PG_HOST -p $PG_PORT -U $PG_SUPERUSER"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

# ---------------------------------------------------------------------------
# Create admin user (database owner, runs migrations)
# ---------------------------------------------------------------------------
log "Checking if user '$ADMIN_USER' exists..."
ADMIN_EXISTS=$($PSQL -t -A -c "SELECT 1 FROM pg_roles WHERE rolname = '$ADMIN_USER';" 2>/dev/null || echo "")

if [[ "$ADMIN_EXISTS" == "1" ]]; then
    log "User '$ADMIN_USER' already exists. Updating password..."
    $PSQL -c "ALTER USER $ADMIN_USER WITH PASSWORD '$ADMIN_PASSWORD';" >/dev/null
else
    log "Creating user '$ADMIN_USER'..."
    $PSQL -c "CREATE USER $ADMIN_USER WITH PASSWORD '$ADMIN_PASSWORD';" >/dev/null
fi

# ---------------------------------------------------------------------------
# Create app user (backend, limited privileges)
# ---------------------------------------------------------------------------
log "Checking if user '$APP_USER' exists..."
APP_EXISTS=$($PSQL -t -A -c "SELECT 1 FROM pg_roles WHERE rolname = '$APP_USER';" 2>/dev/null || echo "")

if [[ "$APP_EXISTS" == "1" ]]; then
    log "User '$APP_USER' already exists. Updating password..."
    $PSQL -c "ALTER USER $APP_USER WITH PASSWORD '$APP_PASSWORD';" >/dev/null
else
    log "Creating user '$APP_USER'..."
    $PSQL -c "CREATE USER $APP_USER WITH PASSWORD '$APP_PASSWORD';" >/dev/null
fi

# ---------------------------------------------------------------------------
# Create database (owned by admin)
# ---------------------------------------------------------------------------
log "Checking if database '$DB_NAME' exists..."
DB_EXISTS=$($PSQL -t -A -c "SELECT 1 FROM pg_database WHERE datname = '$DB_NAME';" 2>/dev/null || echo "")

if [[ "$DB_EXISTS" == "1" ]]; then
    log "Database '$DB_NAME' already exists. Skipping creation."
else
    log "Creating database '$DB_NAME' owned by '$ADMIN_USER'..."
    $PSQL -c "CREATE DATABASE $DB_NAME OWNER $ADMIN_USER ENCODING 'UTF8' TEMPLATE template0;" >/dev/null
fi

# ---------------------------------------------------------------------------
# Configure privileges
# ---------------------------------------------------------------------------
DB_PSQL="psql -h $PG_HOST -p $PG_PORT -U $PG_SUPERUSER -d $DB_NAME"

log "Configuring admin privileges..."

# Admin: full control over the database
$DB_PSQL -c "GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $ADMIN_USER;" >/dev/null
$DB_PSQL -c "GRANT ALL ON SCHEMA public TO $ADMIN_USER;" >/dev/null
$DB_PSQL -c "GRANT CREATE ON DATABASE $DB_NAME TO $ADMIN_USER;" >/dev/null

log "Configuring app user privileges..."

# App: connect to the database
$DB_PSQL -c "GRANT CONNECT ON DATABASE $DB_NAME TO $APP_USER;" >/dev/null

# App: use the public schema (but not create objects)
$DB_PSQL -c "GRANT USAGE ON SCHEMA public TO $APP_USER;" >/dev/null

# App: DML only on existing and future tables/sequences
$DB_PSQL -c "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $APP_USER;" >/dev/null
$DB_PSQL -c "GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO $APP_USER;" >/dev/null
$DB_PSQL -c "GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO $APP_USER;" >/dev/null

# Default privileges: auto-grant to app user on objects created by admin
$DB_PSQL -c "ALTER DEFAULT PRIVILEGES FOR ROLE $ADMIN_USER IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $APP_USER;" >/dev/null
$DB_PSQL -c "ALTER DEFAULT PRIVILEGES FOR ROLE $ADMIN_USER IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO $APP_USER;" >/dev/null
$DB_PSQL -c "ALTER DEFAULT PRIVILEGES FOR ROLE $ADMIN_USER IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO $APP_USER;" >/dev/null

log "Setup complete."
echo ""
echo "=============================="
echo "  Database:   $DB_NAME"
echo "  Admin user: $ADMIN_USER  (owner, migrations)"
echo "  App user:   $APP_USER  (backend, DML only)"
echo "  Host:       $PG_HOST"
echo "  Port:       $PG_PORT"
echo "=============================="
echo ""
echo "Next steps:"
echo "  1. Copy .env.example to .env in script_runners/"
echo "  2. Use $ADMIN_USER credentials in .env (for running migrations)"
echo "  3. Use $APP_USER credentials in your backend appsettings.json"
echo "  4. Run ./run_migrations.sh to apply migrations"
