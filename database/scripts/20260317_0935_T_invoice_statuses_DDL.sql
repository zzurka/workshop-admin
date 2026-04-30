-- Migration: 20260317_0935_T_invoice_statuses_DDL.sql
-- Description: Create the codebook.invoice_statuses lookup table.
--              Referenced by workshop.invoices.invoice_status_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.invoice_statuses (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_invoice_statuses      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_invoice_statuses_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.invoice_statuses            IS 'Lookup table for invoice statuses (e.g. draft, issued, paid, cancelled).';
COMMENT ON COLUMN codebook.invoice_statuses.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.invoice_statuses.code       IS 'Stable machine-readable identifier, e.g. ''issued''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.invoice_statuses.label      IS 'Translated display names as JSONB, e.g. {"en": "Issued", "sr": "Izdata"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.invoice_statuses.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.invoice_statuses.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_invoice_statuses_is_active
    ON codebook.invoice_statuses (is_active);

COMMIT;
