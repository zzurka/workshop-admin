-- Migration: 20260319_0910_T_fuel_types_DDL.sql
-- Description: Create the codebook.fuel_types lookup table.
--              Referenced by customer.vehicles.fuel_type_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.fuel_types (
    id         SMALLSERIAL  NOT NULL,
    label      VARCHAR(100) NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_fuel_types       PRIMARY KEY (id),
    CONSTRAINT uq_codebook_fuel_types_label UNIQUE (label)
);

COMMENT ON TABLE  codebook.fuel_types            IS 'Lookup table for vehicle fuel types (e.g. Gasoline, Diesel, Electric, Hybrid).';
COMMENT ON COLUMN codebook.fuel_types.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.fuel_types.label      IS 'Human-readable display name shown in the UI.';
COMMENT ON COLUMN codebook.fuel_types.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.fuel_types.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_fuel_types_is_active
    ON codebook.fuel_types (is_active);

INSERT INTO codebook.fuel_types (label, sort_order) VALUES
    ('Gasoline', 1),
    ('Diesel',   2),
    ('Electric', 3),
    ('Hybrid',   4),
    ('LPG',      5),
    ('CNG',      6)
ON CONFLICT (label) DO NOTHING;

COMMIT;
