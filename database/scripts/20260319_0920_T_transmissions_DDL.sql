-- Migration: 20260319_0920_T_transmissions_DDL.sql
-- Description: Create the codebook.transmissions lookup table.
--              Referenced by customer.vehicles.transmission_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.transmissions (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_transmissions      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_transmissions_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.transmissions            IS 'Lookup table for vehicle transmission types (e.g. automatic, manual, cvt).';
COMMENT ON COLUMN codebook.transmissions.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.transmissions.code       IS 'Stable machine-readable identifier, e.g. ''automatic''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.transmissions.label      IS 'Translated display names as JSONB, e.g. {"en": "Automatic", "hr": "Automatski"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.transmissions.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.transmissions.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_transmissions_is_active
    ON codebook.transmissions (is_active);

INSERT INTO codebook.transmissions (code, label, sort_order) VALUES
    ('automatic', '{"en": "Automatic", "hr": "Automatski"}', 1),
    ('manual',    '{"en": "Manual",    "hr": "Ručni"}',      2),
    ('cvt',       '{"en": "CVT",       "hr": "CVT"}',        3),
    ('dct',       '{"en": "DCT",       "hr": "DCT"}',        4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
