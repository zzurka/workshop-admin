-- Migration: 20260330_1121_T_work_order_statuses_DML.sql
-- Description: Seed data for codebook.work_order_statuses.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.work_order_statuses (code, label, sort_order) VALUES
    ('pending_parts', '{"en": "Pending Parts", "sr": "Čekanje delova"}', 1),
    ('ready',         '{"en": "Ready",         "sr": "Spremno"}',        2),
    ('in_progress',   '{"en": "In Progress",   "sr": "U toku"}',         3),
    ('completed',     '{"en": "Completed",     "sr": "Završeno"}',       4),
    ('cancelled',     '{"en": "Cancelled",     "sr": "Otkazano"}',       5)
ON CONFLICT (code) DO NOTHING;

COMMIT;
