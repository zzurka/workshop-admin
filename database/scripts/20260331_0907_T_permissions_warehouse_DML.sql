-- Migration: 20260402_1070_T_permissions_warehouse_DML.sql
-- Description: Seed warehouse permissions — parts catalog, stock, and
--              stock transactions.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    -- Parts catalog
    ('parts_catalog:read',   'parts_catalog', 'read',   'View parts catalog'),
    ('parts_catalog:create', 'parts_catalog', 'create', 'Add new parts to catalog'),
    ('parts_catalog:update', 'parts_catalog', 'update', 'Edit parts in catalog'),
    ('parts_catalog:delete', 'parts_catalog', 'delete', 'Soft-delete parts from catalog'),

    -- Stock
    ('stock:read',   'stock', 'read',   'View stock levels'),
    ('stock:update', 'stock', 'update', 'Manually adjust stock levels'),

    -- Stock transactions
    ('stock_transactions:read',   'stock_transactions', 'read',   'View stock transaction history'),
    ('stock_transactions:create', 'stock_transactions', 'create', 'Create stock transactions (receive, issue, return)')
ON CONFLICT (name) DO NOTHING;

COMMIT;
