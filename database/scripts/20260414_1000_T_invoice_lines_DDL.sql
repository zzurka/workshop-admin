-- Migration: 20260414_1000_T_invoice_lines_DDL.sql
-- Description: Add discount_percent column to workshop.invoice_lines.
--              Percentage discount per line item (0–100). Default 0 (no discount).
-- Author: WorkshopAdmin Team
-- Date: 2026-04-14
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

ALTER TABLE workshop.invoice_lines
    ADD COLUMN IF NOT EXISTS discount_percent NUMERIC(5,2) NOT NULL DEFAULT 0;

ALTER TABLE workshop.invoice_lines
    DROP CONSTRAINT IF EXISTS ck_workshop_invoice_lines_discount_percent;

ALTER TABLE workshop.invoice_lines
    ADD CONSTRAINT ck_workshop_invoice_lines_discount_percent
        CHECK (discount_percent >= 0 AND discount_percent <= 100);

COMMENT ON COLUMN workshop.invoice_lines.discount_percent IS 'Percentage discount for this line item (0–100). line_total = quantity * unit_price * (1 - discount_percent / 100).';

COMMIT;
