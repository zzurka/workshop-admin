-- Migration: 20260401_1000_T_expense_categories_DDL.sql
-- Description: Create the codebook.expense_categories lookup table.
--              Referenced by workshop.expenses.expense_category_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-01
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.expense_categories (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_expense_categories      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_expense_categories_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.expense_categories            IS 'Lookup table for expense categories (e.g. rent, utilities, tools, insurance).';
COMMENT ON COLUMN codebook.expense_categories.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.expense_categories.code       IS 'Stable machine-readable identifier, e.g. ''rent''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.expense_categories.label      IS 'Translated display names as JSONB, e.g. {"en": "Rent", "sr": "Zakup"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.expense_categories.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.expense_categories.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_expense_categories_is_active
    ON codebook.expense_categories (is_active);

COMMIT;
