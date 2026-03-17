-- Migration: 20260317_1320_T_vehicles_DDL.sql
-- Description: Create the customer.vehicles table. Each vehicle belongs to one
--              customer. tenant_id is denormalized to enable plate/VIN lookups
--              scoped to a tenant without joining customer.customers.
--              A trigger (20260317_1330) enforces vehicles.tenant_id = customers.tenant_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS customer.vehicles (
    id                  UUID         NOT NULL DEFAULT uuidv7(),
    customer_id         UUID         NOT NULL,
    tenant_id           UUID         NOT NULL,
    make                VARCHAR(100) NOT NULL,
    model               VARCHAR(100) NOT NULL,
    year                SMALLINT,
    vin                 VARCHAR(17),
    license_plate       VARCHAR(20),
    color               VARCHAR(50),
    fuel_type           VARCHAR(50),
    engine_type         VARCHAR(100),
    transmission        VARCHAR(50),
    mileage             INTEGER,
    mileage_recorded_at TIMESTAMPTZ,
    notes               TEXT,
    is_active           BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_customer_vehicles               PRIMARY KEY (id),
    CONSTRAINT fk_customer_vehicles_customer_id   FOREIGN KEY (customer_id) REFERENCES customer.customers(id),
    CONSTRAINT fk_customer_vehicles_tenant_id     FOREIGN KEY (tenant_id)   REFERENCES tenant.tenants(id),
    CONSTRAINT fk_customer_vehicles_created_by    FOREIGN KEY (created_by)  REFERENCES auth.users(id),
    CONSTRAINT fk_customer_vehicles_updated_by    FOREIGN KEY (updated_by)  REFERENCES auth.users(id)
);

COMMENT ON TABLE  customer.vehicles                       IS 'Vehicles owned by customers. tenant_id is denormalized for efficient plate/VIN lookups within a tenant.';
COMMENT ON COLUMN customer.vehicles.id                   IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN customer.vehicles.customer_id          IS 'The customer (owner) this vehicle belongs to.';
COMMENT ON COLUMN customer.vehicles.tenant_id            IS 'Denormalized tenant scope. Must match customer.tenant_id — enforced by trigger tr_vehicles_tenant_check.';
COMMENT ON COLUMN customer.vehicles.vin                  IS 'Vehicle Identification Number (17 characters).';
COMMENT ON COLUMN customer.vehicles.fuel_type            IS 'e.g. gasoline, diesel, electric, hybrid.';
COMMENT ON COLUMN customer.vehicles.transmission         IS 'e.g. automatic, manual, cvt.';
COMMENT ON COLUMN customer.vehicles.mileage              IS 'Last recorded odometer reading.';
COMMENT ON COLUMN customer.vehicles.mileage_recorded_at  IS 'Timestamp when the mileage value was last updated.';
COMMENT ON COLUMN customer.vehicles.notes                IS 'Internal workshop notes about this vehicle. Not visible to the customer.';
COMMENT ON COLUMN customer.vehicles.is_active            IS 'FALSE = vehicle record suspended / inactive.';
COMMENT ON COLUMN customer.vehicles.created_by           IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN customer.vehicles.updated_at           IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN customer.vehicles.is_deleted           IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_customer_vehicles_customer_id
    ON customer.vehicles (customer_id);

CREATE INDEX IF NOT EXISTS ix_customer_vehicles_tenant_id
    ON customer.vehicles (tenant_id);

CREATE INDEX IF NOT EXISTS ix_customer_vehicles_is_deleted
    ON customer.vehicles (is_deleted);

CREATE INDEX IF NOT EXISTS ix_customer_vehicles_vin
    ON customer.vehicles (vin);

COMMIT;
