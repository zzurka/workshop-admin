-- Migration: 20260319_0930_T_subscription_plans_DDL.sql
-- Description: Create the codebook.subscription_plans lookup table.
--              Referenced by tenant.tenants.subscription_plan_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.subscription_plans (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_subscription_plans      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_subscription_plans_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.subscription_plans            IS 'Lookup table for tenant subscription tiers (e.g. free, trial, paid).';
COMMENT ON COLUMN codebook.subscription_plans.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.subscription_plans.code       IS 'Stable machine-readable identifier, e.g. ''free''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.subscription_plans.label      IS 'Translated display names as JSONB, e.g. {"en": "Free", "hr": "Besplatno"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.subscription_plans.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.subscription_plans.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_subscription_plans_is_active
    ON codebook.subscription_plans (is_active);

COMMIT;
