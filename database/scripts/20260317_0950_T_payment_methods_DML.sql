-- Migration: 20260317_0950_T_payment_methods_DML.sql
-- Description: Seed data for codebook.payment_methods.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.payment_methods (code, label, sort_order) VALUES
    ('cash',          '{"en": "Cash",          "sr": "Gotovina"}', 1),
    ('card',          '{"en": "Card",          "sr": "Kartica"}',  2),
    ('bank_transfer', '{"en": "Bank transfer", "sr": "Virman"}',   3),
    ('other',         '{"en": "Other",         "sr": "Ostalo"}',   4)
ON CONFLICT (code) DO NOTHING;

COMMIT;
