-- Migration: 20260330_1020_T_time_entries_DDL.sql
-- Description: Create the hr.time_entries table. Tracks employee clock-in/out times.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS hr.time_entries (
    id                  UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id           UUID         NOT NULL,
    employee_id         UUID         NOT NULL,
    clock_in            TIMESTAMPTZ  NOT NULL,
    clock_out           TIMESTAMPTZ,
    break_duration_min  SMALLINT     NOT NULL DEFAULT 0,
    notes               TEXT,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_hr_time_entries              PRIMARY KEY (id),
    CONSTRAINT fk_hr_time_entries_tenant_id    FOREIGN KEY (tenant_id)   REFERENCES tenant.tenants(id),
    CONSTRAINT fk_hr_time_entries_employee_id  FOREIGN KEY (employee_id) REFERENCES hr.employees(id),
    CONSTRAINT fk_hr_time_entries_created_by   FOREIGN KEY (created_by)  REFERENCES auth.users(id),
    CONSTRAINT fk_hr_time_entries_updated_by   FOREIGN KEY (updated_by)  REFERENCES auth.users(id),
    CONSTRAINT ck_hr_time_entries_clock_range  CHECK (clock_out IS NULL OR clock_out > clock_in),
    CONSTRAINT ck_hr_time_entries_break_min    CHECK (break_duration_min >= 0)
);

COMMENT ON TABLE  hr.time_entries                     IS 'Employee clock-in/out records. Tracks daily work time per employee.';
COMMENT ON COLUMN hr.time_entries.id                  IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN hr.time_entries.tenant_id           IS 'The tenant (workshop) this time entry belongs to.';
COMMENT ON COLUMN hr.time_entries.employee_id         IS 'The employee who clocked in.';
COMMENT ON COLUMN hr.time_entries.clock_in            IS 'Timestamp when the employee clocked in.';
COMMENT ON COLUMN hr.time_entries.clock_out           IS 'Timestamp when the employee clocked out. NULL if still clocked in.';
COMMENT ON COLUMN hr.time_entries.break_duration_min  IS 'Total break time in minutes during this entry. Default 0.';
COMMENT ON COLUMN hr.time_entries.notes               IS 'Optional notes about the time entry (e.g. task description).';
COMMENT ON COLUMN hr.time_entries.created_by          IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN hr.time_entries.updated_at          IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN hr.time_entries.is_deleted          IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_hr_time_entries_tenant_id
    ON hr.time_entries (tenant_id);

CREATE INDEX IF NOT EXISTS ix_hr_time_entries_employee_id
    ON hr.time_entries (employee_id);

CREATE INDEX IF NOT EXISTS ix_hr_time_entries_clock_in
    ON hr.time_entries (clock_in);

CREATE INDEX IF NOT EXISTS ix_hr_time_entries_is_deleted
    ON hr.time_entries (is_deleted);

COMMIT;
