-- Migration: 20260331_0940_T_permissions_employees_DML.sql
-- Description: Seed employee, time entry, leave request, and leave balance
--              permissions. Self-access (own time entries, own leave requests)
--              is handled in application logic.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    -- Employees
    ('employees:read',       'employees', 'read',       'View employee list and details'),
    ('employees:create',     'employees', 'create',     'Create new employees'),
    ('employees:update',     'employees', 'update',     'Edit employee details'),
    ('employees:delete',     'employees', 'delete',     'Soft-delete employees'),
    ('employees:deactivate', 'employees', 'deactivate', 'Activate/deactivate employees'),

    -- Time entries
    ('time_entries:read',   'time_entries', 'read',   'View time entries'),
    ('time_entries:create', 'time_entries', 'create', 'Clock in/out'),
    ('time_entries:update', 'time_entries', 'update', 'Edit time entries'),
    ('time_entries:delete', 'time_entries', 'delete', 'Soft-delete time entries'),

    -- Leave requests
    ('leave_requests:read',    'leave_requests', 'read',    'View leave requests'),
    ('leave_requests:create',  'leave_requests', 'create',  'Submit leave requests'),
    ('leave_requests:update',  'leave_requests', 'update',  'Edit leave requests'),
    ('leave_requests:delete',  'leave_requests', 'delete',  'Soft-delete leave requests'),
    ('leave_requests:approve', 'leave_requests', 'approve', 'Approve or reject leave requests'),

    -- Leave balances
    ('leave_balances:read',   'leave_balances', 'read',   'View leave balances'),
    ('leave_balances:manage', 'leave_balances', 'manage', 'Create and adjust leave balances')
ON CONFLICT (name) DO NOTHING;

COMMIT;
