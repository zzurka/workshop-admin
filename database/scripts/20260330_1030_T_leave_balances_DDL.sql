-- Migration: 20260330_1030_T_leave_balances_DDL.sql
-- Description: Create the hr.leave_balances table. Tracks annual leave entitlements
--              per employee, leave type, and year.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS hr.leave_balances (
    id             UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id      UUID          NOT NULL,
    employee_id    UUID          NOT NULL,
    leave_type_id  SMALLINT      NOT NULL,
    year           SMALLINT      NOT NULL,
    total_days     NUMERIC(5,2)  NOT NULL,
    used_days      NUMERIC(5,2)  NOT NULL DEFAULT 0,
    created_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by     UUID,
    updated_at     TIMESTAMPTZ,
    updated_by     UUID,
    is_deleted     BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_hr_leave_balances                PRIMARY KEY (id),
    CONSTRAINT uq_hr_leave_balances_emp_type_year  UNIQUE (tenant_id, employee_id, leave_type_id, year),
    CONSTRAINT fk_hr_leave_balances_tenant_id      FOREIGN KEY (tenant_id)     REFERENCES tenant.tenants(id),
    CONSTRAINT fk_hr_leave_balances_employee_id    FOREIGN KEY (employee_id)   REFERENCES hr.employees(id),
    CONSTRAINT fk_hr_leave_balances_leave_type_id  FOREIGN KEY (leave_type_id) REFERENCES codebook.leave_types(id),
    CONSTRAINT fk_hr_leave_balances_created_by     FOREIGN KEY (created_by)    REFERENCES auth.users(id),
    CONSTRAINT fk_hr_leave_balances_updated_by     FOREIGN KEY (updated_by)    REFERENCES auth.users(id),
    CONSTRAINT ck_hr_leave_balances_total_days     CHECK (total_days >= 0),
    CONSTRAINT ck_hr_leave_balances_used_days      CHECK (used_days >= 0)
);

COMMENT ON TABLE  hr.leave_balances                   IS 'Annual leave entitlements. One row per employee per leave type per year.';
COMMENT ON COLUMN hr.leave_balances.id                IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN hr.leave_balances.tenant_id         IS 'The tenant (workshop) this balance belongs to.';
COMMENT ON COLUMN hr.leave_balances.employee_id       IS 'The employee this balance applies to.';
COMMENT ON COLUMN hr.leave_balances.leave_type_id     IS 'FK to codebook.leave_types (vacation, sick, personal, etc.).';
COMMENT ON COLUMN hr.leave_balances.year              IS 'Calendar year this balance applies to (e.g. 2026).';
COMMENT ON COLUMN hr.leave_balances.total_days        IS 'Total days allocated for this year. Supports half-days (e.g. 20.5).';
COMMENT ON COLUMN hr.leave_balances.used_days         IS 'Days consumed so far. Updated when leave requests are approved. Supports half-days.';
COMMENT ON COLUMN hr.leave_balances.created_by        IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN hr.leave_balances.updated_at        IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN hr.leave_balances.is_deleted        IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_hr_leave_balances_tenant_id
    ON hr.leave_balances (tenant_id);

CREATE INDEX IF NOT EXISTS ix_hr_leave_balances_employee_id
    ON hr.leave_balances (employee_id);

CREATE INDEX IF NOT EXISTS ix_hr_leave_balances_year
    ON hr.leave_balances (year);

CREATE INDEX IF NOT EXISTS ix_hr_leave_balances_is_deleted
    ON hr.leave_balances (is_deleted);

COMMIT;
