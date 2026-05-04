-- Migration: 20260504_1010_T_permissions_subscription_plans_DML.sql
-- Description: Seed subscription plan permissions. Platform-level only —
--              subscription_plans is a billable product catalog managed
--              exclusively by platform_admin.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-04
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description, scope) VALUES
    ('subscription_plans:read',   'subscription_plans', 'read',   'View subscription plans',          'platform'),
    ('subscription_plans:create', 'subscription_plans', 'create', 'Create new subscription plans',    'platform'),
    ('subscription_plans:update', 'subscription_plans', 'update', 'Edit subscription plans',          'platform'),
    ('subscription_plans:delete', 'subscription_plans', 'delete', 'Soft-delete subscription plans',   'platform')
ON CONFLICT (name) DO NOTHING;

COMMIT;
