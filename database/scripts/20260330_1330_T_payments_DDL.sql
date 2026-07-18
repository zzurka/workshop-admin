-- Migration: 20260330_1330_T_payments_DDL.sql
-- Description: Create the workshop.payments table. Payments received against
--              invoices, including partial payments.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.payments (
    id                UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id         UUID          NOT NULL,
    invoice_id        UUID          NOT NULL,
    payment_method_id SMALLINT      NOT NULL,
    amount            NUMERIC(12,2) NOT NULL,
    paid_at           TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    reference         VARCHAR(100),
    notes             TEXT,
    created_at        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by        UUID,
    updated_at        TIMESTAMPTZ,
    updated_by        UUID,
    is_deleted        BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_payments                   PRIMARY KEY (id),
    CONSTRAINT fk_workshop_payments_tenant_id         FOREIGN KEY (tenant_id)         REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_payments_invoice_id        FOREIGN KEY (tenant_id, invoice_id) REFERENCES workshop.invoices(tenant_id, id),
    CONSTRAINT fk_workshop_payments_payment_method_id FOREIGN KEY (payment_method_id) REFERENCES codebook.payment_methods(id),
    CONSTRAINT fk_workshop_payments_created_by        FOREIGN KEY (created_by)        REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_payments_updated_by        FOREIGN KEY (updated_by)        REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_payments_amount            CHECK (amount > 0)
);

COMMENT ON TABLE  workshop.payments                   IS 'Payments received against invoices. Supports partial payments. Positive amounts only — an incorrect entry is soft-deleted and re-entered. Refunds are handled via credit-note documents (out of scope, comes with complaints). Overpayment is prevented at the application level.';
COMMENT ON COLUMN workshop.payments.id                IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.payments.tenant_id         IS 'The tenant (workshop) this payment belongs to.';
COMMENT ON COLUMN workshop.payments.invoice_id        IS 'The invoice this payment settles (fully or partially). Composite FK (tenant_id, invoice_id) guarantees the invoice belongs to the same tenant.';
COMMENT ON COLUMN workshop.payments.payment_method_id IS 'FK to codebook.payment_methods (cash, card, bank_transfer, other).';
COMMENT ON COLUMN workshop.payments.amount            IS 'Amount paid, in the invoice currency. Always positive.';
COMMENT ON COLUMN workshop.payments.paid_at           IS 'When the payment was made (bank statement date / cash receipt time), not when it was recorded.';
COMMENT ON COLUMN workshop.payments.reference         IS 'Payment reference — poziv na broj, bank statement reference, or card slip number.';
COMMENT ON COLUMN workshop.payments.notes             IS 'Optional notes about this payment.';
COMMENT ON COLUMN workshop.payments.created_by        IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.payments.updated_at        IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.payments.is_deleted        IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp. Used to void incorrectly entered payments.';

CREATE INDEX IF NOT EXISTS ix_workshop_payments_tenant_id
    ON workshop.payments (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_payments_invoice_id
    ON workshop.payments (invoice_id);

CREATE INDEX IF NOT EXISTS ix_workshop_payments_payment_method_id
    ON workshop.payments (payment_method_id);

CREATE INDEX IF NOT EXISTS ix_workshop_payments_paid_at
    ON workshop.payments (paid_at);

CREATE INDEX IF NOT EXISTS ix_workshop_payments_is_deleted
    ON workshop.payments (is_deleted);

COMMIT;
