-- Migration: 20260317_0939_T_units_of_measure_DDL.sql
-- Description: Create the codebook.units_of_measure lookup table.
--              Referenced by warehouse.parts_catalog.unit_of_measure_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.units_of_measure (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_units_of_measure      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_units_of_measure_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.units_of_measure            IS 'Lookup table for units of measure (e.g. piece, liter, kilogram).';
COMMENT ON COLUMN codebook.units_of_measure.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.units_of_measure.code       IS 'Stable machine-readable identifier, e.g. ''liter''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.units_of_measure.label      IS 'Translated display names as JSONB, e.g. {"en": "Liter", "sr": "Litar"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.units_of_measure.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.units_of_measure.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_units_of_measure_is_active
    ON codebook.units_of_measure (is_active);

COMMIT;
