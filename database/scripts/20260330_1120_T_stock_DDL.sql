-- Migration: 20260330_1120_T_stock_DDL.sql
-- Description: Create the warehouse.stock table. Tracks current inventory
--              levels per part per tenant.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS warehouse.stock (
    id               UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id        UUID          NOT NULL,
    catalog_part_id  UUID          NOT NULL,
    quantity_on_hand NUMERIC(10,2) NOT NULL DEFAULT 0,
    bin_location     VARCHAR(50),
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by       UUID,
    updated_at       TIMESTAMPTZ,
    updated_by       UUID,

    CONSTRAINT pk_warehouse_stock                    PRIMARY KEY (id),
    CONSTRAINT uq_warehouse_stock_tenant_part        UNIQUE (tenant_id, catalog_part_id),
    CONSTRAINT fk_warehouse_stock_tenant_id          FOREIGN KEY (tenant_id)       REFERENCES tenant.tenants(id),
    CONSTRAINT fk_warehouse_stock_catalog_part_id    FOREIGN KEY (catalog_part_id) REFERENCES warehouse.parts_catalog(id),
    CONSTRAINT fk_warehouse_stock_created_by         FOREIGN KEY (created_by)      REFERENCES auth.users(id),
    CONSTRAINT fk_warehouse_stock_updated_by         FOREIGN KEY (updated_by)      REFERENCES auth.users(id),
    CONSTRAINT ck_warehouse_stock_quantity_on_hand    CHECK (quantity_on_hand >= 0)
);

COMMENT ON TABLE  warehouse.stock                      IS 'Current inventory levels. One row per part per tenant. Updated atomically with each stock_transaction.';
COMMENT ON COLUMN warehouse.stock.id                   IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN warehouse.stock.tenant_id            IS 'The tenant (workshop) this stock belongs to.';
COMMENT ON COLUMN warehouse.stock.catalog_part_id      IS 'FK to warehouse.parts_catalog. The part being tracked.';
COMMENT ON COLUMN warehouse.stock.quantity_on_hand      IS 'Current quantity in stock. Must be >= 0. Unit is defined by the part catalog entry.';
COMMENT ON COLUMN warehouse.stock.bin_location          IS 'Physical location in the warehouse (e.g. "A3-12", "Shelf B"). NULL if not tracked.';
COMMENT ON COLUMN warehouse.stock.created_by           IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN warehouse.stock.updated_at           IS 'NULL on creation. Set on any update.';

CREATE INDEX IF NOT EXISTS ix_warehouse_stock_tenant_id
    ON warehouse.stock (tenant_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_stock_catalog_part_id
    ON warehouse.stock (catalog_part_id);

COMMIT;
