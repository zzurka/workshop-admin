-- Migration: 20260330_0920_T_leave_statuses_DDL.sql
-- Description: Create the codebook.leave_statuses lookup table.
--              Referenced by hr.leave_requests.leave_status_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.leave_statuses (
    id         SMALLSERIAL  NOT NULL,
    code       VARCHAR(50)  NOT NULL,
    label      JSONB        NOT NULL,
    sort_order SMALLINT     NOT NULL DEFAULT 0,
    is_active  BOOLEAN      NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_leave_statuses      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_leave_statuses_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.leave_statuses            IS 'Lookup table for leave request statuses (e.g. pending, approved, rejected).';
COMMENT ON COLUMN codebook.leave_statuses.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.leave_statuses.code       IS 'Stable machine-readable identifier, e.g. ''approved''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.leave_statuses.label      IS 'Translated display names as JSONB, e.g. {"en": "Approved", "sr": "Odobreno"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.leave_statuses.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.leave_statuses.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_leave_statuses_is_active
    ON codebook.leave_statuses (is_active);

COMMIT;
