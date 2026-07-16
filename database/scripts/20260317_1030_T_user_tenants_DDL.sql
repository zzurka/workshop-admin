-- Migration: 20260317_1030_T_user_tenants_DDL.sql
-- Description: Create the auth.user_tenants membership table. Maps users to the
--              tenants they can access (M:N). auth.users is a global identity
--              (one row per person); this table records which workshops that
--              person can enter. A staff member typically has one membership;
--              a customer servicing vehicles at multiple workshops has one
--              membership per workshop. Platform super admins have no
--              memberships — they are recognized by holding a platform-scope
--              role (auth.user_roles.tenant_id IS NULL), not by tenant linkage.
--              Referenced by composite FKs from customer.customers,
--              hr.employees, and auth.user_roles so those rows can only point
--              at (user, tenant) pairs that are actual memberships.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.user_tenants (
    user_id    UUID        NOT NULL,
    tenant_id  UUID        NOT NULL,
    is_active  BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID,
    is_deleted BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_user_tenants             PRIMARY KEY (user_id, tenant_id),
    CONSTRAINT fk_auth_user_tenants_user_id     FOREIGN KEY (user_id)    REFERENCES auth.users(id),
    CONSTRAINT fk_auth_user_tenants_tenant_id   FOREIGN KEY (tenant_id)  REFERENCES tenant.tenants(id),
    CONSTRAINT fk_auth_user_tenants_created_by  FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_user_tenants_updated_by  FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.user_tenants            IS 'Tenant memberships: which tenants a user can access. One row per (user, tenant). The authoritative source for login tenant selection and the FK target that keeps customers, employees, and role assignments inside real memberships. Re-joining a tenant un-deletes the existing row (natural PK).';
COMMENT ON COLUMN auth.user_tenants.user_id    IS 'The user (global identity in auth.users).';
COMMENT ON COLUMN auth.user_tenants.tenant_id  IS 'The tenant (workshop) the user can access.';
COMMENT ON COLUMN auth.user_tenants.is_active  IS 'FALSE = access to this tenant suspended, without affecting the user''s other memberships or the account itself.';
COMMENT ON COLUMN auth.user_tenants.created_by IS 'User who created this record. NULL for bootstrap/seed records.';
COMMENT ON COLUMN auth.user_tenants.updated_at IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.user_tenants.is_deleted IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_user_tenants_tenant_id
    ON auth.user_tenants (tenant_id);

CREATE INDEX IF NOT EXISTS ix_auth_user_tenants_is_deleted
    ON auth.user_tenants (is_deleted);

COMMIT;
