-- Migration: 20260317_1020_T_users_tenants_fk_DDL.sql
-- Description: Add deferred foreign keys between auth.users and tenant tables
--              (tenants, subscription_plans). These cannot live in the original
--              CREATE TABLE scripts because users references tenants and
--              tenants references users (circular), and subscription_plans is
--              created before users.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

-- auth.users.tenant_id → tenant.tenants
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_auth_users_tenant_id'
    ) THEN
        ALTER TABLE auth.users
            ADD CONSTRAINT fk_auth_users_tenant_id
            FOREIGN KEY (tenant_id) REFERENCES tenant.tenants(id);
    END IF;
END $$;

-- tenant.tenants.created_by → auth.users
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_tenant_tenants_created_by'
    ) THEN
        ALTER TABLE tenant.tenants
            ADD CONSTRAINT fk_tenant_tenants_created_by
            FOREIGN KEY (created_by) REFERENCES auth.users(id);
    END IF;
END $$;

-- tenant.tenants.updated_by → auth.users
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_tenant_tenants_updated_by'
    ) THEN
        ALTER TABLE tenant.tenants
            ADD CONSTRAINT fk_tenant_tenants_updated_by
            FOREIGN KEY (updated_by) REFERENCES auth.users(id);
    END IF;
END $$;

-- tenant.subscription_plans.created_by → auth.users
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_tenant_subscription_plans_created_by'
    ) THEN
        ALTER TABLE tenant.subscription_plans
            ADD CONSTRAINT fk_tenant_subscription_plans_created_by
            FOREIGN KEY (created_by) REFERENCES auth.users(id);
    END IF;
END $$;

-- tenant.subscription_plans.updated_by → auth.users
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_tenant_subscription_plans_updated_by'
    ) THEN
        ALTER TABLE tenant.subscription_plans
            ADD CONSTRAINT fk_tenant_subscription_plans_updated_by
            FOREIGN KEY (updated_by) REFERENCES auth.users(id);
    END IF;
END $$;

COMMIT;
