-- Migration: 20260330_1320_T_invoice_lines_DDL.sql
-- Description: Create the workshop.invoice_lines table. Individual line items
--              on an invoice — parts (linked back to work_order_parts) and
--              labor charges (linked back to work_orders). Prices are NET
--              (excluding VAT); VAT is computed and rounded per line.
--              Carries a denormalized tenant_id (exception to the child-table
--              convention) because it references multiple tenant-scoped tables
--              (invoices, work_orders, work_order_parts) — composite FKs need
--              it to guarantee all references stay within one tenant.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.invoice_lines (
    id                  UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id           UUID          NOT NULL,
    invoice_id          UUID          NOT NULL,
    work_order_id       UUID,
    work_order_part_id  UUID,
    work_order_labor_id UUID,
    description         VARCHAR(500)  NOT NULL,
    quantity            NUMERIC(10,2) NOT NULL DEFAULT 1,
    unit_price          NUMERIC(10,2) NOT NULL,
    discount_percent    NUMERIC(5,2)  NOT NULL DEFAULT 0,
    line_total          NUMERIC(10,2) NOT NULL,
    tax_rate_id         SMALLINT      NOT NULL,
    tax_rate            NUMERIC(5,2)  NOT NULL,
    tax_amount          NUMERIC(12,2) NOT NULL DEFAULT 0,
    notes               TEXT,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,

    CONSTRAINT pk_workshop_invoice_lines                        PRIMARY KEY (id),
    CONSTRAINT fk_workshop_invoice_lines_tenant_id              FOREIGN KEY (tenant_id)                     REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_invoice_lines_invoice_id             FOREIGN KEY (tenant_id, invoice_id)         REFERENCES workshop.invoices(tenant_id, id),
    CONSTRAINT fk_workshop_invoice_lines_work_order_id          FOREIGN KEY (tenant_id, work_order_id)      REFERENCES workshop.work_orders(tenant_id, id),
    CONSTRAINT fk_workshop_invoice_lines_work_order_part_id     FOREIGN KEY (tenant_id, work_order_part_id) REFERENCES workshop.work_order_parts(tenant_id, id),
    CONSTRAINT fk_workshop_invoice_lines_work_order_labor_id    FOREIGN KEY (tenant_id, work_order_labor_id) REFERENCES workshop.work_order_labor(tenant_id, id),
    CONSTRAINT fk_workshop_invoice_lines_tax_rate_id            FOREIGN KEY (tax_rate_id)        REFERENCES codebook.tax_rates(id),
    CONSTRAINT fk_workshop_invoice_lines_created_by             FOREIGN KEY (created_by)         REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_invoice_lines_updated_by             FOREIGN KEY (updated_by)         REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_invoice_lines_quantity                CHECK (quantity > 0),
    CONSTRAINT ck_workshop_invoice_lines_unit_price              CHECK (unit_price >= 0),
    CONSTRAINT ck_workshop_invoice_lines_discount_percent        CHECK (discount_percent >= 0 AND discount_percent <= 100),
    CONSTRAINT ck_workshop_invoice_lines_tax_rate                CHECK (tax_rate >= 0 AND tax_rate <= 100),
    CONSTRAINT ck_workshop_invoice_lines_tax_amount              CHECK (tax_amount >= 0)
);

COMMENT ON TABLE  workshop.invoice_lines                        IS 'Individual line items on an invoice. Parts lines link to work_order_parts; labor lines link to work_orders. Walk-in invoice lines have no work order links. All prices are NET (excluding VAT).';
COMMENT ON COLUMN workshop.invoice_lines.id                     IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.invoice_lines.tenant_id              IS 'Denormalized tenant scope. Must match the tenant of the invoice, work order, and work order part — enforced by the composite FKs.';
COMMENT ON COLUMN workshop.invoice_lines.invoice_id             IS 'The invoice this line belongs to. Composite FK (tenant_id, invoice_id) guarantees the invoice belongs to the same tenant.';
COMMENT ON COLUMN workshop.invoice_lines.work_order_id          IS 'Optional link to the work order this line relates to. NULL for walk-in invoice lines; composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.invoice_lines.work_order_part_id     IS 'Optional link to the specific work_order_part. NULL for labor lines and walk-in lines; composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.invoice_lines.work_order_labor_id    IS 'Optional link to the work_order_labor entry this labor line bills (quantity = hours, unit_price = hourly_rate snapshot). NULL for parts lines and walk-in lines; composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.invoice_lines.description            IS 'Line item description shown on the invoice (e.g. "Oil filter OEM 123-456" or "Labor: brake pad replacement").';
COMMENT ON COLUMN workshop.invoice_lines.quantity               IS 'Number of units. Supports decimals for labor hours (e.g. 1.5 hours).';
COMMENT ON COLUMN workshop.invoice_lines.unit_price             IS 'NET price per unit (excluding VAT). The UI may accept gross input and convert.';
COMMENT ON COLUMN workshop.invoice_lines.discount_percent       IS 'Percentage discount for this line item (0–100). Default 0 (no discount).';
COMMENT ON COLUMN workshop.invoice_lines.line_total             IS 'NET total for this line (quantity * unit_price * (1 - discount_percent / 100)), excluding VAT. Stored for immutability — once invoiced, the amount is fixed. Gross per line is derivable as line_total + tax_amount and is not stored.';
COMMENT ON COLUMN workshop.invoice_lines.tax_rate_id            IS 'FK to codebook.tax_rates. The VAT rate applied to this line; its numeric value is snapshotted in tax_rate.';
COMMENT ON COLUMN workshop.invoice_lines.tax_rate               IS 'VAT percentage snapshot taken when the line is added — later changes in codebook.tax_rates never affect existing lines. For tenants outside the VAT system this is always 0 (rate ''exempt'').';
COMMENT ON COLUMN workshop.invoice_lines.tax_amount             IS 'VAT for this line = ROUND(line_total * tax_rate / 100, 2). Rounded per line to 2 decimals; invoice header totals are sums of line values.';
COMMENT ON COLUMN workshop.invoice_lines.notes                  IS 'Optional notes for this line item.';
COMMENT ON COLUMN workshop.invoice_lines.created_by             IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.invoice_lines.updated_at             IS 'NULL on creation. Set on any update.';

CREATE INDEX IF NOT EXISTS ix_workshop_invoice_lines_tenant_id
    ON workshop.invoice_lines (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoice_lines_invoice_id
    ON workshop.invoice_lines (invoice_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoice_lines_work_order_id
    ON workshop.invoice_lines (work_order_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoice_lines_work_order_labor_id
    ON workshop.invoice_lines (work_order_labor_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoice_lines_tax_rate_id
    ON workshop.invoice_lines (tax_rate_id);

COMMIT;
