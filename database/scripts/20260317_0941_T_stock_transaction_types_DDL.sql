-- Migration: 20260317_0941_T_stock_transaction_types_DDL.sql
-- Description: Create the codebook.stock_transaction_types lookup table.
--              Referenced by warehouse.stock_transactions.transaction_type_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.stock_transaction_types (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_stock_transaction_types      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_stock_transaction_types_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.stock_transaction_types            IS 'Lookup table for stock transaction types (e.g. receipt, issue, return, adjustment).';
COMMENT ON COLUMN codebook.stock_transaction_types.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.stock_transaction_types.code       IS 'Stable machine-readable identifier, e.g. ''receipt''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.stock_transaction_types.label      IS 'Translated display names as JSONB, e.g. {"en": "Receipt", "sr": "Prijem"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.stock_transaction_types.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.stock_transaction_types.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_stock_transaction_types_is_active
    ON codebook.stock_transaction_types (is_active);

COMMIT;
