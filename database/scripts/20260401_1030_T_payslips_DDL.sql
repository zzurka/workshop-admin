-- Migration: 20260401_1030_T_payslips_DDL.sql
-- Description: Create the hr.payslips table. One payslip per employee per
--              payroll run. Amounts are recorded results (gross/net computed
--              by accounting), not calculated by the database.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS hr.payslips (
    id                UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id         UUID          NOT NULL,
    payroll_run_id    UUID          NOT NULL,
    employee_id       UUID          NOT NULL,
    hours_worked      NUMERIC(7,2),
    base_amount       NUMERIC(12,2) NOT NULL,
    bonus_amount      NUMERIC(12,2) NOT NULL DEFAULT 0,
    deductions_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    gross_amount      NUMERIC(12,2) NOT NULL,
    net_amount        NUMERIC(12,2) NOT NULL,
    currency_id       SMALLINT      NOT NULL,
    notes             TEXT,
    created_at        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by        UUID,
    updated_at        TIMESTAMPTZ,
    updated_by        UUID,
    is_deleted        BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_hr_payslips                  PRIMARY KEY (id),
    CONSTRAINT uq_hr_payslips_run_employee     UNIQUE (payroll_run_id, employee_id),
    CONSTRAINT fk_hr_payslips_tenant_id        FOREIGN KEY (tenant_id)                 REFERENCES tenant.tenants(id),
    CONSTRAINT fk_hr_payslips_payroll_run_id   FOREIGN KEY (tenant_id, payroll_run_id) REFERENCES hr.payroll_runs(tenant_id, id),
    CONSTRAINT fk_hr_payslips_employee_id      FOREIGN KEY (tenant_id, employee_id)    REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_hr_payslips_currency_id      FOREIGN KEY (currency_id)               REFERENCES codebook.currencies(id),
    CONSTRAINT fk_hr_payslips_created_by       FOREIGN KEY (created_by)                REFERENCES auth.users(id),
    CONSTRAINT fk_hr_payslips_updated_by       FOREIGN KEY (updated_by)                REFERENCES auth.users(id),
    CONSTRAINT ck_hr_payslips_hours_worked     CHECK (hours_worked IS NULL OR hours_worked >= 0),
    CONSTRAINT ck_hr_payslips_amounts          CHECK (base_amount >= 0 AND bonus_amount >= 0 AND deductions_amount >= 0
                                                      AND gross_amount >= 0 AND net_amount >= 0)
);

COMMENT ON TABLE  hr.payslips                   IS 'Payslips — one per employee per payroll run. Frozen once the run is approved (application rule). Zero amounts are allowed (e.g. unpaid leave for the whole month). No per-item breakdown table in v1 — bonus/deductions columns carry the aggregate.';
COMMENT ON COLUMN hr.payslips.id                IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN hr.payslips.tenant_id         IS 'The tenant (workshop) this payslip belongs to.';
COMMENT ON COLUMN hr.payslips.payroll_run_id    IS 'The payroll run this payslip belongs to. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN hr.payslips.employee_id       IS 'The employee this payslip is for. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN hr.payslips.hours_worked      IS 'Hours worked in the period, from time_entries (hourly employees). NULL for monthly-salaried employees.';
COMMENT ON COLUMN hr.payslips.base_amount       IS 'Base salary for the period, from the compensation valid in the period.';
COMMENT ON COLUMN hr.payslips.bonus_amount      IS 'Bonuses / overtime additions. Default 0.';
COMMENT ON COLUMN hr.payslips.deductions_amount IS 'Deductions (obustave). Default 0.';
COMMENT ON COLUMN hr.payslips.gross_amount      IS 'Gross salary (bruto) — recorded result, tax math is external.';
COMMENT ON COLUMN hr.payslips.net_amount        IS 'Net payout (neto).';
COMMENT ON COLUMN hr.payslips.currency_id       IS 'FK to codebook.currencies. Snapshot from the compensation/tenant default.';
COMMENT ON COLUMN hr.payslips.created_by        IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN hr.payslips.updated_at        IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN hr.payslips.is_deleted        IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_hr_payslips_tenant_id
    ON hr.payslips (tenant_id);

CREATE INDEX IF NOT EXISTS ix_hr_payslips_payroll_run_id
    ON hr.payslips (payroll_run_id);

CREATE INDEX IF NOT EXISTS ix_hr_payslips_employee_id
    ON hr.payslips (employee_id);

CREATE INDEX IF NOT EXISTS ix_hr_payslips_is_deleted
    ON hr.payslips (is_deleted);

COMMIT;
