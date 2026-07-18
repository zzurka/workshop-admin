-- Migration: 20260317_0955_T_payroll_run_statuses_DDL.sql
-- Description: Create the codebook.payroll_run_statuses lookup table. Statuses
--              for monthly payroll runs. Referenced by
--              hr.payroll_runs.payroll_run_status_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS codebook.payroll_run_statuses (
    id         SMALLSERIAL NOT NULL,
    code       VARCHAR(50) NOT NULL,
    label      JSONB       NOT NULL,
    sort_order SMALLINT    NOT NULL DEFAULT 0,
    is_active  BOOLEAN     NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_codebook_payroll_run_statuses      PRIMARY KEY (id),
    CONSTRAINT uq_codebook_payroll_run_statuses_code UNIQUE (code)
);

COMMENT ON TABLE  codebook.payroll_run_statuses            IS 'Lookup table for payroll run statuses.';
COMMENT ON COLUMN codebook.payroll_run_statuses.id         IS 'SMALLSERIAL primary key. Max 32,767 values — sufficient for a codebook.';
COMMENT ON COLUMN codebook.payroll_run_statuses.code       IS 'Stable machine-readable identifier, e.g. ''approved''. Never changes — safe to use in code and APIs.';
COMMENT ON COLUMN codebook.payroll_run_statuses.label      IS 'Translated display names as JSONB, e.g. {"en": "Approved", "sr": "Odobren"}. Use COALESCE(label->>lang, label->>''en'') to fall back to English.';
COMMENT ON COLUMN codebook.payroll_run_statuses.sort_order IS 'Controls display order in dropdowns. Lower values appear first.';
COMMENT ON COLUMN codebook.payroll_run_statuses.is_active  IS 'FALSE = hidden from UI but retained for historical records that reference it.';

CREATE INDEX IF NOT EXISTS ix_codebook_payroll_run_statuses_is_active
    ON codebook.payroll_run_statuses (is_active);

COMMIT;
