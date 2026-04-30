-- Migration: 20260317_0922_T_employment_types_DML.sql
-- Description: Seed data for codebook.employment_types.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.employment_types (code, label, sort_order) VALUES
    ('salaried', '{"en": "Salaried", "sr": "Stalno zaposleni"}', 1),
    ('hourly',   '{"en": "Hourly",   "sr": "Po satu"}',         2)
ON CONFLICT (code) DO NOTHING;

COMMIT;
