-- Migration: 20260317_0949_T_payment_methods_DDL.sql
-- Description: Create the codebook.payment_methods lookup table.
--              Referenced by workshop.payments.payment_method_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.payment_methods (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_payment_methods      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_payment_methods_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.payment_methods            IS 'Lookup table for payment methods (e.g. cash, card, bank transfer).';
COMMENT ON COLUMN codebook.payment_methods.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.payment_methods.code       IS 'Stable machine-readable identifier, e.g. ''bank_transfer''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.payment_methods.label      IS 'Translated display names as JSONB, e.g. {"en": "Cash", "sr": "Gotovina"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.payment_methods.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.payment_methods.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_payment_methods_is_active
    ON codebook.payment_methods (is_active);

COMMIT;
