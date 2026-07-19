-- Migration: 20260719_1310_T_appointments_arrival_reminder_DDL.sql
-- Description: Add arrival_reminder_sent_at to workshop.appointments and extend
--              the arrived_at column comment. "Vehicle on site" is intentionally
--              NOT a separate boolean — it is derived as arrived_at IS NOT NULL:
--              portal bookings never set arrived_at (always "not on site"), while
--              an in-person booking where the customer leaves a non-roadworthy
--              vehicle sets arrived_at at booking time.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

ALTER TABLE workshop.appointments
    ADD COLUMN IF NOT EXISTS arrival_reminder_sent_at TIMESTAMPTZ;

COMMENT ON COLUMN workshop.appointments.arrival_reminder_sent_at IS 'When the "you are next — bring your vehicle in" reminder email was enqueued for this appointment. The daily reminder job targets appointments with scheduled_date = today + tenants.arrival_reminder_lead_days, arrived_at IS NULL and arrival_reminder_sent_at IS NULL; it sets this column in the same transaction as the notification.email_outbox insert, guaranteeing at-most-once delivery. NULL = not sent (or not applicable — vehicle already on site).';

COMMENT ON COLUMN workshop.appointments.arrived_at IS 'When the vehicle physically arrived at the shop. "Vehicle on site" is derived, not stored: arrived_at IS NOT NULL. Set at booking when the customer leaves the vehicle immediately (e.g. booked in person, vehicle not roadworthy), at arrival for walk-ins, or at check-in for scheduled appointments. Portal bookings never set it — the customer still has the vehicle. Used by day-queue planning (which vehicles are already here) and by the arrival-reminder job (only appointments with arrived_at IS NULL get the "bring your vehicle in" email).';

COMMIT;
