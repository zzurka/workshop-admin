-- Migration: 20260317_1010_T_users_DDL.sql
-- Description: Create the auth.users table. Stores login credentials, profile,
--              and optional Active Directory linkage for all principals
--              (staff, admins, future customer portal users).
--              Tenant membership is managed via auth.user_tenants (not a column here).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.users (
    id            UUID         NOT NULL DEFAULT uuid_generate_v7(),
    email         VARCHAR(255) NOT NULL,
    password_hash TEXT         NOT NULL,
    first_name    VARCHAR(100),
    last_name     VARCHAR(100),
    ad_object_id  UUID,
    ad_upn        VARCHAR(255),
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by    UUID,
    updated_at    TIMESTAMPTZ,
    updated_by    UUID,
    is_deleted    BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_users               PRIMARY KEY (id),
    CONSTRAINT uq_auth_users_email         UNIQUE (email),
    CONSTRAINT uq_auth_users_ad_object_id  UNIQUE (ad_object_id),
    CONSTRAINT fk_auth_users_created_by    FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_users_updated_by    FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.users                IS 'All principals that can authenticate: staff, admins, and future customer portal users.';
COMMENT ON COLUMN auth.users.id             IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN auth.users.email          IS 'Unique login identifier.';
COMMENT ON COLUMN auth.users.password_hash  IS 'Hashed password (bcrypt or Argon2). Never store plaintext.';
COMMENT ON COLUMN auth.users.ad_object_id  IS 'Active Directory objectGUID — immutable unique identifier. NULL for non-AD users.';
COMMENT ON COLUMN auth.users.ad_upn        IS 'Active Directory User Principal Name (e.g. jsmith@domain.local). Can change if user is renamed. NULL for non-AD users.';
COMMENT ON COLUMN auth.users.is_active      IS 'FALSE = account suspended (still exists, cannot log in).';
COMMENT ON COLUMN auth.users.created_by     IS 'User who created this record. NULL for bootstrap/seed records (self-referential).';
COMMENT ON COLUMN auth.users.updated_at     IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.users.is_deleted     IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_users_email
    ON auth.users (email);

CREATE INDEX IF NOT EXISTS ix_auth_users_is_deleted
    ON auth.users (is_deleted);

COMMIT;
