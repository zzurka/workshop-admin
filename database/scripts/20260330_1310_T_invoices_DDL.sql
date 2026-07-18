-- Migration: 20260330_1310_T_invoices_DDL.sql
-- Description: Create the workshop.invoices table. Represents a bill to the
--              customer for work done. Can be linked to an appointment (full
--              service visit) or stand-alone (walk-in quick jobs like bulb change).
--              Carries the legal invoice elements for Serbia: sequential number
--              per (tenant, year), VAT totals, snapshotted currency and buyer data.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.invoices (
    id                  UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id           UUID          NOT NULL,
    customer_id         UUID          NOT NULL,
    vehicle_id          UUID          NOT NULL,
    appointment_id      UUID,
    invoice_status_id   SMALLINT      NOT NULL,
    invoice_number      INTEGER,
    invoice_year        SMALLINT,
    currency_id         SMALLINT      NOT NULL,
    subtotal            NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_amount          NUMERIC(12,2) NOT NULL DEFAULT 0,
    total               NUMERIC(12,2) NOT NULL DEFAULT 0,
    issued_at           TIMESTAMPTZ,
    service_date        DATE,
    due_date            DATE,
    paid_at             TIMESTAMPTZ,
    billed_to_name      VARCHAR(255),
    billed_to_address   VARCHAR(500),
    billed_to_tax_id    VARCHAR(20),
    vat_note            TEXT,
    notes               TEXT,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_invoices                       PRIMARY KEY (id),
    CONSTRAINT uq_workshop_invoices_tenant_id_id          UNIQUE (tenant_id, id),
    CONSTRAINT uq_workshop_invoices_tenant_year_number    UNIQUE (tenant_id, invoice_year, invoice_number),
    CONSTRAINT fk_workshop_invoices_tenant_id             FOREIGN KEY (tenant_id)         REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_invoices_customer_id           FOREIGN KEY (tenant_id, customer_id)    REFERENCES customer.customers(tenant_id, id),
    CONSTRAINT fk_workshop_invoices_vehicle_id            FOREIGN KEY (tenant_id, vehicle_id)     REFERENCES customer.vehicles(tenant_id, id),
    CONSTRAINT fk_workshop_invoices_appointment_id        FOREIGN KEY (tenant_id, appointment_id) REFERENCES workshop.appointments(tenant_id, id),
    CONSTRAINT fk_workshop_invoices_invoice_status_id     FOREIGN KEY (invoice_status_id) REFERENCES codebook.invoice_statuses(id),
    CONSTRAINT fk_workshop_invoices_currency_id           FOREIGN KEY (currency_id)       REFERENCES codebook.currencies(id),
    CONSTRAINT fk_workshop_invoices_created_by            FOREIGN KEY (created_by)        REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_invoices_updated_by            FOREIGN KEY (updated_by)        REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_invoices_number_year           CHECK ((invoice_number IS NULL) = (invoice_year IS NULL)),
    CONSTRAINT ck_workshop_invoices_totals                CHECK (subtotal >= 0 AND tax_amount >= 0 AND total >= 0)
);

COMMENT ON TABLE  workshop.invoices                       IS 'Customer invoices. Linked to an appointment for full service visits, or stand-alone for walk-in quick jobs. Application rules: status issued and later requires invoice_number/invoice_year assigned, totals recomputed from lines and frozen, and billed_to_* populated; paid_at is set when the sum of payments reaches total.';
COMMENT ON COLUMN workshop.invoices.id                    IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.invoices.tenant_id             IS 'The tenant (workshop) this invoice belongs to.';
COMMENT ON COLUMN workshop.invoices.customer_id           IS 'The customer being billed. Composite FK (tenant_id, customer_id) guarantees the customer belongs to the same tenant.';
COMMENT ON COLUMN workshop.invoices.vehicle_id            IS 'The vehicle that was serviced. Composite FK (tenant_id, vehicle_id) guarantees the vehicle belongs to the same tenant.';
COMMENT ON COLUMN workshop.invoices.appointment_id        IS 'Optional link to appointment. NULL for walk-in quick jobs (e.g. bulb change); composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.invoices.invoice_status_id     IS 'FK to codebook.invoice_statuses (draft, issued, partially_paid, paid, cancelled). Transitions (application rule): draft -> issued -> partially_paid <-> paid; cancelled from draft/issued. ''overdue'' is derived from due_date + status, not stored.';
COMMENT ON COLUMN workshop.invoices.invoice_number        IS 'Sequential invoice number, gapless per (tenant, invoice_year) — assigned from workshop.invoice_counters at issue time. NULL while draft, so cancelled drafts leave no numbering gaps; the UNIQUE constraint treats NULLs as distinct, allowing many drafts. Once assigned, never changes (a cancelled issued invoice keeps its number). Displayed as {number}/{year}, e.g. 125/2026.';
COMMENT ON COLUMN workshop.invoices.invoice_year          IS 'Numbering year for invoice_number. Always set together with invoice_number (ck_workshop_invoices_number_year).';
COMMENT ON COLUMN workshop.invoices.currency_id           IS 'FK to codebook.currencies. Snapshot of the tenant''s default currency at invoice creation — a later change of the tenant default must not affect this invoice.';
COMMENT ON COLUMN workshop.invoices.subtotal              IS 'Net amount (excluding VAT) — the sum of line_total across invoice lines. Frozen at issue time.';
COMMENT ON COLUMN workshop.invoices.tax_amount            IS 'Total VAT — the sum of tax_amount across invoice lines (VAT is rounded per line). Frozen at issue time.';
COMMENT ON COLUMN workshop.invoices.total                 IS 'Gross amount payable = subtotal + tax_amount. Frozen at issue time.';
COMMENT ON COLUMN workshop.invoices.issued_at             IS 'Timestamp when the invoice was issued to the customer. NULL while draft.';
COMMENT ON COLUMN workshop.invoices.service_date          IS 'Date of the supply of goods/services (datum prometa) — legally required on Serbian invoices, distinct from the issue date.';
COMMENT ON COLUMN workshop.invoices.due_date              IS 'Payment due date (valuta plaćanja).';
COMMENT ON COLUMN workshop.invoices.paid_at               IS 'Timestamp when the invoice became fully paid — when the sum of payments reached total. NULL until then.';
COMMENT ON COLUMN workshop.invoices.billed_to_name        IS 'Snapshot of the customer name at issue time. The issued invoice is an immutable document — later changes to the customer record must not alter it.';
COMMENT ON COLUMN workshop.invoices.billed_to_address     IS 'Snapshot of the customer address at issue time.';
COMMENT ON COLUMN workshop.invoices.billed_to_tax_id      IS 'Snapshot of the customer PIB at issue time (B2B invoices). Fillable once customers support legal entities (model review item 3.3).';
COMMENT ON COLUMN workshop.invoices.vat_note              IS 'VAT exemption note printed on the invoice, e.g. reference to čl. 33 ZPDV for tenants outside the VAT system. NULL when VAT is charged.';
COMMENT ON COLUMN workshop.invoices.notes                 IS 'Internal notes about this invoice.';
COMMENT ON COLUMN workshop.invoices.created_by            IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.invoices.updated_at            IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.invoices.is_deleted            IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_tenant_id
    ON workshop.invoices (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_customer_id
    ON workshop.invoices (customer_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_vehicle_id
    ON workshop.invoices (vehicle_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_appointment_id
    ON workshop.invoices (appointment_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_invoice_status_id
    ON workshop.invoices (invoice_status_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_currency_id
    ON workshop.invoices (currency_id);

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_is_deleted
    ON workshop.invoices (is_deleted);

COMMIT;
