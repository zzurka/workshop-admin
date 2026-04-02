-- Migration: 20260319_0911_T_fuel_types_DML.sql
-- Description: Seed data for codebook.fuel_types.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.fuel_types (code, label, sort_order) VALUES
    ('gasoline', '{"en": "Gasoline", "sr": "Benzin"}',     1),
    ('diesel',   '{"en": "Diesel",   "sr": "Dizel"}',      2),
    ('electric', '{"en": "Electric", "sr": "Električni"}', 3),
    ('hybrid',   '{"en": "Hybrid",   "sr": "Hibrid"}',     4),
    ('lpg',      '{"en": "LPG",      "sr": "LPG"}',        5),
    ('cng',      '{"en": "CNG",      "sr": "CNG"}',        6)
ON CONFLICT (code) DO NOTHING;

COMMIT;
