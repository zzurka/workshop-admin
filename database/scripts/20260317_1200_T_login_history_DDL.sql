-- Migration: 20260317_1200_T_login_history_DDL.sql
-- Description: Create the auth.login_history table. Tracks every login attempt
--              including method, IP, success/failure, and reason.
--              This is an immutable event log — entries are never updated or deleted.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.login_history (
    id             UUID         NOT NULL DEFAULT uuidv7(),
    user_id        UUID         NOT NULL,
    login_method   VARCHAR(50)  NOT NULL,
    ip_address     INET,
    user_agent     TEXT,
    success        BOOLEAN      NOT NULL,
    failure_reason VARCHAR(255),
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_auth_login_history           PRIMARY KEY (id),
    CONSTRAINT fk_auth_login_history_user_id   FOREIGN KEY (user_id) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.login_history                IS 'Immutable event log tracking every login attempt. Entries are never updated or deleted.';
COMMENT ON COLUMN auth.login_history.user_id        IS 'The user who attempted to log in.';
COMMENT ON COLUMN auth.login_history.login_method   IS 'Authentication method: password, google, microsoft, github, magic_link, etc.';
COMMENT ON COLUMN auth.login_history.ip_address     IS 'Client IP address (IPv4 or IPv6) using PostgreSQL INET type.';
COMMENT ON COLUMN auth.login_history.user_agent     IS 'Browser or client user-agent string.';
COMMENT ON COLUMN auth.login_history.success        IS 'TRUE if login succeeded, FALSE if it failed.';
COMMENT ON COLUMN auth.login_history.failure_reason IS 'Reason for failure: invalid_password, account_locked, mfa_failed, etc. NULL on success.';

CREATE INDEX IF NOT EXISTS ix_auth_login_history_user_id_created_at
    ON auth.login_history (user_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_auth_login_history_created_at
    ON auth.login_history (created_at DESC);

COMMIT;
