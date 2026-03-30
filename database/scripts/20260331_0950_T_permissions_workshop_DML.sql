-- Migration: 20260331_0950_T_permissions_workshop_DML.sql
-- Description: Seed workshop permissions — appointments, work orders,
--              suppliers, and invoices.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    -- Appointments
    ('appointments:read',   'appointments', 'read',   'View appointments'),
    ('appointments:create', 'appointments', 'create', 'Create new appointments'),
    ('appointments:update', 'appointments', 'update', 'Edit appointments'),
    ('appointments:delete', 'appointments', 'delete', 'Soft-delete appointments'),

    -- Work orders
    ('work_orders:read',   'work_orders', 'read',   'View work orders and their parts'),
    ('work_orders:create', 'work_orders', 'create', 'Create work orders from appointments'),
    ('work_orders:update', 'work_orders', 'update', 'Edit work orders and manage parts'),
    ('work_orders:delete', 'work_orders', 'delete', 'Soft-delete work orders'),

    -- Suppliers
    ('suppliers:read',   'suppliers', 'read',   'View suppliers and contacts'),
    ('suppliers:create', 'suppliers', 'create', 'Create new suppliers'),
    ('suppliers:update', 'suppliers', 'update', 'Edit suppliers and contacts'),
    ('suppliers:delete', 'suppliers', 'delete', 'Soft-delete suppliers'),

    -- Invoices
    ('invoices:read',      'invoices', 'read',      'View invoices'),
    ('invoices:create',    'invoices', 'create',    'Create invoices'),
    ('invoices:update',    'invoices', 'update',    'Edit invoices and line items'),
    ('invoices:delete',    'invoices', 'delete',    'Soft-delete invoices'),
    ('invoices:issue',     'invoices', 'issue',     'Issue invoices to customers'),
    ('invoices:mark_paid', 'invoices', 'mark_paid', 'Mark invoices as paid')
ON CONFLICT (name) DO NOTHING;

COMMIT;
