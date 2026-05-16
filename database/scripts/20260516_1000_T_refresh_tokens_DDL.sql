-- Migration: 20260516_1000_T_refresh_tokens_DDL.sql
-- Description: Create the auth.refresh_tokens table. Stores hashed refresh
--              tokens used to obtain new JWT access tokens. The raw token is
--              never stored — only its SHA-256 hash. Rotation creates a new
--              row and revokes the old one (revoked_at + replaced_by_token_id),
--              which enables reuse / theft detection. This is an
--              immutable-style event-log table: a row is created once and at
--              most revoked once; no audit columns are needed.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-16
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.refresh_tokens (
    id                   UUID         NOT NULL DEFAULT uuidv7(),
    user_id              UUID         NOT NULL,
    token_hash           VARCHAR(255) NOT NULL,
    expires_at           TIMESTAMPTZ  NOT NULL,
    revoked_at           TIMESTAMPTZ,
    replaced_by_token_id UUID,
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_auth_refresh_tokens                       PRIMARY KEY (id),
    CONSTRAINT uq_auth_refresh_tokens_token_hash            UNIQUE (token_hash),
    CONSTRAINT fk_auth_refresh_tokens_user_id               FOREIGN KEY (user_id)              REFERENCES auth.users(id),
    CONSTRAINT fk_auth_refresh_tokens_replaced_by_token_id  FOREIGN KEY (replaced_by_token_id) REFERENCES auth.refresh_tokens(id)
);

COMMENT ON TABLE  auth.refresh_tokens                      IS 'Hashed refresh tokens for JWT session continuation. Immutable-style: created once, revoked at most once. The raw token is returned to the client only once and never persisted.';
COMMENT ON COLUMN auth.refresh_tokens.token_hash           IS 'SHA-256 hash (Base64) of the raw refresh token. The raw value is never stored.';
COMMENT ON COLUMN auth.refresh_tokens.expires_at           IS 'Absolute expiry of this refresh token.';
COMMENT ON COLUMN auth.refresh_tokens.revoked_at           IS 'NULL = active. Set when the token is rotated, logged out, or revoked due to reuse / theft detection.';
COMMENT ON COLUMN auth.refresh_tokens.replaced_by_token_id IS 'On rotation, points to the new token that superseded this one. Enables reuse detection.';

CREATE INDEX IF NOT EXISTS ix_auth_refresh_tokens_user_id
    ON auth.refresh_tokens (user_id);

COMMIT;
