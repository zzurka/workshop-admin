-- Migration: 20260330_1015_T_employee_compensations_DDL.sql
-- Description: Create the hr.employee_compensations table. History of agreed
--              compensation per employee — monthly salary or hourly rate.
--              Replaces the former employees.hourly_rate column. At most one
--              open (valid_to IS NULL) compensation per employee.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS hr.employee_compensations (
    id           UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id    UUID          NOT NULL,
    employee_id  UUID          NOT NULL,
    basis        VARCHAR(10)   NOT NULL,
    amount       NUMERIC(12,2) NOT NULL,
    currency_id  SMALLINT      NOT NULL,
    valid_from   DATE          NOT NULL,
    valid_to     DATE,
    notes        TEXT,
    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by   UUID,
    updated_at   TIMESTAMPTZ,
    updated_by   UUID,
    is_deleted   BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_hr_employee_compensations             PRIMARY KEY (id),
    CONSTRAINT fk_hr_employee_compensations_tenant_id   FOREIGN KEY (tenant_id)              REFERENCES tenant.tenants(id),
    CONSTRAINT fk_hr_employee_compensations_employee_id FOREIGN KEY (tenant_id, employee_id) REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_hr_employee_compensations_currency_id FOREIGN KEY (currency_id)            REFERENCES codebook.currencies(id),
    CONSTRAINT fk_hr_employee_compensations_created_by  FOREIGN KEY (created_by)             REFERENCES auth.users(id),
    CONSTRAINT fk_hr_employee_compensations_updated_by  FOREIGN KEY (updated_by)             REFERENCES auth.users(id),
    CONSTRAINT ck_hr_employee_compensations_basis       CHECK (basis IN ('monthly', 'hourly')),
    CONSTRAINT ck_hr_employee_compensations_amount      CHECK (amount > 0),
    CONSTRAINT ck_hr_employee_compensations_valid_range CHECK (valid_to IS NULL OR valid_to >= valid_from)
);

COMMENT ON TABLE  hr.employee_compensations             IS 'History of agreed compensation per employee. Overlap of closed periods is prevented at the application level (no btree_gist EXCLUDE by design); the database guarantees only "one open period per employee" via a partial unique index.';
COMMENT ON COLUMN hr.employee_compensations.id          IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN hr.employee_compensations.tenant_id   IS 'The tenant (workshop) this compensation belongs to.';
COMMENT ON COLUMN hr.employee_compensations.employee_id IS 'The employee this compensation applies to. Composite FK (tenant_id, employee_id) guarantees the employee belongs to the same tenant.';
COMMENT ON COLUMN hr.employee_compensations.basis       IS '''monthly'' = fixed monthly salary; ''hourly'' = hourly rate (labor cost for time entries reads the compensation valid on the entry date).';
COMMENT ON COLUMN hr.employee_compensations.amount      IS 'Agreed amount: per month for basis ''monthly'', per hour for basis ''hourly''. Always positive.';
COMMENT ON COLUMN hr.employee_compensations.currency_id IS 'FK to codebook.currencies. Snapshot of the tenant''s default currency when the compensation was agreed.';
COMMENT ON COLUMN hr.employee_compensations.valid_from  IS 'First day this compensation applies.';
COMMENT ON COLUMN hr.employee_compensations.valid_to    IS 'Last day this compensation applies (inclusive). NULL = currently valid.';
COMMENT ON COLUMN hr.employee_compensations.created_by  IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN hr.employee_compensations.updated_at  IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN hr.employee_compensations.is_deleted  IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

-- At most one open (valid_to IS NULL) compensation per employee.
CREATE UNIQUE INDEX IF NOT EXISTS uq_hr_employee_compensations_open
    ON hr.employee_compensations (employee_id)
    WHERE valid_to IS NULL AND NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_hr_employee_compensations_tenant_id
    ON hr.employee_compensations (tenant_id);

CREATE INDEX IF NOT EXISTS ix_hr_employee_compensations_employee_id
    ON hr.employee_compensations (employee_id);

CREATE INDEX IF NOT EXISTS ix_hr_employee_compensations_is_deleted
    ON hr.employee_compensations (is_deleted);

COMMIT;
