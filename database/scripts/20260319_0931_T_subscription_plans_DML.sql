-- Migration: 20260319_0931_T_subscription_plans_DML.sql
-- Description: Seed data for codebook.subscription_plans.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.subscription_plans (code, label, sort_order) VALUES
    ('free',  '{"en": "Free",  "hr": "Besplatno"}', 1),
    ('trial', '{"en": "Trial", "hr": "Probno"}',    2),
    ('paid',  '{"en": "Paid",  "hr": "Plaćeno"}',   3)
ON CONFLICT (code) DO NOTHING;

COMMIT;
