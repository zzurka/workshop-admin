-- Migration: 20260330_1010_T_employees_DDL.sql
-- Description: Create the hr.employees table. Each employee belongs to a single
--              tenant and is linked to an auth.users account for login.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS hr.employees (
    id                 UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id          UUID         NOT NULL,
    user_id            UUID         NOT NULL,
    employment_type_id SMALLINT     NOT NULL,
    hire_date          DATE         NOT NULL,
    termination_date   DATE,
    hourly_rate        NUMERIC(10,2),
    notes              TEXT,
    is_active          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by         UUID,
    updated_at         TIMESTAMPTZ,
    updated_by         UUID,
    is_deleted         BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_hr_employees                    PRIMARY KEY (id),
    CONSTRAINT uq_hr_employees_user_id            UNIQUE (user_id),
    CONSTRAINT fk_hr_employees_tenant_id          FOREIGN KEY (tenant_id)          REFERENCES tenant.tenants(id),
    CONSTRAINT fk_hr_employees_user_id            FOREIGN KEY (user_id)            REFERENCES auth.users(id),
    CONSTRAINT fk_hr_employees_employment_type_id FOREIGN KEY (employment_type_id) REFERENCES codebook.employment_types(id),
    CONSTRAINT fk_hr_employees_created_by         FOREIGN KEY (created_by)         REFERENCES auth.users(id),
    CONSTRAINT fk_hr_employees_updated_by         FOREIGN KEY (updated_by)         REFERENCES auth.users(id)
);

COMMENT ON TABLE  hr.employees                    IS 'Workshop employees. Each employee belongs to one tenant and has exactly one login account.';
COMMENT ON COLUMN hr.employees.id                 IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN hr.employees.tenant_id          IS 'The tenant (workshop) this employee belongs to.';
COMMENT ON COLUMN hr.employees.user_id            IS 'Link to auth.users — every employee must have a login account. UNIQUE — one auth account per employee. Name, email, phone are on auth.users.';
COMMENT ON COLUMN hr.employees.employment_type_id IS 'FK to codebook.employment_types (salaried, hourly).';
COMMENT ON COLUMN hr.employees.hire_date          IS 'Date the employee started working.';
COMMENT ON COLUMN hr.employees.termination_date   IS 'Date the employee left. NULL if still employed.';
COMMENT ON COLUMN hr.employees.hourly_rate        IS 'Hourly rate for hourly employees. NULL for salaried employees. Used for time entry cost calculations.';
COMMENT ON COLUMN hr.employees.notes              IS 'Internal HR notes about this employee. Not visible to the employee.';
COMMENT ON COLUMN hr.employees.is_active          IS 'FALSE = employee record suspended / inactive.';
COMMENT ON COLUMN hr.employees.created_by         IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN hr.employees.updated_at         IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN hr.employees.is_deleted         IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_hr_employees_tenant_id
    ON hr.employees (tenant_id);

CREATE INDEX IF NOT EXISTS ix_hr_employees_user_id
    ON hr.employees (user_id);

CREATE INDEX IF NOT EXISTS ix_hr_employees_is_deleted
    ON hr.employees (is_deleted);

CREATE INDEX IF NOT EXISTS ix_hr_employees_employment_type_id
    ON hr.employees (employment_type_id);

COMMIT;
