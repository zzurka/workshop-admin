-- Migration: 20260719_1320_T_email_templates_appointment_arrival_reminder_DML.sql
-- Description: Seed the 'appointment_arrival_reminder' email template — sent
--              N days (tenants.arrival_reminder_lead_days) before the scheduled
--              service day to customers whose vehicle is not yet at the shop.
--              Placeholders: {{CustomerName}}, {{WorkshopName}}, {{VehicleName}},
--              {{ScheduledDate}}.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO notification.email_templates (code, subject, body_text, body_html)
VALUES (
    'appointment_arrival_reminder',
    '{"en":"Your service appointment at {{WorkshopName}} — you can bring your vehicle in","sr":"Vaš termin u servisu {{WorkshopName}} — možete dovesti vozilo"}'::jsonb,
    '{"en":"Hello {{CustomerName}},\n\nYour service appointment at {{WorkshopName}} for {{VehicleName}} is scheduled for {{ScheduledDate}}. You are next in line — you can bring your vehicle to the workshop.\n\nIf you are unable to make it, please contact the workshop to reschedule.\n\n— {{WorkshopName}}","sr":"Zdravo {{CustomerName}},\n\nVaš termin u servisu {{WorkshopName}} za vozilo {{VehicleName}} zakazan je za {{ScheduledDate}}. Na redu ste — vozilo možete dovesti u servis.\n\nAko ste sprečeni, molimo Vas da kontaktirate servis radi promene termina.\n\n— {{WorkshopName}}"}'::jsonb,
    '{"en":"<p>Hello {{CustomerName}},</p><p>Your service appointment at <strong>{{WorkshopName}}</strong> for <strong>{{VehicleName}}</strong> is scheduled for <strong>{{ScheduledDate}}</strong>. You are next in line — you can bring your vehicle to the workshop.</p><p>If you are unable to make it, please contact the workshop to reschedule.</p><p>— {{WorkshopName}}</p>","sr":"<p>Zdravo {{CustomerName}},</p><p>Vaš termin u servisu <strong>{{WorkshopName}}</strong> za vozilo <strong>{{VehicleName}}</strong> zakazan je za <strong>{{ScheduledDate}}</strong>. Na redu ste — vozilo možete dovesti u servis.</p><p>Ako ste sprečeni, molimo Vas da kontaktirate servis radi promene termina.</p><p>— {{WorkshopName}}</p>"}'::jsonb
)
ON CONFLICT (code) DO NOTHING;

COMMIT;
