-- Migration: 20260317_0930_T_appointment_statuses_DML.sql
-- Description: Seed data for codebook.appointment_statuses.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.appointment_statuses (code, label, sort_order) VALUES
    ('scheduled',   '{"en": "Scheduled",   "sr": "Zakazano"}',        1),
    ('confirmed',   '{"en": "Confirmed",   "sr": "Potvrđeno"}',       2),
    ('in_progress', '{"en": "In Progress", "sr": "U radu"}',          3),
    ('completed',   '{"en": "Completed",   "sr": "Završeno"}',        4),
    ('no_show',     '{"en": "No Show",     "sr": "Nedolazak"}',       5),
    ('cancelled',   '{"en": "Cancelled",   "sr": "Otkazano"}',        6)
ON CONFLICT (code) DO NOTHING;

COMMIT;
