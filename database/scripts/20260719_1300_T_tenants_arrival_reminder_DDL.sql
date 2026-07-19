-- Migration: 20260719_1300_T_tenants_arrival_reminder_DDL.sql
-- Description: Add arrival_reminder_lead_days to tenant.tenants — per-tenant
--              configuration for the "bring your vehicle in" reminder email,
--              sent N days before appointments.scheduled_date to customers
--              whose vehicle is not yet at the shop.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

ALTER TABLE tenant.tenants
    ADD COLUMN IF NOT EXISTS arrival_reminder_lead_days SMALLINT DEFAULT 1;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_tenant_tenants_arrival_reminder_lead_days'
    ) THEN
        ALTER TABLE tenant.tenants
            ADD CONSTRAINT ck_tenant_tenants_arrival_reminder_lead_days
            CHECK (arrival_reminder_lead_days IS NULL OR arrival_reminder_lead_days BETWEEN 0 AND 30);
    END IF;
END $$;

COMMENT ON COLUMN tenant.tenants.arrival_reminder_lead_days IS 'How many days before appointments.scheduled_date the arrival-reminder email is sent to customers whose vehicle is not yet at the shop (appointments.arrived_at IS NULL). 0 = the morning of the scheduled day. NULL = arrival reminders disabled for this tenant. Default 1 (the day before).';

COMMIT;
