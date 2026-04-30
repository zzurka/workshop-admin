-- Migration: 20260317_0945_T_currencies_DDL.sql
-- Description: Create the codebook.currencies lookup table.
--              Stores ISO 4217 currency codes used by the system.
--              Referenced by tenant.subscription_plans.currency_id (and future
--              invoice tables once multi-currency is supported).
-- Author: WorkshopAdmin Team
-- Date: 2026-04-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.currencies (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_currencies      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_currencies_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.currencies            IS 'Lookup table for currencies. Code is the ISO 4217 three-letter identifier (e.g. EUR, USD, RSD).';
COMMENT ON COLUMN codebook.currencies.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.currencies.code       IS 'ISO 4217 currency code, e.g. ''EUR''. Stable machine-readable identifier — never changes.';
COMMENT ON COLUMN codebook.currencies.label      IS 'Translated display names as JSONB, e.g. {"en": "Euro", "sr": "Evro"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.currencies.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.currencies.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_currencies_is_active
    ON codebook.currencies (is_active);

COMMIT;
