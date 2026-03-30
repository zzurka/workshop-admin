-- Migration: 20260331_0910_T_permissions_roles_DML.sql
-- Description: Seed role management permissions.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    ('roles:read',               'roles', 'read',               'View roles and their permissions'),
    ('roles:create',             'roles', 'create',             'Create new roles'),
    ('roles:update',             'roles', 'update',             'Edit role name and description'),
    ('roles:delete',             'roles', 'delete',             'Soft-delete roles'),
    ('roles:assign_permissions', 'roles', 'assign_permissions', 'Assign/remove permissions to/from roles')
ON CONFLICT (name) DO NOTHING;

COMMIT;
