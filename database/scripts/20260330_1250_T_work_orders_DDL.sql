-- Migration: 20260330_1250_T_work_orders_DDL.sql
-- Description: Create the workshop.work_orders table. Created after appointment
--              inspection. Multiple work orders per appointment are supported
--              (e.g. two mechanics working on different jobs for the same car).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.work_orders (
    id                     UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id              UUID         NOT NULL,
    appointment_id         UUID         NOT NULL,
    employee_id            UUID         NOT NULL,
    work_order_status_id   SMALLINT     NOT NULL,
    description            TEXT,
    started_at             TIMESTAMPTZ,
    completed_at           TIMESTAMPTZ,
    notes                  TEXT,
    created_at             TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by             UUID,
    updated_at             TIMESTAMPTZ,
    updated_by             UUID,
    is_deleted             BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_work_orders                       PRIMARY KEY (id),
    CONSTRAINT uq_workshop_work_orders_tenant_id_id          UNIQUE (tenant_id, id),
    CONSTRAINT fk_workshop_work_orders_tenant_id             FOREIGN KEY (tenant_id)            REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_work_orders_appointment_id        FOREIGN KEY (tenant_id, appointment_id) REFERENCES workshop.appointments(tenant_id, id),
    CONSTRAINT fk_workshop_work_orders_employee_id           FOREIGN KEY (tenant_id, employee_id)    REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_workshop_work_orders_work_order_status_id  FOREIGN KEY (work_order_status_id) REFERENCES codebook.work_order_statuses(id),
    CONSTRAINT fk_workshop_work_orders_created_by            FOREIGN KEY (created_by)           REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_work_orders_updated_by            FOREIGN KEY (updated_by)           REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_work_orders_completed_range       CHECK (completed_at IS NULL OR completed_at >= started_at)
);

COMMENT ON TABLE  workshop.work_orders                       IS 'Work orders created after vehicle inspection. Multiple per appointment (different mechanics/jobs). Tracks status from pending_parts through completion.';
COMMENT ON COLUMN workshop.work_orders.id                    IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.work_orders.tenant_id             IS 'The tenant (workshop) this work order belongs to.';
COMMENT ON COLUMN workshop.work_orders.appointment_id        IS 'The appointment this work order was created from. Multiple work orders can reference the same appointment. Composite FK (tenant_id, appointment_id) guarantees the appointment belongs to the same tenant.';
COMMENT ON COLUMN workshop.work_orders.employee_id           IS 'The mechanic/employee assigned to this work order. Composite FK (tenant_id, employee_id) guarantees the employee belongs to the same tenant.';
COMMENT ON COLUMN workshop.work_orders.work_order_status_id  IS 'FK to codebook.work_order_statuses (pending_parts, ready, in_progress, completed, cancelled).';
COMMENT ON COLUMN workshop.work_orders.description           IS 'Description of the work to be done on this order.';
COMMENT ON COLUMN workshop.work_orders.started_at            IS 'Timestamp when work actually started. NULL until in_progress.';
COMMENT ON COLUMN workshop.work_orders.completed_at          IS 'Timestamp when work was completed. NULL until completed.';
COMMENT ON COLUMN workshop.work_orders.notes                 IS 'Internal notes about this work order.';
COMMENT ON COLUMN workshop.work_orders.created_by            IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.work_orders.updated_at            IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.work_orders.is_deleted            IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_work_orders_tenant_id
    ON workshop.work_orders (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_orders_appointment_id
    ON workshop.work_orders (appointment_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_orders_employee_id
    ON workshop.work_orders (employee_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_orders_work_order_status_id
    ON workshop.work_orders (work_order_status_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_orders_is_deleted
    ON workshop.work_orders (is_deleted);

COMMIT;
