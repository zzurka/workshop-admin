-- Migration: 20260317_0951_T_complaint_statuses_DDL.sql
-- Description: Create the codebook.complaint_statuses lookup table. Statuses
--              for customer complaints (reklamacije) on completed work.
--              Referenced by workshop.complaints.complaint_status_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.complaint_statuses (
    id         SMALLSERIAL NOT NULL,
    code       VARCHAR(50) NOT NULL,
    label      JSONB       NOT NULL,
    sort_order SMALLINT    NOT NULL DEFAULT 0,
    is_active  BOOLEAN     NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_complaint_statuses      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_complaint_statuses_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.complaint_statuses            IS 'Lookup table for complaint (reklamacija) statuses.';
COMMENT ON COLUMN codebook.complaint_statuses.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.complaint_statuses.code       IS 'Stable machine-readable identifier, e.g. ''submitted''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.complaint_statuses.label      IS 'Translated display names as JSONB, e.g. {"en": "Submitted", "sr": "Podneta"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.complaint_statuses.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.complaint_statuses.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_complaint_statuses_is_active
    ON codebook.complaint_statuses (is_active);

COMMIT;
