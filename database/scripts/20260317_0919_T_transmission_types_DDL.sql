-- Migration: 20260317_0919_T_transmission_types_DDL.sql
-- Description: Create the codebook.transmission_types lookup table.
--              Referenced by customer.vehicles.transmission_type_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.transmission_types (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_transmission_types      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_transmission_types_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.transmission_types            IS 'Lookup table for vehicle transmission types (e.g. automatic, manual, cvt).';
COMMENT ON COLUMN codebook.transmission_types.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.transmission_types.code       IS 'Stable machine-readable identifier, e.g. ''automatic''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.transmission_types.label      IS 'Translated display names as JSONB, e.g. {"en": "Automatic", "sr": "Automatski"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.transmission_types.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.transmission_types.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_transmission_types_is_active
    ON codebook.transmission_types (is_active);

COMMIT;
