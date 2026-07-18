-- Migration: 20260317_0952_T_complaint_statuses_DML.sql
-- Description: Seed data for codebook.complaint_statuses.
--              Transitions (application rule): submitted -> in_review ->
--              accepted -> resolved, or rejected (terminal, with reasoning in
--              resolution); cancelled from submitted/in_review.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.complaint_statuses (code, label, sort_order) VALUES
    ('submitted', '{"en": "Submitted",  "sr": "Podneta"}',        1),
    ('in_review', '{"en": "In review",  "sr": "U razmatranju"}',  2),
    ('accepted',  '{"en": "Accepted",   "sr": "Prihvaćena"}',     3),
    ('rejected',  '{"en": "Rejected",   "sr": "Odbijena"}',       4),
    ('resolved',  '{"en": "Resolved",   "sr": "Rešena"}',         5),
    ('cancelled', '{"en": "Cancelled",  "sr": "Povučena"}',       6)
ON CONFLICT (code) DO NOTHING;

COMMIT;
