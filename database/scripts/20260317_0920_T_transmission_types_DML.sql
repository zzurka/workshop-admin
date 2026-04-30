-- Migration: 20260317_0920_T_transmission_types_DML.sql
-- Description: Seed data for codebook.transmission_types.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.transmission_types (code, label, sort_order) VALUES
    ('automatic', '{"en": "Automatic", "sr": "Automatski"}', 1),
    ('manual',    '{"en": "Manual",    "sr": "Ručni"}',      2),
    ('cvt',       '{"en": "CVT",       "sr": "CVT"}',        3),
    ('dct',       '{"en": "DCT",       "sr": "DCT"}',        4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
