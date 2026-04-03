-- Migration: 20260330_1230_T_appointments_DDL.sql
-- Description: Create the workshop.appointments table. Represents a customer
--              bringing in (or scheduling) a vehicle for service. Stores the
--              customer's description of what they need, plus structured service
--              indicators via the appointment_services junction table.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.appointments (
    id                      UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id               UUID         NOT NULL,
    customer_id             UUID         NOT NULL,
    vehicle_id              UUID         NOT NULL,
    appointment_status_id   SMALLINT     NOT NULL,
    preferred_date          DATE,
    scheduled_at            TIMESTAMPTZ,
    description             TEXT,
    notes                   TEXT,
    is_active               BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_at              TIMESTAMPTZ,
    updated_by              UUID,
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_appointments                        PRIMARY KEY (id),
    CONSTRAINT fk_workshop_appointments_tenant_id              FOREIGN KEY (tenant_id)             REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_appointments_customer_id            FOREIGN KEY (customer_id)           REFERENCES customer.customers(id),
    CONSTRAINT fk_workshop_appointments_vehicle_id             FOREIGN KEY (vehicle_id)            REFERENCES customer.vehicles(id),
    CONSTRAINT fk_workshop_appointments_appointment_status_id  FOREIGN KEY (appointment_status_id) REFERENCES codebook.appointment_statuses(id),
    CONSTRAINT fk_workshop_appointments_created_by             FOREIGN KEY (created_by)            REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_appointments_updated_by             FOREIGN KEY (updated_by)            REFERENCES auth.users(id)
);

COMMENT ON TABLE  workshop.appointments                        IS 'Service appointments. Created when a customer calls or walks in to schedule vehicle service.';
COMMENT ON COLUMN workshop.appointments.id                     IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.appointments.tenant_id              IS 'The tenant (workshop) this appointment belongs to.';
COMMENT ON COLUMN workshop.appointments.customer_id            IS 'The customer requesting service.';
COMMENT ON COLUMN workshop.appointments.vehicle_id             IS 'The vehicle to be serviced.';
COMMENT ON COLUMN workshop.appointments.appointment_status_id  IS 'FK to codebook.appointment_statuses (scheduled, confirmed, completed, cancelled).';
COMMENT ON COLUMN workshop.appointments.preferred_date         IS 'Date the customer would prefer. NULL if no preference.';
COMMENT ON COLUMN workshop.appointments.scheduled_at           IS 'Confirmed date/time. NULL until the workshop confirms a slot.';
COMMENT ON COLUMN workshop.appointments.description            IS 'Free-text description of what the customer needs — written during intake (phone call or walk-in).';
COMMENT ON COLUMN workshop.appointments.notes                  IS 'Internal workshop notes about this appointment.';
COMMENT ON COLUMN workshop.appointments.is_active              IS 'FALSE = appointment voided / inactive.';
COMMENT ON COLUMN workshop.appointments.created_by             IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.appointments.updated_at             IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.appointments.is_deleted             IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_tenant_id
    ON workshop.appointments (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_customer_id
    ON workshop.appointments (customer_id);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_vehicle_id
    ON workshop.appointments (vehicle_id);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_scheduled_at
    ON workshop.appointments (scheduled_at);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_is_deleted
    ON workshop.appointments (is_deleted);

COMMIT;
