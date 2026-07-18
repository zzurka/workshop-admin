-- Migration: 20260330_1255_T_work_order_labor_DDL.sql
-- Description: Create the workshop.work_order_labor table. Billable labor per
--              work order: who worked, how many hours, at what billing rate.
--              Distinct concepts: hr.time_entries = attendance (clock-in/out,
--              for HR/payroll); this table = billable work (for invoicing);
--              the billing rate (charged to the customer) is not the employee
--              cost (hr.employee_compensations).
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.work_order_labor (
    id            UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id     UUID          NOT NULL,
    work_order_id UUID          NOT NULL,
    employee_id   UUID          NOT NULL,
    work_date     DATE          NOT NULL DEFAULT CURRENT_DATE,
    hours         NUMERIC(6,2)  NOT NULL,
    hourly_rate   NUMERIC(10,2) NOT NULL,
    description   VARCHAR(500),
    notes         TEXT,
    created_at    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by    UUID,
    updated_at    TIMESTAMPTZ,
    updated_by    UUID,
    is_deleted    BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_work_order_labor               PRIMARY KEY (id),
    CONSTRAINT uq_workshop_work_order_labor_tenant_id_id  UNIQUE (tenant_id, id),
    CONSTRAINT fk_workshop_work_order_labor_tenant_id     FOREIGN KEY (tenant_id)                REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_work_order_labor_work_order_id FOREIGN KEY (tenant_id, work_order_id) REFERENCES workshop.work_orders(tenant_id, id),
    CONSTRAINT fk_workshop_work_order_labor_employee_id   FOREIGN KEY (tenant_id, employee_id)   REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_workshop_work_order_labor_created_by    FOREIGN KEY (created_by)               REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_work_order_labor_updated_by    FOREIGN KEY (updated_by)               REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_work_order_labor_hours         CHECK (hours > 0),
    CONSTRAINT ck_workshop_work_order_labor_hourly_rate   CHECK (hourly_rate >= 0)
);

COMMENT ON TABLE  workshop.work_order_labor               IS 'Billable labor entries per work order — the source for labor lines on invoices. Hours are entered manually (no start/stop tracking in v1); attendance stays in hr.time_entries with no DB link (reconciliation is a report, not a constraint).';
COMMENT ON COLUMN workshop.work_order_labor.id            IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.work_order_labor.tenant_id     IS 'The tenant (workshop) this labor entry belongs to.';
COMMENT ON COLUMN workshop.work_order_labor.work_order_id IS 'The work order the labor was performed on. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN workshop.work_order_labor.employee_id   IS 'The employee (mechanic) who performed the work. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN workshop.work_order_labor.work_date     IS 'The date the work was performed.';
COMMENT ON COLUMN workshop.work_order_labor.hours         IS 'Hours worked, entered manually (e.g. 2.5). Must be positive. Becomes quantity on the invoice line.';
COMMENT ON COLUMN workshop.work_order_labor.hourly_rate   IS 'Billing rate charged to the customer, snapshotted at entry (prefilled from tenants.default_labor_rate). A later change of the default never affects existing entries. Becomes unit_price on the invoice line.';
COMMENT ON COLUMN workshop.work_order_labor.description   IS 'What was done — used as the invoice line description.';
COMMENT ON COLUMN workshop.work_order_labor.notes         IS 'Internal notes, not shown on the invoice.';
COMMENT ON COLUMN workshop.work_order_labor.created_by    IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.work_order_labor.updated_at    IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.work_order_labor.is_deleted    IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_labor_tenant_id
    ON workshop.work_order_labor (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_labor_work_order_id
    ON workshop.work_order_labor (work_order_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_labor_employee_id
    ON workshop.work_order_labor (employee_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_labor_is_deleted
    ON workshop.work_order_labor (is_deleted);

COMMIT;
