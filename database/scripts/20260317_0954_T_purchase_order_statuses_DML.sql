-- Migration: 20260317_0954_T_purchase_order_statuses_DML.sql
-- Description: Seed data for codebook.purchase_order_statuses.
--              Transitions (application rule): draft -> ordered ->
--              partially_received -> received; cancelled from draft/ordered.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.purchase_order_statuses (code, label, sort_order) VALUES
    ('draft',              '{"en": "Draft",              "sr": "Nacrt"}',               1),
    ('ordered',            '{"en": "Ordered",            "sr": "Poručeno"}',            2),
    ('partially_received', '{"en": "Partially received", "sr": "Delimično primljeno"}', 3),
    ('received',           '{"en": "Received",           "sr": "Primljeno"}',           4),
    ('cancelled',          '{"en": "Cancelled",          "sr": "Otkazano"}',            5)
ON CONFLICT (code) DO NOTHING;

COMMIT;
