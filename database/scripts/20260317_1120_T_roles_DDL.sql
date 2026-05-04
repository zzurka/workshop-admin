-- Migration: 20260317_1120_T_roles_DDL.sql
-- Description: Create the auth.roles table. Roles group permissions and are
--              assigned to users.
--              tenant_id IS NULL means a global role managed by platform_admin
--              (e.g. platform_admin, tenant_admin, manager). 
--              tenant_id NOT NULL means a tenant-scoped custom role, managed by 
--              that tenant's tenant_admin.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.roles (
    id          UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id   UUID,
    name        VARCHAR(100) NOT NULL,
    scope       VARCHAR(20)  NOT NULL DEFAULT 'tenant',
    description TEXT,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by  UUID,
    updated_at  TIMESTAMPTZ,
    updated_by  UUID,
    is_deleted  BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_roles                PRIMARY KEY (id),
    CONSTRAINT uq_auth_roles_tenant_id_name UNIQUE NULLS NOT DISTINCT (tenant_id, name),
    CONSTRAINT ck_auth_roles_scope          CHECK (scope IN ('platform', 'tenant')),
    CONSTRAINT ck_auth_roles_platform_scope CHECK (scope = 'tenant' OR tenant_id IS NULL),
    CONSTRAINT fk_auth_roles_tenant_id      FOREIGN KEY (tenant_id)  REFERENCES tenant.tenants(id),
    CONSTRAINT fk_auth_roles_created_by     FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_roles_updated_by     FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.roles             IS 'Named roles that group permissions, assigned to users. Global roles have tenant_id IS NULL; tenant-scoped custom roles have tenant_id set.';
COMMENT ON COLUMN auth.roles.tenant_id   IS 'NULL = global role managed by platform_admin. Set = tenant-scoped custom role managed by that tenant''s tenant_admin.';
COMMENT ON COLUMN auth.roles.scope       IS '''platform'' = assignable only to platform users (auth.users.tenant_id IS NULL); only contains scope=platform permissions. ''tenant'' = assignable to tenant users; only contains scope=tenant permissions. Custom roles created by tenant_admins are always scope=tenant.';
COMMENT ON COLUMN auth.roles.updated_at  IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.roles.is_deleted  IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_roles_tenant_id
    ON auth.roles (tenant_id);

CREATE INDEX IF NOT EXISTS ix_auth_roles_is_deleted
    ON auth.roles (is_deleted);

COMMIT;
