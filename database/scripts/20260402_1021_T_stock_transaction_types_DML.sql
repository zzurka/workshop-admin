-- Migration: 20260402_1021_T_stock_transaction_types_DML.sql
-- Description: Seed data for codebook.stock_transaction_types.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.stock_transaction_types (code, label, sort_order) VALUES
    ('receipt',        '{"en": "Receipt",        "sr": "Prijem"}',        1),
    ('issue',          '{"en": "Issue",          "sr": "Izdavanje"}',     2),
    ('return',         '{"en": "Return",         "sr": "Povrat"}',        3),
    ('adjustment_in',  '{"en": "Adjustment In",  "sr": "Korekcija +"}',  4),
    ('adjustment_out', '{"en": "Adjustment Out", "sr": "Korekcija -"}',  5)
ON CONFLICT (code) DO NOTHING;

COMMIT;
