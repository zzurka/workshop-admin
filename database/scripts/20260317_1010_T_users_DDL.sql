-- Migration: 20260317_1010_T_users_DDL.sql
-- Description: Create the auth.users table. Stores login credentials, profile,
--              Active Directory linkage, and MFA configuration for all principals
--              (staff, admins, customer portal users). One row per person — a
--              global identity. Tenant access is granted through
--              auth.user_tenants memberships (M:N). Platform super admins are
--              recognized by holding a platform-scope role, not by tenant linkage.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.users (
    id                  UUID         NOT NULL DEFAULT uuidv7(),
    email               VARCHAR(320) NOT NULL,
    email_verified_at   TIMESTAMPTZ,
    password_hash       TEXT,
    first_name          VARCHAR(100),
    last_name           VARCHAR(100),
    ad_object_id        UUID,
    ad_upn              VARCHAR(255),
    phone_number        VARCHAR(50),
    mfa_enabled         BOOLEAN      NOT NULL DEFAULT FALSE,
    mfa_method          VARCHAR(20),
    mfa_secret          TEXT,
    failed_login_count  SMALLINT     NOT NULL DEFAULT 0,
    locked_until        TIMESTAMPTZ,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_users               PRIMARY KEY (id),
    CONSTRAINT uq_auth_users_ad_object_id  UNIQUE (ad_object_id),
    CONSTRAINT fk_auth_users_created_by    FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_users_updated_by    FOREIGN KEY (updated_by) REFERENCES auth.users(id),
    CONSTRAINT ck_auth_users_mfa_method    CHECK (mfa_method IS NULL OR mfa_method IN ('totp', 'sms', 'email')),
    CONSTRAINT ck_auth_users_mfa_enabled   CHECK (NOT mfa_enabled OR mfa_method IS NOT NULL)
);

COMMENT ON TABLE  auth.users                     IS 'All principals that can authenticate: staff, admins, and customer portal users. Global identity — one row per person; tenant access lives in auth.user_tenants.';
COMMENT ON COLUMN auth.users.id                  IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN auth.users.email               IS 'Login identifier. VARCHAR(320) matches the RFC 5321 maximum, consistent with external_logins/email_outbox. Uniqueness is a partial index on LOWER(email) (see uq_auth_users_email below), not a plain UNIQUE.';
COMMENT ON COLUMN auth.users.email_verified_at   IS 'NULL = unverified. Set when the user confirms via auth.email_verification_tokens. Required before self-registered customer accounts get full access; staff-created accounts verify through their first login link.';
COMMENT ON COLUMN auth.users.password_hash       IS 'Hashed password (bcrypt or Argon2). Never store plaintext. NULL for external auth (AD, Google, etc.).';
COMMENT ON COLUMN auth.users.ad_object_id        IS 'Active Directory objectGUID — immutable unique identifier. NULL for non-AD users.';
COMMENT ON COLUMN auth.users.ad_upn        	  IS 'Active Directory User Principal Name (e.g. jsmith@domain.local). Can change if user is renamed. NULL for non-AD users.';
COMMENT ON COLUMN auth.users.phone_number  	  IS 'Phone number for SMS-based MFA and user profile.';
COMMENT ON COLUMN auth.users.mfa_enabled   	  IS 'TRUE if two-factor authentication is active for this user. CHECK ck_auth_users_mfa_enabled requires mfa_method to be set whenever this is TRUE.';
COMMENT ON COLUMN auth.users.mfa_method    	  IS 'Active MFA method: totp, sms, or email. NULL if MFA not configured.';
COMMENT ON COLUMN auth.users.mfa_secret    	  IS 'Encrypted TOTP shared secret for authenticator apps. Must be AES-256 encrypted at application level, never plaintext.';
COMMENT ON COLUMN auth.users.failed_login_count  IS 'Consecutive failed login attempts. Application increments on failure, resets to 0 on success. Lockout threshold is application config, not a DB constraint.';
COMMENT ON COLUMN auth.users.locked_until        IS 'NULL = not locked. Set by the application after failed_login_count crosses its configured threshold; login is refused while NOW() < locked_until.';
COMMENT ON COLUMN auth.users.is_active           IS 'FALSE = account suspended (still exists, cannot log in).';
COMMENT ON COLUMN auth.users.created_by          IS 'User who created this record. NULL for bootstrap/seed records (self-referential).';
COMMENT ON COLUMN auth.users.updated_at          IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.users.is_deleted          IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

-- Partial unique index instead of a table constraint so soft-deleted users
-- release their email for reuse (consistent with auth.roles); LOWER() makes
-- it case-insensitive — the application normalizes to lowercase on write.
CREATE UNIQUE INDEX IF NOT EXISTS uq_auth_users_email
    ON auth.users (LOWER(email)) WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_auth_users_is_deleted
    ON auth.users (is_deleted);

COMMIT;
