-- Migration: 20260330_0921_T_leave_statuses_DML.sql
-- Description: Seed data for codebook.leave_statuses.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.leave_statuses (code, label, sort_order) VALUES
    ('pending',   '{"en": "Pending",   "sr": "Na čekanju"}', 1),
    ('approved',  '{"en": "Approved",  "sr": "Odobreno"}',   2),
    ('rejected',  '{"en": "Rejected",  "sr": "Odbijeno"}',   3),
    ('cancelled', '{"en": "Cancelled", "sr": "Otkazano"}',   4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
