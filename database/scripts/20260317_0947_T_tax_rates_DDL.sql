-- Migration: 20260317_0947_T_tax_rates_DDL.sql
-- Description: Create the codebook.tax_rates lookup table. VAT rates applied
--              to invoice lines. Referenced by workshop.invoice_lines.tax_rate_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.tax_rates (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    rate       NUMERIC(5,2) NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_tax_rates      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_tax_rates_code UNIQUE (code),
    CONSTRAINT ck_codebook_tax_rates_rate CHECK (rate >= 0 AND rate <= 100)
);

COMMENT ON TABLE  codebook.tax_rates            IS 'Lookup table for VAT rates (Serbian ZPDV: standard 20%, reduced 10%, exempt 0%).';
COMMENT ON COLUMN codebook.tax_rates.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.tax_rates.code       IS 'Stable machine-readable identifier, e.g. ''standard''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.tax_rates.label      IS 'Translated display names as JSONB, e.g. {"en": "VAT 20%", "sr": "PDV 20%"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.tax_rates.rate       IS 'VAT percentage (0–100). Invoice lines snapshot this value at line creation — changing it here never affects existing invoices.';
COMMENT ON COLUMN codebook.tax_rates.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.tax_rates.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_tax_rates_is_active
    ON codebook.tax_rates (is_active);

COMMIT;
