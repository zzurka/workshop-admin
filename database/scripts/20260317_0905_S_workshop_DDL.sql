-- Migration: 20260317_0905_S_workshop_DDL.sql
-- Description: Create the workshop schema for appointments, work orders,
--              parts tracking, and suppliers.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS workshop;

COMMENT ON SCHEMA workshop IS
    'Workshop operations: appointments, work orders, parts, and suppliers.';

-- App user: schema access
GRANT USAGE ON SCHEMA workshop TO workshopadmin_app;

-- App user: DML on all current tables/sequences/functions in this schema
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA workshop TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA workshop TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA workshop TO workshopadmin_app;

-- App user: auto-grant on future objects created by workshopadmin_admin
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA workshop
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA workshop
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA workshop
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
