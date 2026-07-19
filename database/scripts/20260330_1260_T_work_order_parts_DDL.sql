-- Migration: 20260330_1260_T_work_order_parts_DDL.sql
-- Description: Create the workshop.work_order_parts table. Tracks parts needed
--              for a work order, their availability status, and supplier.
--              Carries a denormalized tenant_id (exception to the child-table
--              convention) because it references multiple tenant-scoped tables
--              (work_orders, parts_catalog, suppliers) — composite FKs need it
--              to guarantee all references stay within one tenant.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.work_order_parts (
    id              UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id       UUID          NOT NULL,
    work_order_id   UUID          NOT NULL,
    catalog_part_id UUID,
    supplier_id     UUID,
    part_status_id  SMALLINT      NOT NULL,
    part_name       VARCHAR(255)  NOT NULL,
    part_number     VARCHAR(100),
    quantity        NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_price      NUMERIC(10,2),
    notes           TEXT,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_work_order_parts                 PRIMARY KEY (id),
    CONSTRAINT uq_workshop_work_order_parts_tenant_id_id    UNIQUE (tenant_id, id),
    CONSTRAINT fk_workshop_work_order_parts_tenant_id       FOREIGN KEY (tenant_id)                  REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_work_order_parts_work_order_id   FOREIGN KEY (tenant_id, work_order_id)   REFERENCES workshop.work_orders(tenant_id, id),
    CONSTRAINT fk_workshop_work_order_parts_supplier_id     FOREIGN KEY (tenant_id, supplier_id)     REFERENCES workshop.suppliers(tenant_id, id),
    CONSTRAINT fk_workshop_work_order_parts_catalog_part_id FOREIGN KEY (tenant_id, catalog_part_id) REFERENCES warehouse.parts_catalog(tenant_id, id),
    CONSTRAINT fk_workshop_work_order_parts_part_status_id  FOREIGN KEY (part_status_id)             REFERENCES codebook.part_statuses(id),
    CONSTRAINT fk_workshop_work_order_parts_created_by      FOREIGN KEY (created_by)                 REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_work_order_parts_updated_by      FOREIGN KEY (updated_by)                 REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_work_order_parts_quantity        CHECK (quantity > 0)
);

COMMENT ON TABLE  workshop.work_order_parts                  IS 'Parts needed for a work order. Tracks part details, supplier, availability status, and pricing. Has a denormalized tenant_id (exception to the child-table convention) because it references multiple tenant-scoped tables — composite FKs keep every reference within one tenant.';
COMMENT ON COLUMN workshop.work_order_parts.id               IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.work_order_parts.tenant_id        IS 'Denormalized tenant scope. Must match the tenant of work_order, catalog part, and supplier — enforced by the composite FKs.';
COMMENT ON COLUMN workshop.work_order_parts.work_order_id    IS 'The work order this part belongs to. Composite FK (tenant_id, work_order_id) guarantees the work order belongs to the same tenant.';
COMMENT ON COLUMN workshop.work_order_parts.catalog_part_id  IS 'FK to warehouse.parts_catalog. Set when part comes from warehouse stock. NULL if externally sourced. Composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.work_order_parts.supplier_id      IS 'FK to workshop.suppliers. NULL if supplier not yet determined or part is from warehouse. Composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.work_order_parts.part_status_id   IS 'FK to codebook.part_statuses (in_stock, ordered, received, issued, returned, cancelled). ''issued'' means physically pulled from warehouse stock (stock_transactions reduced quantity_on_hand) — warehouse.v_stock_availability treats in_stock/received as still-reserved but not issued, since issued parts are already reflected in quantity_on_hand.';
COMMENT ON COLUMN workshop.work_order_parts.part_name        IS 'Human-readable part name (e.g. "Oil filter for BMW 320d").';
COMMENT ON COLUMN workshop.work_order_parts.part_number      IS 'Manufacturer or supplier part number. NULL if unknown.';
COMMENT ON COLUMN workshop.work_order_parts.quantity         IS 'Number of units needed. NUMERIC to support fractional quantities (e.g. 1.5 L of oil), consistent with stock and invoice line quantities. Must be positive.';
COMMENT ON COLUMN workshop.work_order_parts.unit_price       IS 'Price per unit. NULL if not yet quoted.';
COMMENT ON COLUMN workshop.work_order_parts.notes            IS 'Notes about this part (e.g. "OEM only, no aftermarket").';
COMMENT ON COLUMN workshop.work_order_parts.created_by       IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.work_order_parts.updated_at       IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.work_order_parts.is_deleted       IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_parts_tenant_id
    ON workshop.work_order_parts (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_parts_work_order_id
    ON workshop.work_order_parts (work_order_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_parts_supplier_id
    ON workshop.work_order_parts (supplier_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_parts_is_deleted
    ON workshop.work_order_parts (is_deleted);

COMMIT;
