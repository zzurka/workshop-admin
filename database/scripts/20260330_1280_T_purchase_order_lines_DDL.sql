-- Migration: 20260330_1280_T_purchase_order_lines_DDL.sql
-- Description: Create the warehouse.purchase_order_lines table. Line items on
--              a purchase order, with partial-receipt tracking and an optional
--              link back to the work order part that triggered the order.
--              Carries a denormalized tenant_id (rule from plan 2.1: it
--              references multiple tenant-scoped tables).
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS warehouse.purchase_order_lines (
    id                 UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id          UUID          NOT NULL,
    purchase_order_id  UUID          NOT NULL,
    catalog_part_id    UUID,
    part_name          VARCHAR(255),
    part_number        VARCHAR(100),
    quantity           NUMERIC(10,2) NOT NULL,
    unit_cost          NUMERIC(12,2),
    received_quantity  NUMERIC(10,2) NOT NULL DEFAULT 0,
    work_order_part_id UUID,
    notes              TEXT,
    created_at         TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by         UUID,
    updated_at         TIMESTAMPTZ,
    updated_by         UUID,
    is_deleted         BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_warehouse_purchase_order_lines                    PRIMARY KEY (id),
    CONSTRAINT uq_warehouse_purchase_order_lines_tenant_id_id       UNIQUE (tenant_id, id),
    CONSTRAINT fk_warehouse_purchase_order_lines_tenant_id          FOREIGN KEY (tenant_id)                     REFERENCES tenant.tenants(id),
    CONSTRAINT fk_warehouse_purchase_order_lines_purchase_order_id  FOREIGN KEY (tenant_id, purchase_order_id)  REFERENCES warehouse.purchase_orders(tenant_id, id),
    CONSTRAINT fk_warehouse_purchase_order_lines_catalog_part_id    FOREIGN KEY (tenant_id, catalog_part_id)    REFERENCES warehouse.parts_catalog(tenant_id, id),
    CONSTRAINT fk_warehouse_purchase_order_lines_work_order_part_id FOREIGN KEY (tenant_id, work_order_part_id) REFERENCES workshop.work_order_parts(tenant_id, id),
    CONSTRAINT fk_warehouse_purchase_order_lines_created_by         FOREIGN KEY (created_by)                    REFERENCES auth.users(id),
    CONSTRAINT fk_warehouse_purchase_order_lines_updated_by         FOREIGN KEY (updated_by)                    REFERENCES auth.users(id),
    CONSTRAINT ck_warehouse_purchase_order_lines_quantity           CHECK (quantity > 0),
    CONSTRAINT ck_warehouse_purchase_order_lines_unit_cost          CHECK (unit_cost IS NULL OR unit_cost >= 0),
    CONSTRAINT ck_warehouse_purchase_order_lines_received_quantity  CHECK (received_quantity >= 0),
    CONSTRAINT ck_warehouse_purchase_order_lines_part_identity      CHECK (catalog_part_id IS NOT NULL OR part_name IS NOT NULL)
);

COMMENT ON TABLE  warehouse.purchase_order_lines                    IS 'Line items on a purchase order. Receipt bookings (stock_transactions with purchase_order_line_id) increment received_quantity; the application derives PO status from line receipts and updates parts_catalog.cost_price with the latest unit_cost on receipt.';
COMMENT ON COLUMN warehouse.purchase_order_lines.id                 IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN warehouse.purchase_order_lines.tenant_id          IS 'Denormalized tenant scope. Must match the tenant of the PO, catalog part, and work order part — enforced by the composite FKs.';
COMMENT ON COLUMN warehouse.purchase_order_lines.purchase_order_id  IS 'The purchase order this line belongs to. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN warehouse.purchase_order_lines.catalog_part_id    IS 'FK to warehouse.parts_catalog. NULL for parts not in the catalog (part_name is then required). Composite FK check is skipped while NULL.';
COMMENT ON COLUMN warehouse.purchase_order_lines.part_name          IS 'Fallback part name when the part is not in the catalog. Required if catalog_part_id is NULL.';
COMMENT ON COLUMN warehouse.purchase_order_lines.part_number        IS 'Manufacturer or supplier part number. NULL if unknown.';
COMMENT ON COLUMN warehouse.purchase_order_lines.quantity           IS 'Quantity ordered. Must be positive.';
COMMENT ON COLUMN warehouse.purchase_order_lines.unit_cost          IS 'Purchase price per unit. NULL until known/quoted.';
COMMENT ON COLUMN warehouse.purchase_order_lines.received_quantity  IS 'Quantity received so far — supports partial receipts. Reconciled against receipt stock_transactions via purchase_order_line_id.';
COMMENT ON COLUMN warehouse.purchase_order_lines.work_order_part_id IS 'Optional link to the work order part that triggered this order (part_status ''ordered''). Composite FK check is skipped while NULL.';
COMMENT ON COLUMN warehouse.purchase_order_lines.created_by         IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN warehouse.purchase_order_lines.updated_at         IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN warehouse.purchase_order_lines.is_deleted         IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_order_lines_tenant_id
    ON warehouse.purchase_order_lines (tenant_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_order_lines_purchase_order_id
    ON warehouse.purchase_order_lines (purchase_order_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_order_lines_catalog_part_id
    ON warehouse.purchase_order_lines (catalog_part_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_order_lines_work_order_part_id
    ON warehouse.purchase_order_lines (work_order_part_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_order_lines_is_deleted
    ON warehouse.purchase_order_lines (is_deleted);

COMMIT;
