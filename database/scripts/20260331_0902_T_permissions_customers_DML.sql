-- Migration: 20260331_0902_T_permissions_customers_DML.sql
-- Description: Seed customer management permissions.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    ('customers:read',   'customers', 'read',   'View customer list and details'),
    ('customers:create', 'customers', 'create', 'Create new customers'),
    ('customers:update', 'customers', 'update', 'Edit customer details'),
    ('customers:delete', 'customers', 'delete', 'Soft-delete customers')
ON CONFLICT (name) DO NOTHING;

COMMIT;
