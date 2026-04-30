-- Migration: 20260317_0927_T_service_types_DDL.sql
-- Description: Create the codebook.service_types lookup table.
--              Referenced by workshop.appointment_services.service_type_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.service_types (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_service_types      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_service_types_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.service_types            IS 'Lookup table for common service/maintenance types used as appointment indicators (e.g. oil change, timing belt).';
COMMENT ON COLUMN codebook.service_types.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.service_types.code       IS 'Stable machine-readable identifier, e.g. ''oil_change''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.service_types.label      IS 'Translated display names as JSONB, e.g. {"en": "Oil Change", "sr": "Zamena ulja"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.service_types.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.service_types.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_service_types_is_active
    ON codebook.service_types (is_active);

COMMIT;
