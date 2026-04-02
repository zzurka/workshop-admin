-- Migration: 20260330_1130_T_part_statuses_DDL.sql
-- Description: Create the codebook.part_statuses lookup table.
--              Referenced by workshop.work_order_parts.part_status_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.part_statuses (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_part_statuses      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_part_statuses_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.part_statuses            IS 'Lookup table for part availability statuses (e.g. in_stock, ordered, received).';
COMMENT ON COLUMN codebook.part_statuses.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.part_statuses.code       IS 'Stable machine-readable identifier, e.g. ''in_stock''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.part_statuses.label      IS 'Translated display names as JSONB, e.g. {"en": "In Stock", "sr": "Na stanju"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.part_statuses.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.part_statuses.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_part_statuses_is_active
    ON codebook.part_statuses (is_active);

COMMIT;
