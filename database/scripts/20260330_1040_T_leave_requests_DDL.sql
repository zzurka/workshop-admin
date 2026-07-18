-- Migration: 20260330_1040_T_leave_requests_DDL.sql
-- Description: Create the hr.leave_requests table. Individual leave/absence requests
--              with approval workflow (pending → approved/rejected/cancelled).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS hr.leave_requests (
    id               UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id        UUID          NOT NULL,
    employee_id      UUID          NOT NULL,
    leave_type_id    SMALLINT      NOT NULL,
    leave_status_id  SMALLINT      NOT NULL,
    start_date       DATE          NOT NULL,
    end_date         DATE          NOT NULL,
    total_days       NUMERIC(5,2)  NOT NULL,
    notes            TEXT,
    reviewed_by      UUID,
    reviewed_at      TIMESTAMPTZ,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by       UUID,
    updated_at       TIMESTAMPTZ,
    updated_by       UUID,
    is_deleted       BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_hr_leave_requests                  PRIMARY KEY (id),
    CONSTRAINT fk_hr_leave_requests_tenant_id        FOREIGN KEY (tenant_id)       REFERENCES tenant.tenants(id),
    CONSTRAINT fk_hr_leave_requests_employee_id      FOREIGN KEY (tenant_id, employee_id) REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_hr_leave_requests_leave_type_id    FOREIGN KEY (leave_type_id)   REFERENCES codebook.leave_types(id),
    CONSTRAINT fk_hr_leave_requests_leave_status_id  FOREIGN KEY (leave_status_id) REFERENCES codebook.leave_statuses(id),
    CONSTRAINT fk_hr_leave_requests_reviewed_by      FOREIGN KEY (tenant_id, reviewed_by) REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_hr_leave_requests_created_by       FOREIGN KEY (created_by)      REFERENCES auth.users(id),
    CONSTRAINT fk_hr_leave_requests_updated_by       FOREIGN KEY (updated_by)      REFERENCES auth.users(id),
    CONSTRAINT ck_hr_leave_requests_date_range       CHECK (end_date >= start_date),
    CONSTRAINT ck_hr_leave_requests_total_days       CHECK (total_days > 0)
);

COMMENT ON TABLE  hr.leave_requests                    IS 'Individual leave/absence requests with approval workflow.';
COMMENT ON COLUMN hr.leave_requests.id                 IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN hr.leave_requests.tenant_id          IS 'The tenant (workshop) this request belongs to.';
COMMENT ON COLUMN hr.leave_requests.employee_id        IS 'The employee requesting leave.';
COMMENT ON COLUMN hr.leave_requests.leave_type_id      IS 'FK to codebook.leave_types (vacation, sick, personal, etc.).';
COMMENT ON COLUMN hr.leave_requests.leave_status_id    IS 'FK to codebook.leave_statuses (pending, approved, rejected, cancelled).';
COMMENT ON COLUMN hr.leave_requests.start_date         IS 'First day of the requested leave period.';
COMMENT ON COLUMN hr.leave_requests.end_date           IS 'Last day of the requested leave period (inclusive).';
COMMENT ON COLUMN hr.leave_requests.total_days         IS 'Number of leave days requested. Supports half-days (e.g. 0.5 for a half-day).';
COMMENT ON COLUMN hr.leave_requests.notes              IS 'Optional reason or details for the leave request.';
COMMENT ON COLUMN hr.leave_requests.reviewed_by        IS 'Employee (manager) who approved or rejected the request. NULL while pending (composite FK check is skipped while NULL). Must belong to the same tenant.';
COMMENT ON COLUMN hr.leave_requests.reviewed_at        IS 'Timestamp of the approval/rejection. NULL while pending.';
COMMENT ON COLUMN hr.leave_requests.created_by         IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN hr.leave_requests.updated_at         IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN hr.leave_requests.is_deleted         IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_hr_leave_requests_tenant_id
    ON hr.leave_requests (tenant_id);

CREATE INDEX IF NOT EXISTS ix_hr_leave_requests_employee_id
    ON hr.leave_requests (employee_id);

CREATE INDEX IF NOT EXISTS ix_hr_leave_requests_leave_status_id
    ON hr.leave_requests (leave_status_id);

CREATE INDEX IF NOT EXISTS ix_hr_leave_requests_start_date
    ON hr.leave_requests (start_date);

CREATE INDEX IF NOT EXISTS ix_hr_leave_requests_is_deleted
    ON hr.leave_requests (is_deleted);

COMMIT;
