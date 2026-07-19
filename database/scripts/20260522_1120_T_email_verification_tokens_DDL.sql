-- Migration: 20260522_1120_T_email_verification_tokens_DDL.sql
-- Description: Stores single-use email-verification tokens. Only the SHA-256
--              hash of the raw token is persisted (raw is delivered once via
--              email and never stored). Same shape as password_reset_tokens.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.email_verification_tokens (
    id          UUID         NOT NULL DEFAULT uuidv7(),
    user_id     UUID         NOT NULL,
    token_hash  VARCHAR(255) NOT NULL,
    expires_at  TIMESTAMPTZ  NOT NULL,
    used_at     TIMESTAMPTZ,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_auth_email_verification_tokens             PRIMARY KEY (id),
    CONSTRAINT uq_auth_email_verification_tokens_token_hash  UNIQUE (token_hash),
    CONSTRAINT fk_auth_email_verification_tokens_user_id     FOREIGN KEY (user_id) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.email_verification_tokens             IS 'Single-use email-verification tokens. Immutable-style: created once, marked used once.';
COMMENT ON COLUMN auth.email_verification_tokens.token_hash  IS 'SHA-256 (Base64url) of the raw token. The raw value is delivered once via email and never persisted.';
COMMENT ON COLUMN auth.email_verification_tokens.expires_at  IS 'Absolute expiry; tokens older than this are rejected even if unused.';
COMMENT ON COLUMN auth.email_verification_tokens.used_at     IS 'NULL = unused. Set when the token is consumed; consuming it also stamps auth.users.email_verified_at.';

CREATE INDEX IF NOT EXISTS ix_auth_email_verification_tokens_user_id
    ON auth.email_verification_tokens (user_id);

COMMIT;
