-- Migration: 20260317_0932_T_work_order_statuses_DML.sql
-- Description: Seed data for codebook.work_order_statuses.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.work_order_statuses (code, label, sort_order) VALUES
    ('awaiting_approval', '{"en": "Awaiting approval", "sr": "Čeka odobrenje"}', 1),
    ('pending_parts',     '{"en": "Pending parts",     "sr": "Čekanje delova"}', 2),
    ('ready',             '{"en": "Ready",             "sr": "Spremno"}',        3),
    ('in_progress',       '{"en": "In progress",       "sr": "U toku"}',         4),
    ('completed',         '{"en": "Completed",         "sr": "Završeno"}',       5),
    ('cancelled',         '{"en": "Cancelled",         "sr": "Otkazano"}',       6)
ON CONFLICT (code) DO NOTHING;

COMMIT;
