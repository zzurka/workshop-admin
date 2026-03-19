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
    label      VARCHAR(100) NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_transmissions       PRIMARY KEY (id),
    CONSTRAINT uq_codebook_transmissions_label UNIQUE (label)
);

COMMENT ON TABLE  codebook.transmissions            IS 'Lookup table for vehicle transmission types (e.g. Automatic, Manual, CVT).';
COMMENT ON COLUMN codebook.transmissions.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.transmissions.label      IS 'Human-readable display name shown in the UI.';
COMMENT ON COLUMN codebook.transmissions.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.transmissions.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_transmissions_is_active
    ON codebook.transmissions (is_active);

INSERT INTO codebook.transmissions (label, sort_order) VALUES
    ('Automatic', 1),
    ('Manual',    2),
    ('CVT',       3),
    ('DCT',       4)
ON CONFLICT (label) DO NOTHING;

COMMIT;
