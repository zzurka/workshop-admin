-- Migration: 20260330_1050_T_suppliers_DDL.sql
-- Description: Create the workshop.suppliers table. Stores parts suppliers
--              referenced by work_order_parts. Scoped to a single tenant.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.suppliers (
    id            UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id     UUID         NOT NULL,
    name          VARCHAR(255) NOT NULL,
    email         VARCHAR(255),
    phone_number  VARCHAR(50),
    address       TEXT,
    notes         TEXT,
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by    UUID,
    updated_at    TIMESTAMPTZ,
    updated_by    UUID,
    is_deleted    BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_suppliers                 PRIMARY KEY (id),
    CONSTRAINT uq_workshop_suppliers_tenant_id_id    UNIQUE (tenant_id, id),
    CONSTRAINT uq_workshop_suppliers_tenant_name     UNIQUE (tenant_id, name),
    CONSTRAINT fk_workshop_suppliers_tenant_id    FOREIGN KEY (tenant_id)  REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_suppliers_created_by   FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_suppliers_updated_by   FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  workshop.suppliers               IS 'Parts suppliers. Each supplier belongs to one tenant. Contacts are stored in workshop.supplier_contacts.';
COMMENT ON COLUMN workshop.suppliers.id            IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.suppliers.tenant_id     IS 'The tenant (workshop) this supplier belongs to.';
COMMENT ON COLUMN workshop.suppliers.name          IS 'Supplier company name. Must be unique within the tenant.';
COMMENT ON COLUMN workshop.suppliers.email         IS 'General company email address.';
COMMENT ON COLUMN workshop.suppliers.phone_number  IS 'General company phone number.';
COMMENT ON COLUMN workshop.suppliers.address       IS 'Supplier address (free text).';
COMMENT ON COLUMN workshop.suppliers.notes         IS 'Internal notes about this supplier.';
COMMENT ON COLUMN workshop.suppliers.is_active     IS 'FALSE = supplier deactivated / no longer used.';
COMMENT ON COLUMN workshop.suppliers.created_by    IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.suppliers.updated_at    IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.suppliers.is_deleted    IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_suppliers_tenant_id
    ON workshop.suppliers (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_suppliers_is_deleted
    ON workshop.suppliers (is_deleted);

COMMIT;
