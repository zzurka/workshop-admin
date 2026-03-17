-- Migration: 20260317_1150_T_role_permissions_DDL.sql
-- Description: Create the auth.role_permissions junction table.
--              Assigns permissions to roles.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.role_permissions (
    role_id       UUID        NOT NULL,
    permission_id UUID        NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by    UUID,
    updated_at    TIMESTAMPTZ,
    updated_by    UUID,
    is_deleted    BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_role_permissions             PRIMARY KEY (role_id, permission_id),
    CONSTRAINT fk_auth_role_permissions_role_id     FOREIGN KEY (role_id)       REFERENCES auth.roles(id),
    CONSTRAINT fk_auth_role_permissions_permission_id FOREIGN KEY (permission_id) REFERENCES auth.permissions(id),
    CONSTRAINT fk_auth_role_permissions_created_by  FOREIGN KEY (created_by)    REFERENCES auth.users(id),
    CONSTRAINT fk_auth_role_permissions_updated_by  FOREIGN KEY (updated_by)    REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.role_permissions            IS 'Assigns permissions to roles. Soft-deleted rows are kept for audit purposes.';
COMMENT ON COLUMN auth.role_permissions.updated_at IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.role_permissions.is_deleted IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_role_permissions_permission_id
    ON auth.role_permissions (permission_id);

CREATE INDEX IF NOT EXISTS ix_auth_role_permissions_is_deleted
    ON auth.role_permissions (is_deleted);

COMMIT;
