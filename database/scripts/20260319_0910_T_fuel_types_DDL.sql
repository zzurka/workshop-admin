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
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_fuel_types      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_fuel_types_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.fuel_types            IS 'Lookup table for vehicle fuel types (e.g. gasoline, diesel, electric, hybrid).';
COMMENT ON COLUMN codebook.fuel_types.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.fuel_types.code       IS 'Stable machine-readable identifier, e.g. ''gasoline''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.fuel_types.label      IS 'Translated display names as JSONB, e.g. {"en": "Gasoline", "hr": "Benzin"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.fuel_types.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.fuel_types.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_fuel_types_is_active
    ON codebook.fuel_types (is_active);

INSERT INTO codebook.fuel_types (code, label, sort_order) VALUES
    ('gasoline', '{"en": "Gasoline", "hr": "Benzin"}',     1),
    ('diesel',   '{"en": "Diesel",   "hr": "Dizel"}',      2),
    ('electric', '{"en": "Electric", "hr": "Električni"}', 3),
    ('hybrid',   '{"en": "Hybrid",   "hr": "Hibrid"}',     4),
    ('lpg',      '{"en": "LPG",      "hr": "UNP"}',        5),
    ('cng',      '{"en": "CNG",      "hr": "SPP"}',        6)
ON CONFLICT (code) DO NOTHING;

COMMIT;
