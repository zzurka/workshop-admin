-- Migration: 20260317_1160_T_user_tenants_DDL.sql
-- Description: Create the auth.user_tenants junction table. A user can belong
--              to multiple tenants (e.g. a consultant working across workshops).
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

COMMENT ON TABLE  auth.user_tenants             IS 'Maps users to tenants they belong to. A user may belong to multiple tenants.';
COMMENT ON COLUMN auth.user_tenants.is_active   IS 'FALSE = membership suspended for this tenant (user can still access others).';
COMMENT ON COLUMN auth.user_tenants.updated_at  IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.user_tenants.is_deleted  IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_user_tenants_tenant_id
    ON auth.user_tenants (tenant_id);

CREATE INDEX IF NOT EXISTS ix_auth_user_tenants_is_deleted
    ON auth.user_tenants (is_deleted);

COMMIT;
