-- Migration: 20260331_0930_T_permissions_vehicles_DML.sql
-- Description: Seed vehicle management permissions.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    ('vehicles:read',   'vehicles', 'read',   'View vehicle list and details'),
    ('vehicles:create', 'vehicles', 'create', 'Create new vehicles'),
    ('vehicles:update', 'vehicles', 'update', 'Edit vehicle details'),
    ('vehicles:delete', 'vehicles', 'delete', 'Soft-delete vehicles')
ON CONFLICT (name) DO NOTHING;

COMMIT;
