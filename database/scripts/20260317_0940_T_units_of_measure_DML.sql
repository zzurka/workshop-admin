-- Migration: 20260402_1011_T_units_of_measure_DML.sql
-- Description: Seed data for codebook.units_of_measure.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.units_of_measure (code, label, sort_order) VALUES
    ('piece', '{"en": "Piece",      "sr": "Komad"}',    1),
    ('liter', '{"en": "Liter",      "sr": "Litar"}',    2),
    ('kg',    '{"en": "Kilogram",   "sr": "Kilogram"}', 3),
    ('meter', '{"en": "Meter",      "sr": "Metar"}',    4),
    ('set',   '{"en": "Set",        "sr": "Komplet"}',  5)
ON CONFLICT (code) DO NOTHING;

COMMIT;
