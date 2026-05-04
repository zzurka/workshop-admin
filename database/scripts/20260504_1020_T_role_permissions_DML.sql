-- Migration: 20260504_1020_T_role_permissions_DML.sql
-- Description: Assign permissions to the seeded global roles. Resolves role and
--              permission IDs by name lookup so this script is independent of
--              UUID values. Only operates on global roles (tenant_id IS NULL).
--
--              Matrix summary:
--                platform_admin  — tenants:*, subscription_plans:*, codebook:manage,
--                                  and roles:* (so platform_admin can manage
--                                  global roles). No tenant business data.
--                tenant_admin    — full tenant-scoped admin: users:*, roles:*,
--                                  all tenant business data, codebook:read.
--                                  Service layer enforces:
--                                    * roles:* only acts on rows with
--                                      tenant_id = actor's tenant
--                                    * permissions assigned to custom roles
--                                      must have scope = 'tenant'
--                manager         — tenant_admin minus users:* and roles:*.
--                mechanic        — work_orders read/update only; reads on
--                                  customers/vehicles/parts/stock; self-clock
--                                  and self-leave. Cannot create stock
--                                  transactions (warehouse_clerk does that).
--                receptionist    — front-desk: customers/vehicles/appointments/
--                                  invoices full CRUD; self-clock and self-leave.
--                warehouse_clerk — parts_catalog/stock/stock_transactions full;
--                                  suppliers read; self-clock and self-leave.
--
--              Service layer enforces self-only access for time_entries,
--              leave_requests, and leave_balances reads/creates by non-managers.
--
-- Author: WorkshopAdmin Team
-- Date: 2026-05-05
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

WITH role_perm_pairs (role_name, permission_name) AS (
    VALUES
        -- platform_admin: platform-level + role/codebook management
        ('platform_admin', 'tenants:read'),
        ('platform_admin', 'tenants:create'),
        ('platform_admin', 'tenants:update'),
        ('platform_admin', 'tenants:delete'),
        ('platform_admin', 'tenants:deactivate'),
        ('platform_admin', 'tenants:manage_admins'),
        ('platform_admin', 'subscription_plans:read'),
        ('platform_admin', 'subscription_plans:create'),
        ('platform_admin', 'subscription_plans:update'),
        ('platform_admin', 'subscription_plans:delete'),
        ('platform_admin', 'codebook:read'),
        ('platform_admin', 'codebook:manage'),
        ('platform_admin', 'roles:read'),
        ('platform_admin', 'roles:create'),
        ('platform_admin', 'roles:update'),
        ('platform_admin', 'roles:delete'),
        ('platform_admin', 'roles:assign_permissions'),
        -- end platform_admin

        -- tenant_admin: full tenant-scoped admin (no codebook:manage)
        ('tenant_admin', 'users:read'),
        ('tenant_admin', 'users:create'),
        ('tenant_admin', 'users:update'),
        ('tenant_admin', 'users:delete'),
        ('tenant_admin', 'users:deactivate'),
        ('tenant_admin', 'users:reset_password'),
        ('tenant_admin', 'users:assign_roles'),
        ('tenant_admin', 'roles:read'),
        ('tenant_admin', 'roles:create'),
        ('tenant_admin', 'roles:update'),
        ('tenant_admin', 'roles:delete'),
        ('tenant_admin', 'roles:assign_permissions'),
        ('tenant_admin', 'customers:read'),
        ('tenant_admin', 'customers:create'),
        ('tenant_admin', 'customers:update'),
        ('tenant_admin', 'customers:delete'),
        ('tenant_admin', 'vehicles:read'),
        ('tenant_admin', 'vehicles:create'),
        ('tenant_admin', 'vehicles:update'),
        ('tenant_admin', 'vehicles:delete'),
        ('tenant_admin', 'employees:read'),
        ('tenant_admin', 'employees:create'),
        ('tenant_admin', 'employees:update'),
        ('tenant_admin', 'employees:delete'),
        ('tenant_admin', 'employees:deactivate'),
        ('tenant_admin', 'time_entries:read'),
        ('tenant_admin', 'time_entries:create'),
        ('tenant_admin', 'time_entries:update'),
        ('tenant_admin', 'time_entries:delete'),
        ('tenant_admin', 'leave_requests:read'),
        ('tenant_admin', 'leave_requests:create'),
        ('tenant_admin', 'leave_requests:update'),
        ('tenant_admin', 'leave_requests:delete'),
        ('tenant_admin', 'leave_requests:approve'),
        ('tenant_admin', 'leave_balances:read'),
        ('tenant_admin', 'leave_balances:manage'),
        ('tenant_admin', 'appointments:read'),
        ('tenant_admin', 'appointments:create'),
        ('tenant_admin', 'appointments:update'),
        ('tenant_admin', 'appointments:delete'),
        ('tenant_admin', 'work_orders:read'),
        ('tenant_admin', 'work_orders:create'),
        ('tenant_admin', 'work_orders:update'),
        ('tenant_admin', 'work_orders:delete'),
        ('tenant_admin', 'suppliers:read'),
        ('tenant_admin', 'suppliers:create'),
        ('tenant_admin', 'suppliers:update'),
        ('tenant_admin', 'suppliers:delete'),
        ('tenant_admin', 'invoices:read'),
        ('tenant_admin', 'invoices:create'),
        ('tenant_admin', 'invoices:update'),
        ('tenant_admin', 'invoices:delete'),
        ('tenant_admin', 'invoices:issue'),
        ('tenant_admin', 'invoices:mark_paid'),
        ('tenant_admin', 'expenses:read'),
        ('tenant_admin', 'expenses:create'),
        ('tenant_admin', 'expenses:update'),
        ('tenant_admin', 'expenses:delete'),
        ('tenant_admin', 'codebook:read'),
        ('tenant_admin', 'parts_catalog:read'),
        ('tenant_admin', 'parts_catalog:create'),
        ('tenant_admin', 'parts_catalog:update'),
        ('tenant_admin', 'parts_catalog:delete'),
        ('tenant_admin', 'stock:read'),
        ('tenant_admin', 'stock:update'),
        ('tenant_admin', 'stock_transactions:read'),
        ('tenant_admin', 'stock_transactions:create'),
        -- end tenant_admin

        -- manager: tenant_admin minus users:*, roles:*
        ('manager', 'customers:read'),
        ('manager', 'customers:create'),
        ('manager', 'customers:update'),
        ('manager', 'customers:delete'),
        ('manager', 'vehicles:read'),
        ('manager', 'vehicles:create'),
        ('manager', 'vehicles:update'),
        ('manager', 'vehicles:delete'),
        ('manager', 'employees:read'),
        ('manager', 'employees:create'),
        ('manager', 'employees:update'),
        ('manager', 'employees:delete'),
        ('manager', 'employees:deactivate'),
        ('manager', 'time_entries:read'),
        ('manager', 'time_entries:create'),
        ('manager', 'time_entries:update'),
        ('manager', 'time_entries:delete'),
        ('manager', 'leave_requests:read'),
        ('manager', 'leave_requests:create'),
        ('manager', 'leave_requests:update'),
        ('manager', 'leave_requests:delete'),
        ('manager', 'leave_requests:approve'),
        ('manager', 'leave_balances:read'),
        ('manager', 'leave_balances:manage'),
        ('manager', 'appointments:read'),
        ('manager', 'appointments:create'),
        ('manager', 'appointments:update'),
        ('manager', 'appointments:delete'),
        ('manager', 'work_orders:read'),
        ('manager', 'work_orders:create'),
        ('manager', 'work_orders:update'),
        ('manager', 'work_orders:delete'),
        ('manager', 'suppliers:read'),
        ('manager', 'suppliers:create'),
        ('manager', 'suppliers:update'),
        ('manager', 'suppliers:delete'),
        ('manager', 'invoices:read'),
        ('manager', 'invoices:create'),
        ('manager', 'invoices:update'),
        ('manager', 'invoices:delete'),
        ('manager', 'invoices:issue'),
        ('manager', 'invoices:mark_paid'),
        ('manager', 'expenses:read'),
        ('manager', 'expenses:create'),
        ('manager', 'expenses:update'),
        ('manager', 'expenses:delete'),
        ('manager', 'codebook:read'),
        ('manager', 'parts_catalog:read'),
        ('manager', 'parts_catalog:create'),
        ('manager', 'parts_catalog:update'),
        ('manager', 'parts_catalog:delete'),
        ('manager', 'stock:read'),
        ('manager', 'stock:update'),
        ('manager', 'stock_transactions:read'),
        ('manager', 'stock_transactions:create'),
        -- end manager

        -- mechanic: performs work; reads what's needed, updates work_orders only
        ('mechanic', 'customers:read'),
        ('mechanic', 'vehicles:read'),
        ('mechanic', 'appointments:read'),
        ('mechanic', 'work_orders:read'),
        ('mechanic', 'work_orders:update'),
        ('mechanic', 'parts_catalog:read'),
        ('mechanic', 'stock:read'),
        ('mechanic', 'stock_transactions:read'),
        ('mechanic', 'time_entries:read'),
        ('mechanic', 'time_entries:create'),
        ('mechanic', 'leave_requests:read'),
        ('mechanic', 'leave_requests:create'),
        ('mechanic', 'leave_balances:read'),
        ('mechanic', 'codebook:read'),
        -- end mechanic

        -- receptionist: front-desk; no HR, no warehouse
        ('receptionist', 'customers:read'),
        ('receptionist', 'customers:create'),
        ('receptionist', 'customers:update'),
        ('receptionist', 'customers:delete'),
        ('receptionist', 'vehicles:read'),
        ('receptionist', 'vehicles:create'),
        ('receptionist', 'vehicles:update'),
        ('receptionist', 'vehicles:delete'),
        ('receptionist', 'appointments:read'),
        ('receptionist', 'appointments:create'),
        ('receptionist', 'appointments:update'),
        ('receptionist', 'appointments:delete'),
        ('receptionist', 'invoices:read'),
        ('receptionist', 'invoices:create'),
        ('receptionist', 'invoices:update'),
        ('receptionist', 'invoices:delete'),
        ('receptionist', 'invoices:issue'),
        ('receptionist', 'invoices:mark_paid'),
        ('receptionist', 'time_entries:read'),
        ('receptionist', 'time_entries:create'),
        ('receptionist', 'leave_requests:read'),
        ('receptionist', 'leave_requests:create'),
        ('receptionist', 'leave_balances:read'),
        ('receptionist', 'codebook:read'),
        -- end receptionist

        -- warehouse_clerk: parts/stock/transactions full; suppliers read
        ('warehouse_clerk', 'parts_catalog:read'),
        ('warehouse_clerk', 'parts_catalog:create'),
        ('warehouse_clerk', 'parts_catalog:update'),
        ('warehouse_clerk', 'parts_catalog:delete'),
        ('warehouse_clerk', 'stock:read'),
        ('warehouse_clerk', 'stock:update'),
        ('warehouse_clerk', 'stock_transactions:read'),
        ('warehouse_clerk', 'stock_transactions:create'),
        ('warehouse_clerk', 'suppliers:read'),
        ('warehouse_clerk', 'time_entries:read'),
        ('warehouse_clerk', 'time_entries:create'),
        ('warehouse_clerk', 'leave_requests:read'),
        ('warehouse_clerk', 'leave_requests:create'),
        ('warehouse_clerk', 'leave_balances:read'),
        ('warehouse_clerk', 'codebook:read')
        -- end warehouse_clerk
)
INSERT INTO auth.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM role_perm_pairs rp
JOIN auth.roles       r ON r.name = rp.role_name AND r.tenant_id IS NULL
JOIN auth.permissions p ON p.name = rp.permission_name
ON CONFLICT (role_id, permission_id) DO NOTHING;

COMMIT;
