-- Migration: 20260317_0944_T_billing_periods_DML.sql
-- Description: Seed data for codebook.billing_periods.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.billing_periods (code, label, sort_order) VALUES
    ('monthly',   '{"en": "Monthly",   "sr": "Mesečno"}',     1),
    ('quarterly', '{"en": "Quarterly", "sr": "Tromesečno"}',  2),
    ('yearly',    '{"en": "Yearly",    "sr": "Godišnje"}',    3),
    ('one_time',  '{"en": "One-time",  "sr": "Jednokratno"}', 4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
