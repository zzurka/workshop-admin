-- Migration: 20260330_1110_T_parts_catalog_DDL.sql
-- Description: Create the warehouse.parts_catalog table. Master list of parts
--              a workshop stocks, with barcode support and default pricing.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS warehouse.parts_catalog (
    id                  UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id           UUID          NOT NULL,
    part_number         VARCHAR(100),
    name                VARCHAR(255)  NOT NULL,
    barcode             VARCHAR(100),
    description         TEXT,
    unit_of_measure_id  SMALLINT      NOT NULL,
    default_supplier_id UUID,
    cost_price          NUMERIC(12,2),
    selling_price       NUMERIC(12,2),
    min_stock_level     NUMERIC(10,2) NOT NULL DEFAULT 0,
    is_active           BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_warehouse_parts_catalog                       PRIMARY KEY (id),
    CONSTRAINT uq_warehouse_parts_catalog_tenant_id_id          UNIQUE (tenant_id, id),
    CONSTRAINT uq_warehouse_parts_catalog_tenant_barcode        UNIQUE (tenant_id, barcode),
    CONSTRAINT fk_warehouse_parts_catalog_tenant_id             FOREIGN KEY (tenant_id)          REFERENCES tenant.tenants(id),
    CONSTRAINT fk_warehouse_parts_catalog_unit_of_measure_id    FOREIGN KEY (unit_of_measure_id) REFERENCES codebook.units_of_measure(id),
    CONSTRAINT fk_warehouse_parts_catalog_default_supplier_id   FOREIGN KEY (tenant_id, default_supplier_id) REFERENCES workshop.suppliers(tenant_id, id),
    CONSTRAINT fk_warehouse_parts_catalog_created_by            FOREIGN KEY (created_by)         REFERENCES auth.users(id),
    CONSTRAINT fk_warehouse_parts_catalog_updated_by            FOREIGN KEY (updated_by)         REFERENCES auth.users(id),
    CONSTRAINT ck_warehouse_parts_catalog_cost_price            CHECK (cost_price >= 0),
    CONSTRAINT ck_warehouse_parts_catalog_selling_price         CHECK (selling_price >= 0),
    CONSTRAINT ck_warehouse_parts_catalog_min_stock_level       CHECK (min_stock_level >= 0)
);

COMMENT ON TABLE  warehouse.parts_catalog                         IS 'Master catalog of parts a workshop stocks. Each part belongs to one tenant. Supports barcode scanning.';
COMMENT ON COLUMN warehouse.parts_catalog.id                      IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN warehouse.parts_catalog.tenant_id               IS 'The tenant (workshop) this part belongs to.';
COMMENT ON COLUMN warehouse.parts_catalog.part_number             IS 'Manufacturer or supplier part number. NULL if unknown.';
COMMENT ON COLUMN warehouse.parts_catalog.name                    IS 'Human-readable part name (e.g. "Oil filter for BMW 320d").';
COMMENT ON COLUMN warehouse.parts_catalog.barcode                 IS 'EAN/UPC or internal barcode. Unique per tenant. NULL if not barcoded.';
COMMENT ON COLUMN warehouse.parts_catalog.description             IS 'Optional detailed description of the part.';
COMMENT ON COLUMN warehouse.parts_catalog.unit_of_measure_id      IS 'FK to codebook.units_of_measure (e.g. piece, liter, kg).';
COMMENT ON COLUMN warehouse.parts_catalog.default_supplier_id     IS 'Optional FK to workshop.suppliers. Preferred supplier for reordering. Composite FK (tenant_id, default_supplier_id) guarantees the supplier belongs to the same tenant; check is skipped while NULL.';
COMMENT ON COLUMN warehouse.parts_catalog.cost_price              IS 'Default purchase price (what you pay the supplier). NULL if not yet known.';
COMMENT ON COLUMN warehouse.parts_catalog.selling_price           IS 'Default selling price (what you charge the customer). NULL if not yet set.';
COMMENT ON COLUMN warehouse.parts_catalog.min_stock_level         IS 'Minimum stock threshold. When quantity_on_hand drops below this, trigger reorder alert.';
COMMENT ON COLUMN warehouse.parts_catalog.is_active               IS 'FALSE = part discontinued / no longer stocked.';
COMMENT ON COLUMN warehouse.parts_catalog.created_by              IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN warehouse.parts_catalog.updated_at              IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN warehouse.parts_catalog.is_deleted              IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_warehouse_parts_catalog_tenant_id
    ON warehouse.parts_catalog (tenant_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_parts_catalog_barcode
    ON warehouse.parts_catalog (barcode);

CREATE INDEX IF NOT EXISTS ix_warehouse_parts_catalog_part_number
    ON warehouse.parts_catalog (part_number);

CREATE INDEX IF NOT EXISTS ix_warehouse_parts_catalog_is_deleted
    ON warehouse.parts_catalog (is_deleted);

COMMIT;
