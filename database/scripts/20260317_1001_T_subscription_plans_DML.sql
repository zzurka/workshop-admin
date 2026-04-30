-- Migration: 20260317_1001_T_subscription_plans_DML.sql
-- Description: Seed initial subscription plans.
--              Prices and limits are illustrative starting points — adjust as
--              the commercial offering is finalized.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO tenant.subscription_plans (
    code, label, description,
    price, currency_id, billing_period_id, trial_days,
    max_users, max_vehicles, max_work_orders_per_month, max_storage_mb,
    features, is_public, sort_order
) VALUES
    (
        'free',
        '{"en": "Free", "sr": "Besplatni"}',
        '{"en": "Limited free tier for individuals or very small workshops.", "sr": "Ograničeno besplatno izdanje za pojedince ili vrlo male radionice."}',
        0,
        (SELECT id FROM codebook.currencies      WHERE code = 'EUR'),
        (SELECT id FROM codebook.billing_periods WHERE code = 'monthly'),
        0,
        2, 25, 20, 100,
        '{"api_access": false, "advanced_reports": false, "email_support": false}'::jsonb,
        TRUE,
        1
    ),
    (
        'trial',
        '{"en": "Trial", "sr": "Probni"}',
        '{"en": "30-day full-feature trial of the Pro plan.", "sr": "30-dnevna probna verzija Pro plana sa svim funkcijama."}',
        0,
        (SELECT id FROM codebook.currencies      WHERE code = 'EUR'),
        (SELECT id FROM codebook.billing_periods WHERE code = 'monthly'),
        30,
        NULL, NULL, NULL, 1024,
        '{"api_access": true, "advanced_reports": true, "email_support": true}'::jsonb,
        FALSE,
        2
    ),
    (
        'starter',
        '{"en": "Starter", "sr": "Starter"}',
        '{"en": "Entry-level paid plan for small workshops.", "sr": "Početni plan za male radionice."}',
        29.00,
        (SELECT id FROM codebook.currencies      WHERE code = 'EUR'),
        (SELECT id FROM codebook.billing_periods WHERE code = 'monthly'),
        14,
        5, 250, 200, 2048,
        '{"api_access": false, "advanced_reports": false, "email_support": true}'::jsonb,
        TRUE,
        3
    ),
    (
        'pro',
        '{"en": "Pro", "sr": "Pro"}',
        '{"en": "Full-feature plan for established workshops.", "sr": "Plan sa svim funkcijama za etablirane radionice."}',
        79.00,
        (SELECT id FROM codebook.currencies      WHERE code = 'EUR'),
        (SELECT id FROM codebook.billing_periods WHERE code = 'monthly'),
        14,
        20, 2000, NULL, 10240,
        '{"api_access": true, "advanced_reports": true, "email_support": true, "priority_support": false}'::jsonb,
        TRUE,
        4
    ),
    (
        'enterprise',
        '{"en": "Enterprise", "sr": "Enterprise"}',
        '{"en": "Custom plan for large operations. Contact sales.", "sr": "Prilagođeni plan za velike organizacije. Kontaktirajte prodaju."}',
        0,
        (SELECT id FROM codebook.currencies      WHERE code = 'EUR'),
        (SELECT id FROM codebook.billing_periods WHERE code = 'yearly'),
        0,
        NULL, NULL, NULL, NULL,
        '{"api_access": true, "advanced_reports": true, "email_support": true, "priority_support": true, "sla": true}'::jsonb,
        FALSE,
        5
    )
ON CONFLICT (code) DO NOTHING;

COMMIT;
