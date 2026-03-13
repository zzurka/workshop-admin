-- Migration: 20260313_1400_S_migration_DDL.sql
-- Description: Create the migration schema and migration_history tracking table.
--              This is the bootstrap migration and must be the first script executed.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-13
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

-- Create dedicated schema for migration tracking
CREATE SCHEMA IF NOT EXISTS migration;

-- Create the migration history tracking table
CREATE TABLE IF NOT EXISTS migration.migration_history (
    id              SERIAL          PRIMARY KEY,
    script_name     VARCHAR(255)    NOT NULL,
    checksum_sha256 CHAR(64)        NOT NULL,
    applied_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    applied_by      VARCHAR(128)    NOT NULL DEFAULT CURRENT_USER,
    execution_ms    INTEGER,
    success         BOOLEAN         NOT NULL DEFAULT TRUE,
    error_message   TEXT,

    CONSTRAINT uq_migration_history_script_name UNIQUE (script_name)
);

COMMENT ON TABLE migration.migration_history IS
    'Tracks executed database migration scripts with SHA-256 checksums to prevent re-execution and detect tampering.';
COMMENT ON COLUMN migration.migration_history.script_name IS
    'Filename of the migration script (e.g., 20260313_1500_T_workshops_DDL.sql).';
COMMENT ON COLUMN migration.migration_history.checksum_sha256 IS
    'SHA-256 hex digest of the script contents at execution time.';
COMMENT ON COLUMN migration.migration_history.applied_at IS
    'Timestamp (with timezone) when the script was executed.';
COMMENT ON COLUMN migration.migration_history.applied_by IS
    'PostgreSQL role that executed the script.';
COMMENT ON COLUMN migration.migration_history.execution_ms IS
    'Execution duration in milliseconds.';
COMMENT ON COLUMN migration.migration_history.success IS
    'Whether the script executed successfully.';
COMMENT ON COLUMN migration.migration_history.error_message IS
    'Error message if execution failed (NULL on success).';

CREATE INDEX IF NOT EXISTS ix_migration_history_applied_at
    ON migration.migration_history (applied_at);

COMMIT;
