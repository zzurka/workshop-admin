-- Migration: 20260331_0900_T_permissions_users_DML.sql
-- Description: Seed user management permissions.
--              Self-access (own profile) is handled in application logic.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    ('users:read',           'users', 'read',           'View user list and profiles'),
    ('users:create',         'users', 'create',         'Create new user accounts'),
    ('users:update',         'users', 'update',         'Edit user profiles and settings'),
    ('users:delete',         'users', 'delete',         'Soft-delete user accounts'),
    ('users:deactivate',     'users', 'deactivate',     'Activate/deactivate user accounts'),
    ('users:reset_password', 'users', 'reset_password', 'Reset another user''s password'),
    ('users:assign_roles',   'users', 'assign_roles',   'Assign/remove roles to/from users')
ON CONFLICT (name) DO NOTHING;

COMMIT;
