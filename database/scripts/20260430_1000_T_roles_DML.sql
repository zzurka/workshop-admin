-- Migration: 20260430_1000_T_roles_DML.sql
-- Description: Seed default roles for the platform and tenants.
--              platform_admin is reserved for users with tenant_id IS NULL.
--              All other roles are intended for tenant-scoped users.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.roles (name, description) VALUES
    ('platform_admin',  'Platform-level super admin (no tenant). Manages tenants, subscription plans, and global codebooks.'),
    ('tenant_admin',    'Full administrative control within a tenant.'),
    ('manager',         'Operational management within a tenant: work orders, scheduling, reports.'),
    ('mechanic',        'Performs work: appointments, work orders, parts usage, time entries.'),
    ('receptionist',    'Front-desk operations: customers, vehicles, appointments, invoices.'),
    ('warehouse_clerk', 'Warehouse operations: parts catalog, stock levels, stock transactions.')
ON CONFLICT (name) DO NOTHING;

COMMIT;
