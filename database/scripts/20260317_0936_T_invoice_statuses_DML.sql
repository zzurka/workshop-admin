-- Migration: 20260317_0936_T_invoice_statuses_DML.sql
-- Description: Seed data for codebook.invoice_statuses.
--              Transitions (application rule): draft -> issued ->
--              partially_paid <-> paid; cancelled from draft/issued.
--              'overdue' is intentionally not a status — it is derived
--              from due_date + status.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.invoice_statuses (code, label, sort_order) VALUES
    ('draft',          '{"en": "Draft",          "sr": "Nacrt"}',             1),
    ('issued',         '{"en": "Issued",         "sr": "Izdata"}',            2),
    ('partially_paid', '{"en": "Partially paid", "sr": "Delimično plaćena"}', 3),
    ('paid',           '{"en": "Paid",           "sr": "Plaćena"}',           4),
    ('cancelled',      '{"en": "Cancelled",      "sr": "Otkazana"}',          5)
ON CONFLICT (code) DO NOTHING;

COMMIT;
