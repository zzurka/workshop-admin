-- Migration: 20260317_1001_T_subscription_plans_DML.sql
-- Description: Seed data for codebook.subscription_plans.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.subscription_plans (code, label, sort_order) VALUES
    ('free',  '{"en": "Free",  "sr": "Besplatni"}', 1),
    ('trial', '{"en": "Trial", "sr": "Probni"}',    2),
    ('paid',  '{"en": "Paid",  "sr": "Plaćen"}',    3)
ON CONFLICT (code) DO NOTHING;

COMMIT;
