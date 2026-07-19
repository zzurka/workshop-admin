-- Migration: 20260522_1130_T_email_templates_email_verification_DML.sql
-- Description: Seed the 'email_verification' email template. Placeholders:
--              {{UserName}}, {{VerificationUrl}}, {{ExpiresInMinutes}}.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO notification.email_templates (code, subject, body_text, body_html)
VALUES (
    'email_verification',
    '{"en":"Verify your WorkshopAdmin email address","sr":"Potvrdite Vašu WorkshopAdmin email adresu"}'::jsonb,
    '{"en":"Hello {{UserName}},\n\nPlease confirm your email address to activate your WorkshopAdmin account. Open the link below within {{ExpiresInMinutes}} minutes:\n\n{{VerificationUrl}}\n\nIf you did not create this account, you can safely ignore this email.\n\n— WorkshopAdmin","sr":"Zdravo {{UserName}},\n\nPotvrdite Vašu email adresu da biste aktivirali WorkshopAdmin nalog. Otvorite link u narednih {{ExpiresInMinutes}} minuta:\n\n{{VerificationUrl}}\n\nAko niste Vi kreirali ovaj nalog, slobodno ignorišite ovu poruku.\n\n— WorkshopAdmin"}'::jsonb,
    '{"en":"<p>Hello {{UserName}},</p><p>Please confirm your email address to activate your WorkshopAdmin account. Open the link below within <strong>{{ExpiresInMinutes}} minutes</strong>:</p><p><a href=\"{{VerificationUrl}}\">Verify your email</a></p><p>If you did not create this account, you can safely ignore this email.</p>","sr":"<p>Zdravo {{UserName}},</p><p>Potvrdite Vašu email adresu da biste aktivirali WorkshopAdmin nalog. Otvorite link u narednih <strong>{{ExpiresInMinutes}} minuta</strong>:</p><p><a href=\"{{VerificationUrl}}\">Potvrdite email</a></p><p>Ako niste Vi kreirali ovaj nalog, slobodno ignorišite ovu poruku.</p>"}'::jsonb
)
ON CONFLICT (code) DO NOTHING;

COMMIT;
