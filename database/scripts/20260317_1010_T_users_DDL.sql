-- Migration: 20260317_1010_T_users_DDL.sql
-- Description: Create the auth.users table. Stores login credentials, profile,
--              Active Directory linkage, and MFA configuration for all principals
--              (staff, admins, future customer portal users).
--              tenant_id is NULL for platform-level super admins.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.users (
    id            UUID         NOT NULL DEFAULT uuidv7(),
    email         VARCHAR(255) NOT NULL,
    password_hash TEXT,
    first_name    VARCHAR(100),
    last_name     VARCHAR(100),
	tenant_id     UUID,
    ad_object_id  UUID,
    ad_upn        VARCHAR(255),
    phone_number  VARCHAR(50),
    mfa_enabled   BOOLEAN      NOT NULL DEFAULT FALSE,
    mfa_method    VARCHAR(20),
    mfa_secret    TEXT,
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
    -- tenant_id FK added in 20260317_1020_T_users_tenants_fk_DDL.sql (circular dependency)
);

COMMENT ON TABLE  auth.users                IS 'All principals that can authenticate: staff, admins, and future customer portal users.';
COMMENT ON COLUMN auth.users.id             IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN auth.users.email          IS 'Unique login identifier.';
COMMENT ON COLUMN auth.users.password_hash  IS 'Hashed password (bcrypt or Argon2). Never store plaintext. NULL for external auth (AD, Google, etc.).';
COMMENT ON COLUMN auth.users.ad_object_id   IS 'Active Directory objectGUID — immutable unique identifier. NULL for non-AD users.';
COMMENT ON COLUMN auth.users.ad_upn        	IS 'Active Directory User Principal Name (e.g. jsmith@domain.local). Can change if user is renamed. NULL for non-AD users.';
COMMENT ON COLUMN auth.users.phone_number  	IS 'Phone number for SMS-based MFA and user profile.';
COMMENT ON COLUMN auth.users.mfa_enabled   	IS 'TRUE if two-factor authentication is active for this user.';
COMMENT ON COLUMN auth.users.mfa_method    	IS 'Active MFA method: totp, sms, or email. NULL if MFA not configured.';
COMMENT ON COLUMN auth.users.mfa_secret    	IS 'Encrypted TOTP shared secret for authenticator apps. Must be AES-256 encrypted at application level, never plaintext.';
COMMENT ON COLUMN auth.users.tenant_id      IS 'FK to tenant.tenants. NULL for platform-level super admins who are not bound to any tenant.';
COMMENT ON COLUMN auth.users.is_active      IS 'FALSE = account suspended (still exists, cannot log in).';
COMMENT ON COLUMN auth.users.created_by     IS 'User who created this record. NULL for bootstrap/seed records (self-referential).';
COMMENT ON COLUMN auth.users.updated_at     IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.users.is_deleted     IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_users_email
    ON auth.users (email);

CREATE INDEX IF NOT EXISTS ix_auth_users_tenant_id
    ON auth.users (tenant_id);

CREATE INDEX IF NOT EXISTS ix_auth_users_is_deleted
    ON auth.users (is_deleted);

COMMIT;
