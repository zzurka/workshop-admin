-- Migration: 20260317_1000_T_subscription_plans_DDL.sql
-- Description: Create the tenant.subscription_plans table. Each row represents
--              a billable product with pricing, billing cadence, usage limits,
--              and feature flags. Referenced by tenant.tenants.subscription_plan_id.
--              Managed exclusively by platform_admin (tenant_id IS NULL users).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS tenant.subscription_plans (
    id                          UUID          NOT NULL DEFAULT uuidv7(),
    code                        VARCHAR(50)   NOT NULL,
    label                       JSONB         NOT NULL,
    description                 JSONB,
    price                       NUMERIC(10,2) NOT NULL DEFAULT 0,
    currency_id                 SMALLINT      NOT NULL,
    billing_period_id           SMALLINT      NOT NULL,
    trial_days                  SMALLINT      NOT NULL DEFAULT 0,
    max_users                   INTEGER,
    max_vehicles                INTEGER,
    max_work_orders_per_month   INTEGER,
    max_storage_mb              INTEGER,
    features                    JSONB         NOT NULL DEFAULT '{}'::jsonb,
    is_public                   BOOLEAN       NOT NULL DEFAULT TRUE,
    sort_order                  SMALLINT      NOT NULL DEFAULT 0,
    is_active                   BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by                  UUID,
    updated_at                  TIMESTAMPTZ,
    updated_by                  UUID,
    is_deleted                  BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tenant_subscription_plans                    		PRIMARY KEY (id),
    CONSTRAINT uq_tenant_subscription_plans_code               		UNIQUE (code),
    CONSTRAINT fk_tenant_subscription_plans_currency_id        		FOREIGN KEY (currency_id)       REFERENCES codebook.currencies(id),
    CONSTRAINT fk_tenant_subscription_plans_billing_period_id  		FOREIGN KEY (billing_period_id) REFERENCES codebook.billing_periods(id),
    CONSTRAINT ck_tenant_subscription_plans_price_non_negative 		CHECK (price >= 0),
    CONSTRAINT ck_tenant_subscription_plans_trial_days_non_negative CHECK (trial_days >= 0)
    -- created_by / updated_by FKs added in 20260317_1020_T_users_tenants_fk_DDL.sql (circular dependency)
);

COMMENT ON TABLE  tenant.subscription_plans                           IS 'Billable subscription plans offered to tenants. Includes pricing, billing cadence, usage limits, and feature flags.';
COMMENT ON COLUMN tenant.subscription_plans.id                        IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN tenant.subscription_plans.code                      IS 'Stable machine-readable identifier, e.g. ''free'', ''pro''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN tenant.subscription_plans.label                     IS 'Translated display names as JSONB, e.g. {"en": "Pro", "sr": "Pro"}.';
COMMENT ON COLUMN tenant.subscription_plans.description               IS 'Translated marketing/descriptive text as JSONB. NULL if no description.';
COMMENT ON COLUMN tenant.subscription_plans.price                     IS 'Plan price per billing period, in the currency referenced by currency_id. 0 = free.';
COMMENT ON COLUMN tenant.subscription_plans.currency_id               IS 'FK to codebook.currencies. The currency the plan is billed in.';
COMMENT ON COLUMN tenant.subscription_plans.billing_period_id         IS 'FK to codebook.billing_periods. How often the plan is billed (monthly, yearly, one_time, ...).';
COMMENT ON COLUMN tenant.subscription_plans.trial_days                IS 'Number of free trial days granted to new tenants on this plan. 0 = no trial.';
COMMENT ON COLUMN tenant.subscription_plans.max_users                 IS 'Maximum users per tenant. NULL = unlimited.';
COMMENT ON COLUMN tenant.subscription_plans.max_vehicles              IS 'Maximum vehicles tracked per tenant. NULL = unlimited.';
COMMENT ON COLUMN tenant.subscription_plans.max_work_orders_per_month IS 'Maximum work orders created per calendar month per tenant. NULL = unlimited.';
COMMENT ON COLUMN tenant.subscription_plans.max_storage_mb            IS 'Maximum storage (attachments, etc.) per tenant in MB. NULL = unlimited.';
COMMENT ON COLUMN tenant.subscription_plans.features                  IS 'Feature flags as JSONB, e.g. {"api_access": true, "advanced_reports": false}. Drives feature gating in the application.';
COMMENT ON COLUMN tenant.subscription_plans.is_public                 IS 'TRUE = visible in self-serve signup. FALSE = internal/custom plan, only assignable by platform_admin.';
COMMENT ON COLUMN tenant.subscription_plans.sort_order                IS 'Controls display order in pricing tables. Lower values appear first.';
COMMENT ON COLUMN tenant.subscription_plans.is_active                 IS 'FALSE = retired plan. Existing tenants keep it; new tenants cannot select it.';
COMMENT ON COLUMN tenant.subscription_plans.created_by                IS 'User who created this record. NULL for bootstrap/seed records.';
COMMENT ON COLUMN tenant.subscription_plans.updated_at                IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN tenant.subscription_plans.is_deleted                IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_tenant_subscription_plans_is_active
    ON tenant.subscription_plans (is_active);

CREATE INDEX IF NOT EXISTS ix_tenant_subscription_plans_is_deleted
    ON tenant.subscription_plans (is_deleted);

COMMIT;
