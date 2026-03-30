-- Migration: 20260330_1240_T_appointment_services_DDL.sql
-- Description: Create the workshop.appointment_services junction table.
--              Links appointments to codebook.service_types (oil change,
--              timing belt, brake fluid, etc.) as structured indicators.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.appointment_services (
    id              UUID        NOT NULL DEFAULT uuidv7(),
    appointment_id  UUID        NOT NULL,
    service_type_id SMALLINT    NOT NULL,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID,

    CONSTRAINT pk_workshop_appointment_services                  PRIMARY KEY (id),
    CONSTRAINT uq_workshop_appointment_services_appt_service     UNIQUE (appointment_id, service_type_id),
    CONSTRAINT fk_workshop_appointment_services_appointment_id   FOREIGN KEY (appointment_id)  REFERENCES workshop.appointments(id),
    CONSTRAINT fk_workshop_appointment_services_service_type_id  FOREIGN KEY (service_type_id) REFERENCES codebook.service_types(id),
    CONSTRAINT fk_workshop_appointment_services_created_by       FOREIGN KEY (created_by)      REFERENCES auth.users(id)
);

COMMENT ON TABLE  workshop.appointment_services                  IS 'Junction table linking appointments to known service types (e.g. oil change, timing belt). Acts as structured indicators alongside the free-text description.';
COMMENT ON COLUMN workshop.appointment_services.id               IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.appointment_services.appointment_id   IS 'The appointment this service indicator belongs to.';
COMMENT ON COLUMN workshop.appointment_services.service_type_id  IS 'FK to codebook.service_types. One row per service type per appointment.';
COMMENT ON COLUMN workshop.appointment_services.notes            IS 'Optional notes specific to this service (e.g. "customer says brakes are squeaking").';
COMMENT ON COLUMN workshop.appointment_services.created_by       IS 'User who created this record. NULL for system/seed records.';

CREATE INDEX IF NOT EXISTS ix_workshop_appointment_services_appointment_id
    ON workshop.appointment_services (appointment_id);

COMMIT;
