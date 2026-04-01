-- Migration: 20260401_1001_T_expense_categories_DML.sql
-- Description: Seed data for codebook.expense_categories.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-01
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.expense_categories (code, label, sort_order) VALUES
    ('rent',        '{"en": "Rent",        "sr": "Zakup"}',              		1),
    ('utilities',   '{"en": "Utilities",   "sr": "Komunalije"}',         		2),
    ('tools',       '{"en": "Tools",       "sr": "Alat"}',               		3),
    ('parts',       '{"en": "Parts",       "sr": "Delovi"}',             		4),
    ('insurance',   '{"en": "Insurance",   "sr": "Osiguranje"}',         		5),
    ('marketing',   '{"en": "Marketing",   "sr": "Marketing"}',          		6),
    ('maintenance', '{"en": "Maintenance", "sr": "Održavanje"}',         		7),
    ('office',      '{"en": "Office",      "sr": "Kancelarijski materijal"}', 	8),
    ('other',       '{"en": "Other",       "sr": "Ostalo"}',             		9)
ON CONFLICT (code) DO NOTHING;

COMMIT;
