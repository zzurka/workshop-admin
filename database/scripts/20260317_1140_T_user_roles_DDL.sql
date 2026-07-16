-- Migration: 20260317_1140_T_user_roles_DDL.sql
-- Description: Create the auth.user_roles table. Assigns roles to users, scoped
--              to a tenant membership. tenant_id IS NULL is a platform-scope
--              assignment (platform_admin); tenant_id set means the role
--              applies within that tenant only — the same user can be
--              'mechanic' in tenant A and a plain customer in tenant B.
--              A composite FK to auth.user_tenants guarantees tenant-scoped
--              assignments only exist for real memberships.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.user_roles (
    id         UUID        NOT NULL DEFAULT uuidv7(),
    user_id    UUID        NOT NULL,
    role_id    UUID        NOT NULL,
    tenant_id  UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID,
    is_deleted BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_user_roles                    PRIMARY KEY (id),
    CONSTRAINT fk_auth_user_roles_user_id            FOREIGN KEY (user_id)   REFERENCES auth.users(id),
    CONSTRAINT fk_auth_user_roles_role_id            FOREIGN KEY (role_id)   REFERENCES auth.roles(id),
    CONSTRAINT fk_auth_user_roles_tenant_id          FOREIGN KEY (tenant_id) REFERENCES tenant.tenants(id),
    CONSTRAINT fk_auth_user_roles_user_id_tenant_id  FOREIGN KEY (user_id, tenant_id) REFERENCES auth.user_tenants(user_id, tenant_id),
    CONSTRAINT fk_auth_user_roles_created_by         FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_user_roles_updated_by         FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.user_roles            IS 'Assigns roles to users per tenant membership. tenant_id IS NULL = platform-scope assignment (only for scope=platform roles). Soft-deleted rows are kept for audit purposes. Consistency between roles.tenant_id and user_roles.tenant_id (a tenant''s custom role may only be assigned within that tenant) is enforced at application level.';
COMMENT ON COLUMN auth.user_roles.id         IS 'UUID v7 primary key (time-ordered). Surrogate key because tenant_id is nullable and cannot participate in a composite PK.';
COMMENT ON COLUMN auth.user_roles.user_id    IS 'The user receiving the role.';
COMMENT ON COLUMN auth.user_roles.role_id    IS 'The role being assigned.';
COMMENT ON COLUMN auth.user_roles.tenant_id  IS 'NULL = platform-scope assignment (platform_admin). Set = the tenant within which the role applies; (user_id, tenant_id) must be an auth.user_tenants membership (composite FK skips the check when tenant_id IS NULL).';
COMMENT ON COLUMN auth.user_roles.created_by IS 'User who created this record. NULL for bootstrap/seed records.';
COMMENT ON COLUMN auth.user_roles.updated_at IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.user_roles.is_deleted IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

-- Partial unique index instead of a table constraint so revoked (soft-deleted)
-- assignments stay in history and the same (user, role, tenant) can be
-- re-assigned later. NULLS NOT DISTINCT keeps platform-scope assignments
-- (tenant_id IS NULL) unique among themselves.
CREATE UNIQUE INDEX IF NOT EXISTS uq_auth_user_roles_user_role_tenant
    ON auth.user_roles (user_id, role_id, tenant_id) NULLS NOT DISTINCT
    WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_auth_user_roles_user_id
    ON auth.user_roles (user_id);

CREATE INDEX IF NOT EXISTS ix_auth_user_roles_role_id
    ON auth.user_roles (role_id);

CREATE INDEX IF NOT EXISTS ix_auth_user_roles_tenant_id
    ON auth.user_roles (tenant_id);

CREATE INDEX IF NOT EXISTS ix_auth_user_roles_is_deleted
    ON auth.user_roles (is_deleted);

COMMIT;
