-- Migration: 20260522_1000_T_external_logins_DDL.sql
-- Description: Links external identity-provider accounts (Google, Microsoft,
--              generic OIDC) to existing auth.users rows. A row is created the
--              first time a user signs in via a given provider; subsequent
--              sign-ins for the same (provider, subject) refresh
--              last_login_at. Append-only-style: no updated_at/updated_by;
--              unlink is a hard DELETE. login_history retains the audit trail.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-22
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.external_logins (
    id             UUID         NOT NULL DEFAULT uuidv7(),
    user_id        UUID         NOT NULL,
    provider       VARCHAR(50)  NOT NULL,
    subject        VARCHAR(255) NOT NULL,
    email          VARCHAR(320),
    linked_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    last_login_at  TIMESTAMPTZ,
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by     UUID,

    CONSTRAINT pk_auth_external_logins                       PRIMARY KEY (id),
    CONSTRAINT uq_auth_external_logins_provider_subject      UNIQUE (provider, subject),
    CONSTRAINT uq_auth_external_logins_user_id_provider      UNIQUE (user_id, provider),
    CONSTRAINT fk_auth_external_logins_user_id               FOREIGN KEY (user_id) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.external_logins                IS 'Provider identities (Google, Microsoft, OIDC) linked to local users. One row per (user, provider).';
COMMENT ON COLUMN auth.external_logins.provider       IS 'Stable provider code: ''google'', ''microsoft'', or ''oidc:<custom>''.';
COMMENT ON COLUMN auth.external_logins.subject        IS 'The provider''s stable user identifier (OIDC ''sub'' claim). Must not change for the same user.';
COMMENT ON COLUMN auth.external_logins.email          IS 'Email captured from the provider at link time. For audit only — matching is by (provider, subject).';
COMMENT ON COLUMN auth.external_logins.linked_at      IS 'When the user-provider link was first established.';
COMMENT ON COLUMN auth.external_logins.last_login_at  IS 'Updated on every successful external login through this provider.';

CREATE INDEX IF NOT EXISTS ix_auth_external_logins_user_id
    ON auth.external_logins (user_id);

COMMIT;
