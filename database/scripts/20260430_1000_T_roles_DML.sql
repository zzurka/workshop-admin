-- Migration: 20260430_1000_T_roles_DML.sql
-- Description: Seed default roles for the platform and tenants.
--              platform_admin is reserved for users with tenant_id IS NULL.
--              All other roles are intended for tenant-scoped users.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.roles (name, scope, description, is_system) VALUES
    ('platform_admin',  'platform', 'Platform-level super admin (no tenant). Manages tenants, subscription plans, and global codebooks.', TRUE),
    ('tenant_admin',    'tenant',   'Full administrative control within a tenant.', TRUE),
    ('manager',         'tenant',   'Operational management within a tenant: work orders, scheduling, reports.', TRUE),
    ('mechanic',        'tenant',   'Performs work: appointments, work orders, parts usage, time entries.', TRUE),
    ('receptionist',    'tenant',   'Front-desk operations: customers, vehicles, appointments, invoices.', TRUE),
    ('warehouse_clerk', 'tenant',   'Warehouse operations: parts catalog, stock levels, stock transactions.', TRUE)
ON CONFLICT (tenant_id, name) WHERE NOT is_deleted DO NOTHING;

COMMIT;
