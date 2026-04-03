-- Migration: 20260330_1310_T_invoices_DDL.sql
-- Description: Create the workshop.invoices table. Represents a bill to the
--              customer for work done. Can be linked to an appointment (full
--              service visit) or stand-alone (walk-in quick jobs like bulb change).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.invoices (
    id                  UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id           UUID          NOT NULL,
    customer_id         UUID          NOT NULL,
    vehicle_id          UUID          NOT NULL,
    appointment_id      UUID,
    invoice_status_id   SMALLINT      NOT NULL,
    issued_at           TIMESTAMPTZ,
    paid_at             TIMESTAMPTZ,
    notes               TEXT,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_invoices                       PRIMARY KEY (id),
    CONSTRAINT fk_workshop_invoices_tenant_id             FOREIGN KEY (tenant_id)         REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_invoices_customer_id           FOREIGN KEY (customer_id)       REFERENCES customer.customers(id),
    CONSTRAINT fk_workshop_invoices_vehicle_id            FOREIGN KEY (vehicle_id)        REFERENCES customer.vehicles(id),
    CONSTRAINT fk_workshop_invoices_appointment_id        FOREIGN KEY (appointment_id)    REFERENCES workshop.appointments(id),
    CONSTRAINT fk_workshop_invoices_invoice_status_id     FOREIGN KEY (invoice_status_id) REFERENCES codebook.invoice_statuses(id),
    CONSTRAINT fk_workshop_invoices_created_by            FOREIGN KEY (created_by)        REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_invoices_updated_by            FOREIGN KEY (updated_by)        REFERENCES auth.users(id)
);

COMMENT ON TABLE  workshop.invoices                       IS 'Customer invoices. Linked to an appointment for full service visits, or stand-alone for walk-in quick jobs.';
COMMENT ON COLUMN workshop.invoices.id                    IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.invoices.tenant_id             IS 'The tenant (workshop) this invoice belongs to.';
COMMENT ON COLUMN workshop.invoices.customer_id           IS 'The customer being billed.';
COMMENT ON COLUMN workshop.invoices.vehicle_id            IS 'The vehicle that was serviced.';
COMMENT ON COLUMN workshop.invoices.appointment_id        IS 'Optional link to appointment. NULL for walk-in quick jobs (e.g. bulb change).';
COMMENT ON COLUMN workshop.invoices.invoice_status_id     IS 'FK to codebook.invoice_statuses (draft, issued, paid, cancelled).';
COMMENT ON COLUMN workshop.invoices.issued_at             IS 'Timestamp when the invoice was issued to the customer. NULL while draft.';
COMMENT ON COLUMN workshop.invoices.paid_at               IS 'Timestamp when the invoice was paid. NULL until paid.';
COMMENT ON COLUMN workshop.invoices.notes                 IS 'Internal notes about this invoice.';
COMMENT ON COLUMN workshop.invoices.created_by            IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.invoices.updated_at            IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.invoices.is_deleted            IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_tenant_id
    ON workshop.invoices (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_customer_id
    ON workshop.invoices (customer_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_vehicle_id
    ON workshop.invoices (vehicle_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_appointment_id
    ON workshop.invoices (appointment_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_invoice_status_id
    ON workshop.invoices (invoice_status_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_is_deleted
    ON workshop.invoices (is_deleted);

COMMIT;
