-- Migration: 20260522_1100_T_password_reset_tokens_DDL.sql
-- Description: Stores single-use password-reset tokens. Only the SHA-256 hash
--              of the raw token is persisted (raw is delivered once via email
--              and never stored). Append-only-style: a row is created once and
--              at most marked used once.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-22
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.password_reset_tokens (
    id          UUID         NOT NULL DEFAULT uuidv7(),
    user_id     UUID         NOT NULL,
    token_hash  VARCHAR(255) NOT NULL,
    expires_at  TIMESTAMPTZ  NOT NULL,
    used_at     TIMESTAMPTZ,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_auth_password_reset_tokens               PRIMARY KEY (id),
    CONSTRAINT uq_auth_password_reset_tokens_token_hash    UNIQUE (token_hash),
    CONSTRAINT fk_auth_password_reset_tokens_user_id       FOREIGN KEY (user_id) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.password_reset_tokens             IS 'Single-use password-reset tokens. Immutable-style: created once, marked used once.';
COMMENT ON COLUMN auth.password_reset_tokens.token_hash  IS 'SHA-256 (Base64url) of the raw token. The raw value is delivered once via email and never persisted.';
COMMENT ON COLUMN auth.password_reset_tokens.expires_at  IS 'Absolute expiry; tokens older than this are rejected even if unused.';
COMMENT ON COLUMN auth.password_reset_tokens.used_at     IS 'NULL = unused. Set when the token is consumed; a token is single-use.';

CREATE INDEX IF NOT EXISTS ix_auth_password_reset_tokens_user_id
    ON auth.password_reset_tokens (user_id);

COMMIT;
