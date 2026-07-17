-- Migration: 20260317_0948_T_tax_rates_DML.sql
-- Description: Seed data for codebook.tax_rates.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.tax_rates (code, label, rate, sort_order) VALUES
    ('standard', '{"en": "VAT 20%",    "sr": "PDV 20%"}',          20.00, 1),
    ('reduced',  '{"en": "VAT 10%",    "sr": "PDV 10%"}',          10.00, 2),
    ('exempt',   '{"en": "VAT exempt", "sr": "Oslobođeno PDV-a"}',  0.00, 3)
ON CONFLICT (code) DO NOTHING;

COMMIT;
