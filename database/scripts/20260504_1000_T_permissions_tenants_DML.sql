-- Migration: 20260504_1000_T_permissions_tenants_DML.sql
-- Description: Seed tenant management permissions. Platform-level only —
--              granted exclusively to platform_admin.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-04
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description, scope) VALUES
    ('tenants:read',           'tenants', 'read',           'View tenant list and details',                                                             'platform'),
    ('tenants:create',         'tenants', 'create',         'Create new tenants (atomic — provisions initial tenant_admin user in same transaction)',   'platform'),
    ('tenants:update',         'tenants', 'update',         'Edit tenant details',                                                                      'platform'),
    ('tenants:delete',         'tenants', 'delete',         'Soft-delete tenants',                                                                      'platform'),
    ('tenants:deactivate',     'tenants', 'deactivate',     'Activate/deactivate tenants',                                                              'platform'),
    ('tenants:manage_admins',  'tenants', 'manage_admins',  'List, add, deactivate, and remove tenant_admin users in a tenant',                         'platform')
ON CONFLICT (name) DO NOTHING;

COMMIT;
