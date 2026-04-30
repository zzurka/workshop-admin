-- Migration: 20260317_0943_T_billing_periods_DDL.sql
-- Description: Create the codebook.billing_periods lookup table.
--              Referenced by tenant.subscription_plans.billing_period_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.billing_periods (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_billing_periods      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_billing_periods_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.billing_periods            IS 'Lookup table for subscription billing cadences (e.g. monthly, yearly, one_time).';
COMMENT ON COLUMN codebook.billing_periods.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.billing_periods.code       IS 'Stable machine-readable identifier, e.g. ''monthly''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.billing_periods.label      IS 'Translated display names as JSONB, e.g. {"en": "Monthly", "sr": "Mesečno"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.billing_periods.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.billing_periods.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_billing_periods_is_active
    ON codebook.billing_periods (is_active);

COMMIT;
