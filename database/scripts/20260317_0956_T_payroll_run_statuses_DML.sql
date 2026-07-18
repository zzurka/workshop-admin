-- Migration: 20260317_0956_T_payroll_run_statuses_DML.sql
-- Description: Seed data for codebook.payroll_run_statuses.
--              Transitions (application rule): draft -> approved -> paid;
--              cancelled from draft. Payslips are frozen once the run is
--              approved.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.payroll_run_statuses (code, label, sort_order) VALUES
    ('draft',     '{"en": "Draft",     "sr": "Nacrt"}',    1),
    ('approved',  '{"en": "Approved",  "sr": "Odobren"}',  2),
    ('paid',      '{"en": "Paid",      "sr": "Isplaćen"}', 3),
    ('cancelled', '{"en": "Cancelled", "sr": "Otkazan"}',  4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
