-- Migration: 20260317_1130_T_permissions_DDL.sql
-- Description: Create the auth.permissions table. Each permission represents
--              a specific action on a resource (e.g. workshop:read, invoice:write).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS auth.permissions (
    id          UUID         NOT NULL DEFAULT uuidv7(),
    name        VARCHAR(100) NOT NULL,
    resource    VARCHAR(100) NOT NULL,
    action      VARCHAR(50)  NOT NULL,
    scope       VARCHAR(20)  NOT NULL DEFAULT 'tenant',
    description TEXT,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by  UUID,
    updated_at  TIMESTAMPTZ,
    updated_by  UUID,
    is_deleted  BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_auth_permissions            PRIMARY KEY (id),
    CONSTRAINT uq_auth_permissions_name       UNIQUE (name),
    CONSTRAINT ck_auth_permissions_scope      CHECK (scope IN ('platform', 'tenant')),
    CONSTRAINT fk_auth_permissions_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_auth_permissions_updated_by FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  auth.permissions              IS 'Fine-grained permissions: a specific action on a resource.';
COMMENT ON COLUMN auth.permissions.name         IS 'Unique permission key, e.g. ''workshop:read'', ''invoice:write''.';
COMMENT ON COLUMN auth.permissions.resource     IS 'The resource being protected, e.g. ''workshop'', ''vehicle'', ''invoice''.';
COMMENT ON COLUMN auth.permissions.action       IS 'The allowed operation, e.g. ''read'', ''write'', ''delete''.';
COMMENT ON COLUMN auth.permissions.scope        IS '''platform'' = assignable only to global roles by platform_admin (e.g. tenants:*, subscription_plans:*, codebook:manage). ''tenant'' = assignable to any role.';
COMMENT ON COLUMN auth.permissions.updated_at   IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN auth.permissions.is_deleted   IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_auth_permissions_resource
    ON auth.permissions (resource);

CREATE INDEX IF NOT EXISTS ix_auth_permissions_is_deleted
    ON auth.permissions (is_deleted);

COMMIT;
