-- Migration: 20260317_0936_T_invoice_statuses_DML.sql
-- Description: Seed data for codebook.invoice_statuses.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.invoice_statuses (code, label, sort_order) VALUES
    ('draft',     '{"en": "Draft",     "sr": "Nacrt"}',    1),
    ('issued',    '{"en": "Issued",    "sr": "Izdata"}',   2),
    ('paid',      '{"en": "Paid",      "sr": "Plaćena"}',  3),
    ('cancelled', '{"en": "Cancelled", "sr": "Otkazana"}', 4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
