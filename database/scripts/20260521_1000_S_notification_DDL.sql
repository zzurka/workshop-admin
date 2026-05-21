-- Migration: 20260521_1000_S_notification_DDL.sql
-- Description: Create the notification schema. Hosts the transactional email
--              outbox and email templates. Designed to also house future
--              channels (SMS, push) without polluting other schemas.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-21
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS notification;

COMMENT ON SCHEMA notification IS
    'Transactional notifications: email outbox, templates, and (future) other channels.';

GRANT USAGE ON SCHEMA notification TO workshopadmin_app;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA notification TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA notification TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA notification TO workshopadmin_app;

ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA notification
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA notification
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA notification
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
