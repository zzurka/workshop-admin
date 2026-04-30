-- Migration: 20260317_0934_T_part_statuses_DML.sql
-- Description: Seed data for codebook.part_statuses.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.part_statuses (code, label, sort_order) VALUES
    ('in_stock',  '{"en": "In Stock",  "sr": "Na stanju"}',  1),
    ('ordered',   '{"en": "Ordered",   "sr": "Naručeno"}',   2),
    ('received',  '{"en": "Received",  "sr": "Primljeno"}',  3)
ON CONFLICT (code) DO NOTHING;

COMMIT;
