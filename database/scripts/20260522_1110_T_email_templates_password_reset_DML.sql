-- Migration: 20260522_1110_T_email_templates_password_reset_DML.sql
-- Description: Seed the 'password_reset' email template. Placeholders:
--              {{UserName}}, {{ResetUrl}}, {{ExpiresInMinutes}}.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-22
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO notification.email_templates (code, subject, body_text, body_html)
VALUES (
    'password_reset',
    '{"en":"Reset your WorkshopAdmin password","sr":"Resetujte vašu WorkshopAdmin lozinku"}'::jsonb,
    '{"en":"Hello {{UserName}},\n\nWe received a request to reset the password on your WorkshopAdmin account. To choose a new password, open the link below within {{ExpiresInMinutes}} minutes:\n\n{{ResetUrl}}\n\nIf you did not request this, you can safely ignore this email — your password will not change.\n\n— WorkshopAdmin","sr":"Zdravo {{UserName}},\n\nPrimili smo zahtev za resetovanje lozinke za Vaš WorkshopAdmin nalog. Da biste izabrali novu lozinku, otvorite link u narednih {{ExpiresInMinutes}} minuta:\n\n{{ResetUrl}}\n\nAko niste poslali ovaj zahtev, slobodno ignorišite ovu poruku — Vaša lozinka neće biti promenjena.\n\n— WorkshopAdmin"}'::jsonb,
    '{"en":"<p>Hello {{UserName}},</p><p>We received a request to reset the password on your WorkshopAdmin account. To choose a new password, open the link below within <strong>{{ExpiresInMinutes}} minutes</strong>:</p><p><a href=\"{{ResetUrl}}\">Reset your password</a></p><p>If you did not request this, you can safely ignore this email — your password will not change.</p>","sr":"<p>Zdravo {{UserName}},</p><p>Primili smo zahtev za resetovanje lozinke za Vaš WorkshopAdmin nalog. Da biste izabrali novu lozinku, otvorite link u narednih <strong>{{ExpiresInMinutes}} minuta</strong>:</p><p><a href=\"{{ResetUrl}}\">Resetujte lozinku</a></p><p>Ako niste poslali ovaj zahtev, slobodno ignorišite ovu poruku — Vaša lozinka neće biti promenjena.</p>"}'::jsonb
)
ON CONFLICT (code) DO NOTHING;

COMMIT;
