-- Migration: 20260317_1120_T_roles_DDL.sql
-- Description: Create the auth.roles table. Roles group permissions and are
--              assigned to users (e.g. admin, staff, customer_portal).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.roles (
    id          UUID         NOT NULL DEFAULT uuidv7(),
    name        VARCHAR(100) NOT NULL,
    description TEXT,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by  UUID,
    updated_at  TIMESTAMPTZ,
    updated_by  UUID,
    is_deleted  BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_roles            PRIMARY KEY (id),
    CONSTRAINT uq_auth_roles_name       UNIQUE (name),
    CONSTRAINT fk_auth_roles_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_roles_updated_by FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.roles             IS 'Named roles that group permissions, assigned to users (e.g. admin, staff, customer_portal).';
COMMENT ON COLUMN auth.roles.updated_at  IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.roles.is_deleted  IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_roles_is_deleted
    ON auth.roles (is_deleted);

COMMIT;
