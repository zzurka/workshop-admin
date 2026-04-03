-- Migration: 20260330_1220_T_supplier_contacts_DDL.sql
-- Description: Create the workshop.supplier_contacts table. Multiple contacts
--              per supplier with optional primary contact flag.
--              Tenant is derived via supplier_id → workshop.suppliers.tenant_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.supplier_contacts (
    id            UUID         NOT NULL DEFAULT uuidv7(),
    supplier_id   UUID         NOT NULL,
    first_name    VARCHAR(100) NOT NULL,
    last_name     VARCHAR(100) NOT NULL,
    email         VARCHAR(255),
    phone_number  VARCHAR(50),
    is_primary    BOOLEAN      NOT NULL DEFAULT FALSE,
    notes         TEXT,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by    UUID,
    updated_at    TIMESTAMPTZ,
    updated_by    UUID,
    is_deleted    BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_supplier_contacts             PRIMARY KEY (id),
    CONSTRAINT fk_workshop_supplier_contacts_supplier_id FOREIGN KEY (supplier_id) REFERENCES workshop.suppliers(id),
    CONSTRAINT fk_workshop_supplier_contacts_created_by  FOREIGN KEY (created_by)  REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_supplier_contacts_updated_by  FOREIGN KEY (updated_by)  REFERENCES auth.users(id)
);

COMMENT ON TABLE  workshop.supplier_contacts               IS 'Contact persons for a supplier. Multiple contacts per supplier, with optional primary flag. Tenant derived via supplier.';
COMMENT ON COLUMN workshop.supplier_contacts.id            IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.supplier_contacts.supplier_id   IS 'The supplier this contact belongs to. Tenant is derived from the supplier row.';
COMMENT ON COLUMN workshop.supplier_contacts.first_name    IS 'Contact first name.';
COMMENT ON COLUMN workshop.supplier_contacts.last_name     IS 'Contact last name.';
COMMENT ON COLUMN workshop.supplier_contacts.email         IS 'Contact email address.';
COMMENT ON COLUMN workshop.supplier_contacts.phone_number  IS 'Contact phone number.';
COMMENT ON COLUMN workshop.supplier_contacts.is_primary    IS 'TRUE = primary contact for this supplier. Enforced at application level (at most one per supplier).';
COMMENT ON COLUMN workshop.supplier_contacts.notes         IS 'Notes about this contact (e.g. role, availability).';
COMMENT ON COLUMN workshop.supplier_contacts.created_by    IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.supplier_contacts.updated_at    IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.supplier_contacts.is_deleted    IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_supplier_contacts_supplier_id
    ON workshop.supplier_contacts (supplier_id);

CREATE INDEX IF NOT EXISTS ix_workshop_supplier_contacts_is_deleted
    ON workshop.supplier_contacts (is_deleted);

COMMIT;
