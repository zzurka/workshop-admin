-- Migration: 20260330_1111_T_appointment_statuses_DML.sql
-- Description: Seed data for codebook.appointment_statuses.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.appointment_statuses (code, label, sort_order) VALUES
    ('scheduled', '{"en": "Scheduled", "sr": "Zakazano"}',  1),
    ('confirmed', '{"en": "Confirmed", "sr": "Potvrđeno"}', 2),
    ('completed', '{"en": "Completed", "sr": "Završeno"}',  3),
    ('cancelled', '{"en": "Cancelled", "sr": "Otkazano"}',  4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
