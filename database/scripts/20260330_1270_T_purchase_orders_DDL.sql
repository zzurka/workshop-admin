-- Migration: 20260330_1270_T_purchase_orders_DDL.sql
-- Description: Create the warehouse.purchase_orders table. Orders placed with
--              parts suppliers: what was ordered, from whom, expected and
--              actual delivery. Lines are in warehouse.purchase_order_lines.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS warehouse.purchase_orders (
    id                       UUID        NOT NULL DEFAULT uuidv7(),
    tenant_id                UUID        NOT NULL,
    supplier_id              UUID        NOT NULL,
    purchase_order_status_id SMALLINT    NOT NULL,
    po_number                INTEGER     NOT NULL,
    po_year                  SMALLINT    NOT NULL,
    currency_id              SMALLINT    NOT NULL,
    ordered_at               TIMESTAMPTZ,
    expected_at              DATE,
    received_at              TIMESTAMPTZ,
    notes                    TEXT,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by               UUID,
    updated_at               TIMESTAMPTZ,
    updated_by               UUID,
    is_deleted               BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_warehouse_purchase_orders                          PRIMARY KEY (id),
    CONSTRAINT uq_warehouse_purchase_orders_tenant_id_id             UNIQUE (tenant_id, id),
    CONSTRAINT uq_warehouse_purchase_orders_tenant_year_number       UNIQUE (tenant_id, po_year, po_number),
    CONSTRAINT fk_warehouse_purchase_orders_tenant_id                FOREIGN KEY (tenant_id)                REFERENCES tenant.tenants(id),
    CONSTRAINT fk_warehouse_purchase_orders_supplier_id              FOREIGN KEY (tenant_id, supplier_id)   REFERENCES workshop.suppliers(tenant_id, id),
    CONSTRAINT fk_warehouse_purchase_orders_purchase_order_status_id FOREIGN KEY (purchase_order_status_id) REFERENCES codebook.purchase_order_statuses(id),
    CONSTRAINT fk_warehouse_purchase_orders_currency_id              FOREIGN KEY (currency_id)              REFERENCES codebook.currencies(id),
    CONSTRAINT fk_warehouse_purchase_orders_created_by               FOREIGN KEY (created_by)               REFERENCES auth.users(id),
    CONSTRAINT fk_warehouse_purchase_orders_updated_by               FOREIGN KEY (updated_by)               REFERENCES auth.users(id)
);

COMMENT ON TABLE  warehouse.purchase_orders                          IS 'Purchase orders to suppliers. Application flow: draft (created manually, from the low-stock report, or from work order parts with status ''ordered'') -> ordered (sent to supplier) -> partially_received/received; cancelled from draft/ordered. Status is derived from line receipts by the application.';
COMMENT ON COLUMN warehouse.purchase_orders.id                       IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN warehouse.purchase_orders.tenant_id                IS 'The tenant (workshop) this purchase order belongs to.';
COMMENT ON COLUMN warehouse.purchase_orders.supplier_id              IS 'The supplier the order is placed with. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN warehouse.purchase_orders.purchase_order_status_id IS 'FK to codebook.purchase_order_statuses (draft, ordered, partially_received, received, cancelled).';
COMMENT ON COLUMN warehouse.purchase_orders.po_number                IS 'Sequential number per (tenant, po_year), assigned at creation from warehouse.purchase_order_counters. Internal reference used with the supplier — gaps are acceptable. Displayed as {number}/{year}.';
COMMENT ON COLUMN warehouse.purchase_orders.po_year                  IS 'Numbering year for po_number.';
COMMENT ON COLUMN warehouse.purchase_orders.currency_id              IS 'FK to codebook.currencies. Snapshot of the tenant''s default currency at creation.';
COMMENT ON COLUMN warehouse.purchase_orders.ordered_at               IS 'When the order was sent to the supplier. NULL while draft.';
COMMENT ON COLUMN warehouse.purchase_orders.expected_at              IS 'Expected delivery date. NULL if unknown.';
COMMENT ON COLUMN warehouse.purchase_orders.received_at              IS 'When the order was fully received. NULL until then.';
COMMENT ON COLUMN warehouse.purchase_orders.created_by               IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN warehouse.purchase_orders.updated_at               IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN warehouse.purchase_orders.is_deleted               IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_orders_tenant_id
    ON warehouse.purchase_orders (tenant_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_orders_supplier_id
    ON warehouse.purchase_orders (supplier_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_orders_purchase_order_status_id
    ON warehouse.purchase_orders (purchase_order_status_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_purchase_orders_is_deleted
    ON warehouse.purchase_orders (is_deleted);

COMMIT;
