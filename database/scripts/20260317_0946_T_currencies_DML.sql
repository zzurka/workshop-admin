-- Migration: 20260317_0946_T_currencies_DML.sql
-- Description: Seed data for codebook.currencies.
--              Only currencies the system currently transacts in are seeded.
--              Add more rows here as needed — do not seed the full ISO 4217 list.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.currencies (code, label, sort_order) VALUES
    ('EUR', '{"en": "Euro",          "sr": "Evro"}',          1),
    ('USD', '{"en": "US Dollar",     "sr": "Američki dolar"}', 2),
    ('RSD', '{"en": "Serbian Dinar", "sr": "Srpski dinar"}',  3)
ON CONFLICT (code) DO NOTHING;

COMMIT;
