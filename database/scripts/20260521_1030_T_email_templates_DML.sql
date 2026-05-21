-- Migration: 20260521_1030_T_email_templates_DML.sql
-- Description: Seed the 'welcome' email template (sent on tenant creation).
--              Placeholders supported: {{TenantName}}, {{AdminName}},
--              {{AdminEmail}}, {{LoginUrl}}.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-21
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO notification.email_templates (code, subject, body_text, body_html)
VALUES (
    'welcome',
    '{"en":"Your WorkshopAdmin workspace ''{{TenantName}}'' is ready","sr":"Vaš WorkshopAdmin radni prostor ''{{TenantName}}'' je spreman"}'::jsonb,
    '{"en":"Hello {{AdminName}},\n\nThe workspace \"{{TenantName}}\" has been created and your administrator account ({{AdminEmail}}) is active.\n\nSign in here: {{LoginUrl}}\n\n— WorkshopAdmin","sr":"Zdravo {{AdminName}},\n\nRadni prostor \"{{TenantName}}\" je kreiran i Vaš administratorski nalog ({{AdminEmail}}) je aktivan.\n\nPrijavite se ovde: {{LoginUrl}}\n\n— WorkshopAdmin"}'::jsonb,
    '{"en":"<p>Hello {{AdminName}},</p><p>The workspace <strong>{{TenantName}}</strong> has been created and your administrator account (<strong>{{AdminEmail}}</strong>) is active.</p><p><a href=\"{{LoginUrl}}\">Sign in to WorkshopAdmin</a></p>","sr":"<p>Zdravo {{AdminName}},</p><p>Radni prostor <strong>{{TenantName}}</strong> je kreiran i Vaš administratorski nalog (<strong>{{AdminEmail}}</strong>) je aktivan.</p><p><a href=\"{{LoginUrl}}\">Prijavite se na WorkshopAdmin</a></p>"}'::jsonb
)
ON CONFLICT (code) DO NOTHING;

COMMIT;
