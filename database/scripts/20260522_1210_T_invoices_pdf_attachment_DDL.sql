-- Migration: 20260522_1210_T_invoices_pdf_attachment_DDL.sql
-- Description: Add pdf_attachment_id to workshop.invoices — the canonical PDF
--              snapshot of the issued invoice, stored via document.attachments.
--              Lives in a separate script because workshop.invoices is created
--              before document.attachments exists.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

ALTER TABLE workshop.invoices
    ADD COLUMN IF NOT EXISTS pdf_attachment_id UUID;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_workshop_invoices_pdf_attachment_id'
    ) THEN
        ALTER TABLE workshop.invoices
            ADD CONSTRAINT fk_workshop_invoices_pdf_attachment_id
            FOREIGN KEY (pdf_attachment_id) REFERENCES document.attachments(id);
    END IF;
END $$;

COMMENT ON COLUMN workshop.invoices.pdf_attachment_id IS 'The canonical PDF of the issued invoice (document.attachments). Rendered and stored at issue time — the issued invoice is an immutable document and must not be re-rendered from a later template. NULL while draft. Other files attached to an invoice go through document.attachments polymorphically (entity_type = ''invoice''); this column marks which one is the official issued document.';

CREATE INDEX IF NOT EXISTS ix_workshop_invoices_pdf_attachment_id
    ON workshop.invoices (pdf_attachment_id);

COMMIT;
