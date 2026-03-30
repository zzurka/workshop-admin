-- Migration: 20260330_1320_T_invoice_lines_DDL.sql
-- Description: Create the workshop.invoice_lines table. Individual line items
--              on an invoice — parts (linked back to work_order_parts) and
--              labor charges (linked back to work_orders).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.invoice_lines (
    id                  UUID          NOT NULL DEFAULT uuidv7(),
    invoice_id          UUID          NOT NULL,
    work_order_id       UUID,
    work_order_part_id  UUID,
    description         VARCHAR(500)  NOT NULL,
    quantity            NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_price          NUMERIC(10,2) NOT NULL,
    line_total          NUMERIC(10,2) NOT NULL,
    notes               TEXT,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,

    CONSTRAINT pk_workshop_invoice_lines                        PRIMARY KEY (id),
    CONSTRAINT fk_workshop_invoice_lines_invoice_id             FOREIGN KEY (invoice_id)         REFERENCES workshop.invoices(id),
    CONSTRAINT fk_workshop_invoice_lines_work_order_id          FOREIGN KEY (work_order_id)      REFERENCES workshop.work_orders(id),
    CONSTRAINT fk_workshop_invoice_lines_work_order_part_id     FOREIGN KEY (work_order_part_id) REFERENCES workshop.work_order_parts(id),
    CONSTRAINT fk_workshop_invoice_lines_created_by             FOREIGN KEY (created_by)         REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_invoice_lines_updated_by             FOREIGN KEY (updated_by)         REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_invoice_lines_quantity                CHECK (quantity > 0),
    CONSTRAINT ck_workshop_invoice_lines_unit_price              CHECK (unit_price >= 0)
);

COMMENT ON TABLE  workshop.invoice_lines                        IS 'Individual line items on an invoice. Parts lines link to work_order_parts; labor lines link to work_orders. Walk-in invoice lines have no work order links.';
COMMENT ON COLUMN workshop.invoice_lines.id                     IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.invoice_lines.invoice_id             IS 'The invoice this line belongs to.';
COMMENT ON COLUMN workshop.invoice_lines.work_order_id          IS 'Optional link to the work order this line relates to. NULL for walk-in invoice lines.';
COMMENT ON COLUMN workshop.invoice_lines.work_order_part_id     IS 'Optional link to the specific work_order_part. NULL for labor lines and walk-in lines.';
COMMENT ON COLUMN workshop.invoice_lines.description            IS 'Line item description shown on the invoice (e.g. "Oil filter OEM 123-456" or "Labor: brake pad replacement").';
COMMENT ON COLUMN workshop.invoice_lines.quantity               IS 'Number of units. Supports decimals for labor hours (e.g. 1.5 hours).';
COMMENT ON COLUMN workshop.invoice_lines.unit_price             IS 'Price per unit.';
COMMENT ON COLUMN workshop.invoice_lines.line_total             IS 'Total for this line (quantity * unit_price). Stored for immutability — once invoiced, the amount is fixed.';
COMMENT ON COLUMN workshop.invoice_lines.notes                  IS 'Optional notes for this line item.';
COMMENT ON COLUMN workshop.invoice_lines.created_by             IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.invoice_lines.updated_at             IS 'NULL on creation. Set on any update.';

CREATE INDEX IF NOT EXISTS ix_workshop_invoice_lines_invoice_id
    ON workshop.invoice_lines (invoice_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoice_lines_work_order_id
    ON workshop.invoice_lines (work_order_id);

COMMIT;
