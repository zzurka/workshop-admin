-- Migration: 20260317_1140_T_user_roles_DDL.sql
-- Description: Create the auth.user_roles junction table. Assigns roles to users.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.user_roles (
    user_id    UUID        NOT NULL,
    role_id    UUID        NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ,
    updated_by UUID,
    is_deleted BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_user_roles             PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_auth_user_roles_user_id     FOREIGN KEY (user_id)    REFERENCES auth.users(id),
    CONSTRAINT fk_auth_user_roles_role_id     FOREIGN KEY (role_id)    REFERENCES auth.roles(id),
    CONSTRAINT fk_auth_user_roles_created_by  FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_user_roles_updated_by  FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.user_roles            IS 'Assigns roles to users. Roles are implicitly scoped to the user''s tenant. Soft-deleted rows are kept for audit purposes.';
COMMENT ON COLUMN auth.user_roles.updated_at IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.user_roles.is_deleted IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_user_roles_role_id
    ON auth.user_roles (role_id);

CREATE INDEX IF NOT EXISTS ix_auth_user_roles_is_deleted
    ON auth.user_roles (is_deleted);

COMMIT;
