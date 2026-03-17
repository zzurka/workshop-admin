-- Migration: 20260317_1210_T_mfa_recovery_codes_DDL.sql
-- Description: Create the auth.mfa_recovery_codes table. Stores hashed backup
--              codes for users who lose access to their primary MFA device.
--              Codes are single-use and generated in batches (typically 10).
--              This is an immutable-style table — codes are created, used once,
--              never updated otherwise.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.mfa_recovery_codes (
    id         UUID         NOT NULL DEFAULT uuid_generate_v7(),
    user_id    UUID         NOT NULL,
    code_hash  VARCHAR(255) NOT NULL,
    is_used    BOOLEAN      NOT NULL DEFAULT FALSE,
    used_at    TIMESTAMPTZ,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_auth_mfa_recovery_codes           PRIMARY KEY (id),
    CONSTRAINT fk_auth_mfa_recovery_codes_user_id   FOREIGN KEY (user_id) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.mfa_recovery_codes            IS 'Single-use backup codes for MFA recovery. Generated in batches, each code can only be used once.';
COMMENT ON COLUMN auth.mfa_recovery_codes.code_hash  IS 'Hashed recovery code (like passwords — never store plaintext).';
COMMENT ON COLUMN auth.mfa_recovery_codes.is_used    IS 'TRUE after the code has been consumed. Cannot be reused.';
COMMENT ON COLUMN auth.mfa_recovery_codes.used_at    IS 'Timestamp when the code was consumed. NULL if unused.';

CREATE INDEX IF NOT EXISTS ix_auth_mfa_recovery_codes_user_id
    ON auth.mfa_recovery_codes (user_id);

COMMIT;
