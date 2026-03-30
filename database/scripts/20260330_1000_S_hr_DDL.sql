-- Migration: 20260330_1000_S_hr_DDL.sql
-- Description: Create the hr schema for employee management, time tracking,
--              and leave/absence tracking.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS hr;

COMMENT ON SCHEMA hr IS
    'Human resources: employees, time entries, leave balances, and leave requests.';

-- App user: schema access
GRANT USAGE ON SCHEMA hr TO workshopadmin_app;

-- App user: DML on all current tables/sequences/functions in this schema
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA hr TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA hr TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA hr TO workshopadmin_app;

-- App user: auto-grant on future objects created by workshopadmin_admin
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA hr
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA hr
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA hr
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
