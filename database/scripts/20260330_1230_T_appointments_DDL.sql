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
    source                  VARCHAR(10)  NOT NULL DEFAULT 'staff',
    preferred_date          DATE,
    scheduled_date          DATE,
    scheduled_time          TIME,
    queue_position          INTEGER,
    estimated_duration_min  SMALLINT,
    arrived_at              TIMESTAMPTZ,
    description             TEXT,
    notes                   TEXT,
    created_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by              UUID,
    updated_at              TIMESTAMPTZ,
    updated_by              UUID,
    is_deleted              BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_appointments                        PRIMARY KEY (id),
    CONSTRAINT uq_workshop_appointments_tenant_id_id           UNIQUE (tenant_id, id),
    CONSTRAINT fk_workshop_appointments_tenant_id              FOREIGN KEY (tenant_id)             REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_appointments_customer_id            FOREIGN KEY (tenant_id, customer_id) REFERENCES customer.customers(tenant_id, id),
    CONSTRAINT fk_workshop_appointments_vehicle_id             FOREIGN KEY (tenant_id, vehicle_id)  REFERENCES customer.vehicles(tenant_id, id),
    CONSTRAINT fk_workshop_appointments_appointment_status_id  FOREIGN KEY (appointment_status_id) REFERENCES codebook.appointment_statuses(id),
    CONSTRAINT fk_workshop_appointments_created_by             FOREIGN KEY (created_by)            REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_appointments_updated_by             FOREIGN KEY (updated_by)            REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_appointments_source                 CHECK (source IN ('staff', 'portal', 'walk_in')),
    CONSTRAINT ck_workshop_appointments_queue_position         CHECK (queue_position IS NULL OR queue_position > 0),
    CONSTRAINT ck_workshop_appointments_duration_min           CHECK (estimated_duration_min IS NULL OR estimated_duration_min > 0)
);

COMMENT ON TABLE  workshop.appointments                        IS 'Vehicle intake / service appointments — the universal record for a vehicle entering the shop, whether booked ahead (staff or portal) or a same-day walk-in (source=''walk_in''). A work order (0..n) is created from an appointment only when a mechanic or tracked parts are involved; trivial jobs (e.g. topping up oil, a bulb) can go straight to an invoice off the appointment with no work order.';
COMMENT ON COLUMN workshop.appointments.id                     IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.appointments.tenant_id              IS 'The tenant (workshop) this appointment belongs to.';
COMMENT ON COLUMN workshop.appointments.customer_id            IS 'The customer requesting service. Composite FK (tenant_id, customer_id) guarantees the customer belongs to the same tenant.';
COMMENT ON COLUMN workshop.appointments.vehicle_id             IS 'The vehicle to be serviced. Composite FK (tenant_id, vehicle_id) guarantees the vehicle belongs to the same tenant.';
COMMENT ON COLUMN workshop.appointments.appointment_status_id  IS 'FK to codebook.appointment_statuses (scheduled, confirmed, in_progress, completed, no_show, cancelled).';
COMMENT ON COLUMN workshop.appointments.source                 IS 'Booking channel. ''staff'' = booked by workshop staff (phone or in person); ''portal'' = customer self-service booking; ''walk_in'' = vehicle arrived with no prior booking — the app creates the appointment at arrival (scheduled_date = today, arrived_at = NOW(), queued at/near the end of today''s order).';
COMMENT ON COLUMN workshop.appointments.preferred_date         IS 'Date the customer would prefer — set on portal bookings before staff confirms a day. NULL if no preference.';
COMMENT ON COLUMN workshop.appointments.scheduled_date         IS 'Confirmed service day. NULL until staff confirms one (status moves to confirmed). Queue order is scoped within this day — see queue_position.';
COMMENT ON COLUMN workshop.appointments.scheduled_time         IS 'Optional precise time within scheduled_date. NULL means "taken in queue order" — the common case, since shops typically work first-come-first-served within the day.';
COMMENT ON COLUMN workshop.appointments.queue_position         IS 'Order in which vehicles are taken into work within (tenant_id, scheduled_date); CHECK > 0. Assigned as the next position when the appointment is confirmed, following booking order (created_at) by default — this is how "booking order is honored" holds without extra logic. Staff can freely reorder same-day (e.g. drag-and-drop), so this is intentionally NOT unique at the DB level — the app owns the invariant; index (tenant_id, scheduled_date, queue_position) supports reading the day''s queue.';
COMMENT ON COLUMN workshop.appointments.estimated_duration_min IS 'Estimated service duration in minutes, used for day-capacity planning. The app prefills this as the sum of default_duration_min across the appointment''s service types (codebook.service_types); staff may override.';
COMMENT ON COLUMN workshop.appointments.arrived_at             IS 'When the vehicle physically arrived at the shop. Walk-ins are queued on arrival; scheduled appointments may also record this at check-in.';
COMMENT ON COLUMN workshop.appointments.description            IS 'Free-text description of what the customer needs — written during intake (phone call or walk-in).';
COMMENT ON COLUMN workshop.appointments.notes                  IS 'Internal workshop notes about this appointment.';
COMMENT ON COLUMN workshop.appointments.created_by             IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.appointments.updated_at             IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.appointments.is_deleted             IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_tenant_id
    ON workshop.appointments (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_customer_id
    ON workshop.appointments (customer_id);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_vehicle_id
    ON workshop.appointments (vehicle_id);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_tenant_date_queue
    ON workshop.appointments (tenant_id, scheduled_date, queue_position);

CREATE INDEX IF NOT EXISTS ix_workshop_appointments_is_deleted
    ON workshop.appointments (is_deleted);

COMMIT;
