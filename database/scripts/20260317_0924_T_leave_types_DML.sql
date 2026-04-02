-- Migration: 20260330_0911_T_leave_types_DML.sql
-- Description: Seed data for codebook.leave_types.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.leave_types (code, label, sort_order) VALUES
    ('vacation',  '{"en": "Vacation",        "sr": "Godišnji odmor"}',     1),
    ('sick',      '{"en": "Sick Leave",      "sr": "Bolovanje"}',          2),
    ('personal',  '{"en": "Personal Leave",  "sr": "Lični razlozi"}',      3),
    ('maternity', '{"en": "Maternity Leave", "sr": "Porodiljsko"}',        4),
    ('paternity', '{"en": "Paternity Leave", "sr": "Očinsko odsustvo"}',   5),
    ('unpaid',    '{"en": "Unpaid Leave",    "sr": "Neplaćeno odsustvo"}', 6),
	('slava',     '{"en": "Slava",    		 "sr": "Slava"}', 			   7)
ON CONFLICT (code) DO NOTHING;

COMMIT;
