-- Migration: 20260319_0921_T_transmissions_DML.sql
-- Description: Seed data for codebook.transmissions.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.transmissions (code, label, sort_order) VALUES
    ('automatic', '{"en": "Automatic", "hr": "Automatski"}', 1),
    ('manual',    '{"en": "Manual",    "hr": "Ručni"}',      2),
    ('cvt',       '{"en": "CVT",       "hr": "CVT"}',        3),
    ('dct',       '{"en": "DCT",       "hr": "DCT"}',        4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
