-- Migration: 20260330_1260_T_work_order_parts_DDL.sql
-- Description: Create the workshop.work_order_parts table. Tracks parts needed
--              for a work order, their availability status, and supplier.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.work_order_parts (
    id              UUID          NOT NULL DEFAULT uuidv7(),
    work_order_id   UUID          NOT NULL,
    supplier_id     UUID,
    part_status_id  SMALLINT      NOT NULL,
    part_name       VARCHAR(255)  NOT NULL,
    part_number     VARCHAR(100),
    quantity        SMALLINT      NOT NULL DEFAULT 1,
    unit_price      NUMERIC(10,2),
    notes           TEXT,
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID,
    is_deleted      BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_work_order_parts                PRIMARY KEY (id),
    CONSTRAINT fk_workshop_work_order_parts_work_order_id  FOREIGN KEY (work_order_id)  REFERENCES workshop.work_orders(id),
    CONSTRAINT fk_workshop_work_order_parts_supplier_id    FOREIGN KEY (supplier_id)    REFERENCES workshop.suppliers(id),
    CONSTRAINT fk_workshop_work_order_parts_part_status_id FOREIGN KEY (part_status_id) REFERENCES codebook.part_statuses(id),
    CONSTRAINT fk_workshop_work_order_parts_created_by     FOREIGN KEY (created_by)     REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_work_order_parts_updated_by     FOREIGN KEY (updated_by)     REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_work_order_parts_quantity        CHECK (quantity > 0)
);

COMMENT ON TABLE  workshop.work_order_parts                 IS 'Parts needed for a work order. Tracks part details, supplier, availability status, and pricing.';
COMMENT ON COLUMN workshop.work_order_parts.id              IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.work_order_parts.work_order_id   IS 'The work order this part belongs to.';
COMMENT ON COLUMN workshop.work_order_parts.supplier_id     IS 'FK to workshop.suppliers. NULL if supplier not yet determined or part is in stock.';
COMMENT ON COLUMN workshop.work_order_parts.part_status_id  IS 'FK to codebook.part_statuses (in_stock, ordered, received).';
COMMENT ON COLUMN workshop.work_order_parts.part_name       IS 'Human-readable part name (e.g. "Oil filter for BMW 320d").';
COMMENT ON COLUMN workshop.work_order_parts.part_number     IS 'Manufacturer or supplier part number. NULL if unknown.';
COMMENT ON COLUMN workshop.work_order_parts.quantity         IS 'Number of units needed. Must be at least 1.';
COMMENT ON COLUMN workshop.work_order_parts.unit_price       IS 'Price per unit. NULL if not yet quoted.';
COMMENT ON COLUMN workshop.work_order_parts.notes            IS 'Notes about this part (e.g. "OEM only, no aftermarket").';
COMMENT ON COLUMN workshop.work_order_parts.is_active        IS 'FALSE = part line voided / removed from order.';
COMMENT ON COLUMN workshop.work_order_parts.created_by       IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.work_order_parts.updated_at       IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.work_order_parts.is_deleted       IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_parts_work_order_id
    ON workshop.work_order_parts (work_order_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_parts_supplier_id
    ON workshop.work_order_parts (supplier_id);

CREATE INDEX IF NOT EXISTS ix_workshop_work_order_parts_is_deleted
    ON workshop.work_order_parts (is_deleted);

COMMIT;
