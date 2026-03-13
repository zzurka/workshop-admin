#!/usr/bin/env bash
# =============================================================================
# run_migrations.sh
# Database migration runner for PostgreSQL
#
# Executes .sql scripts from database/scripts/ in lexicographic order.
# Tracks execution in migration.migration_history with SHA-256 checksums.
#
# Usage:
#   ./run_migrations.sh [OPTIONS]
#
# Options:
#   --dry-run    Show which scripts would be executed without running them
#   --verbose    Print detailed output for each script
#   --help       Show this help message
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
SQL_DIR="$PROJECT_ROOT/database/scripts"
ENV_FILE="$SCRIPT_DIR/.env"

DRY_RUN=false
VERBOSE=false

# ---------------------------------------------------------------------------
# Parse arguments
# ---------------------------------------------------------------------------
for arg in "$@"; do
    case "$arg" in
        --dry-run)  DRY_RUN=true ;;
        --verbose)  VERBOSE=true ;;
        --help)
            head -20 "$0" | grep '^#' | sed 's/^# \?//'
            exit 0
            ;;
        *)
            echo "Unknown option: $arg"
            echo "Use --help for usage information."
            exit 1
            ;;
    esac
done

# ---------------------------------------------------------------------------
# Load environment variables
# ---------------------------------------------------------------------------
if [[ -f "$ENV_FILE" ]]; then
    # Export variables from .env file (skip comments and blank lines)
    set -a
    # shellcheck disable=SC1090
    source <(grep -v '^\s*#' "$ENV_FILE" | grep -v '^\s*$')
    set +a
    $VERBOSE && echo "Loaded connection parameters from $ENV_FILE"
fi

# Validate required connection parameters
for var in PGHOST PGPORT PGDATABASE PGUSER; do
    if [[ -z "${!var:-}" ]]; then
        echo "ERROR: $var is not set. Configure it in $ENV_FILE or as an environment variable."
        exit 1
    fi
done

# ---------------------------------------------------------------------------
# Utility functions
# ---------------------------------------------------------------------------
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

verbose() {
    $VERBOSE && log "$@"
}

compute_checksum() {
    local file="$1"
    if command -v sha256sum &>/dev/null; then
        sha256sum "$file" | awk '{print $1}'
    elif command -v shasum &>/dev/null; then
        shasum -a 256 "$file" | awk '{print $1}'
    else
        echo "ERROR: Neither sha256sum nor shasum found." >&2
        exit 1
    fi
}

# Run a SQL query and return the result
psql_query() {
    psql -t -A -c "$1" 2>/dev/null
}

# Run a SQL file
psql_exec() {
    psql -v ON_ERROR_STOP=1 -f "$1" 2>&1
}

# ---------------------------------------------------------------------------
# Ensure migration tracking infrastructure exists
# ---------------------------------------------------------------------------
ensure_tracking_table() {
    local bootstrap_script="$SQL_DIR/20260313_1400_S_migration_DDL.sql"

    # Check if migration.migration_history exists
    local table_exists
    table_exists=$(psql_query "SELECT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'migration' AND table_name = 'migration_history'
    );" || echo "f")

    if [[ "$table_exists" != "t" ]]; then
        log "Migration tracking table not found. Running bootstrap migration..."

        if [[ ! -f "$bootstrap_script" ]]; then
            echo "ERROR: Bootstrap script not found at $bootstrap_script"
            exit 1
        fi

        if $DRY_RUN; then
            log "[DRY RUN] Would execute bootstrap: $(basename "$bootstrap_script")"
            return
        fi

        local output
        output=$(psql_exec "$bootstrap_script" 2>&1) || {
            echo "ERROR: Bootstrap migration failed:"
            echo "$output"
            exit 1
        }
        log "Bootstrap migration completed successfully."
    fi
}

# ---------------------------------------------------------------------------
# Main migration loop
# ---------------------------------------------------------------------------
run_migrations() {
    local scripts_dir="$SQL_DIR"
    local executed=0
    local skipped=0
    local failed=0
    local total=0

    # Collect and sort SQL files
    local -a sql_files=()
    while IFS= read -r -d '' file; do
        sql_files+=("$file")
    done < <(find "$scripts_dir" -maxdepth 1 -name '*.sql' -print0 | sort -z)

    total=${#sql_files[@]}

    if [[ $total -eq 0 ]]; then
        log "No SQL scripts found in $scripts_dir"
        return
    fi

    log "Found $total SQL script(s) in $scripts_dir"
    echo ""

    for sql_file in "${sql_files[@]}"; do
        local script_name
        script_name=$(basename "$sql_file")
        local checksum
        checksum=$(compute_checksum "$sql_file")

        # Query the tracking table for this script
        local row
        row=$(psql_query "SELECT checksum_sha256, success
                          FROM migration.migration_history
                          WHERE script_name = '$script_name'
                          ORDER BY id DESC LIMIT 1;" 2>/dev/null || echo "")

        if [[ -n "$row" ]]; then
            local stored_checksum stored_success
            stored_checksum=$(echo "$row" | cut -d'|' -f1)
            stored_success=$(echo "$row" | cut -d'|' -f2)

            if [[ "$stored_success" == "t" ]]; then
                # Script was previously executed successfully
                if [[ "$stored_checksum" != "$checksum" ]]; then
                    echo "ERROR: Checksum mismatch for $script_name"
                    echo "  Stored:  $stored_checksum"
                    echo "  Current: $checksum"
                    echo "  The script was modified after execution. Create a new migration instead."
                    exit 1
                fi
                verbose "SKIP: $script_name (already executed)"
                ((skipped++))
                continue
            else
                # Script previously failed — delete the failed record and retry
                log "RETRY: $script_name (previous execution failed)"
                if ! $DRY_RUN; then
                    psql_query "DELETE FROM migration.migration_history
                                WHERE script_name = '$script_name' AND success = FALSE;" >/dev/null
                fi
            fi
        fi

        # Execute the script
        if $DRY_RUN; then
            log "[DRY RUN] Would execute: $script_name (checksum: ${checksum:0:16}...)"
            ((executed++))
            continue
        fi

        log "EXEC: $script_name"
        local start_ms end_ms duration_ms output exit_code
        start_ms=$(date +%s%3N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1000))')

        output=$(psql_exec "$sql_file" 2>&1)
        exit_code=$?

        end_ms=$(date +%s%3N 2>/dev/null || python3 -c 'import time; print(int(time.time()*1000))')
        duration_ms=$((end_ms - start_ms))

        if [[ $exit_code -eq 0 ]]; then
            # Record success
            psql_query "INSERT INTO migration.migration_history
                            (script_name, checksum_sha256, execution_ms, success)
                        VALUES
                            ('$script_name', '$checksum', $duration_ms, TRUE);" >/dev/null
            verbose "  Completed in ${duration_ms}ms"
            ((executed++))
        else
            # Record failure
            local safe_error
            safe_error=$(echo "$output" | sed "s/'/''/g" | head -20)
            psql_query "INSERT INTO migration.migration_history
                            (script_name, checksum_sha256, execution_ms, success, error_message)
                        VALUES
                            ('$script_name', '$checksum', $duration_ms, FALSE, '$safe_error');" >/dev/null

            echo ""
            echo "FAILED: $script_name"
            echo "$output"
            echo ""
            log "Stopping execution due to failure."
            ((failed++))
            break
        fi
    done

    # Summary
    echo ""
    echo "=============================="
    log "Migration Summary"
    echo "  Total scripts: $total"
    echo "  Executed:      $executed"
    echo "  Skipped:       $skipped"
    echo "  Failed:        $failed"
    echo "=============================="

    if [[ $failed -gt 0 ]]; then
        exit 1
    fi
}

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------
log "Starting database migrations..."
$DRY_RUN && log "*** DRY RUN MODE — no changes will be made ***"
echo ""

ensure_tracking_table
run_migrations

log "Done."
