-- Migration: 20260401_1020_T_payroll_runs_DDL.sql
-- Description: Create the hr.payroll_runs table. Monthly payroll cycles per
--              tenant. Scope: record-keeping (gross/net/additions/deductions),
--              NOT tax calculation — Serbian tax/contribution math is done by
--              accounting, results are recorded here.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS hr.payroll_runs (
    id                    UUID        NOT NULL DEFAULT uuidv7(),
    tenant_id             UUID        NOT NULL,
    period_year           SMALLINT    NOT NULL,
    period_month          SMALLINT    NOT NULL,
    payroll_run_status_id SMALLINT    NOT NULL,
    approved_by           UUID,
    approved_at           TIMESTAMPTZ,
    paid_at               TIMESTAMPTZ,
    notes                 TEXT,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by            UUID,
    updated_at            TIMESTAMPTZ,
    updated_by            UUID,
    is_deleted            BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_hr_payroll_runs                       PRIMARY KEY (id),
    CONSTRAINT uq_hr_payroll_runs_tenant_id_id          UNIQUE (tenant_id, id),
    CONSTRAINT fk_hr_payroll_runs_tenant_id             FOREIGN KEY (tenant_id)              REFERENCES tenant.tenants(id),
    CONSTRAINT fk_hr_payroll_runs_payroll_run_status_id FOREIGN KEY (payroll_run_status_id)  REFERENCES codebook.payroll_run_statuses(id),
    CONSTRAINT fk_hr_payroll_runs_approved_by           FOREIGN KEY (tenant_id, approved_by) REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_hr_payroll_runs_created_by            FOREIGN KEY (created_by)             REFERENCES auth.users(id),
    CONSTRAINT fk_hr_payroll_runs_updated_by            FOREIGN KEY (updated_by)             REFERENCES auth.users(id),
    CONSTRAINT ck_hr_payroll_runs_period_month          CHECK (period_month BETWEEN 1 AND 12)
);

COMMENT ON TABLE  hr.payroll_runs                       IS 'Monthly payroll cycles. Application flow: create run -> generate payslips from valid employee_compensations -> corrections -> approved -> paid. Payslips are frozen once the run is approved. Salary payout is also booked as an expense (category ''salaries'') — that link is application-level, no FK.';
COMMENT ON COLUMN hr.payroll_runs.id                    IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN hr.payroll_runs.tenant_id             IS 'The tenant (workshop) this payroll run belongs to.';
COMMENT ON COLUMN hr.payroll_runs.period_year           IS 'Payroll period year (e.g. 2026).';
COMMENT ON COLUMN hr.payroll_runs.period_month          IS 'Payroll period month (1–12). Serbian payroll is monthly.';
COMMENT ON COLUMN hr.payroll_runs.payroll_run_status_id IS 'FK to codebook.payroll_run_statuses (draft, approved, paid, cancelled).';
COMMENT ON COLUMN hr.payroll_runs.approved_by           IS 'Employee (manager/owner) who approved the run. NULL while draft. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN hr.payroll_runs.approved_at           IS 'When the run was approved. NULL while draft.';
COMMENT ON COLUMN hr.payroll_runs.paid_at               IS 'When the salaries were paid out. NULL until paid.';
COMMENT ON COLUMN hr.payroll_runs.created_by            IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN hr.payroll_runs.updated_at            IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN hr.payroll_runs.is_deleted            IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

-- One payroll run per tenant per month (soft-deleted runs release the slot).
CREATE UNIQUE INDEX IF NOT EXISTS uq_hr_payroll_runs_tenant_period
    ON hr.payroll_runs (tenant_id, period_year, period_month)
    WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_hr_payroll_runs_tenant_id
    ON hr.payroll_runs (tenant_id);

CREATE INDEX IF NOT EXISTS ix_hr_payroll_runs_payroll_run_status_id
    ON hr.payroll_runs (payroll_run_status_id);

CREATE INDEX IF NOT EXISTS ix_hr_payroll_runs_is_deleted
    ON hr.payroll_runs (is_deleted);

COMMIT;
