-- Migration: 20260402_1000_S_warehouse_DDL.sql
-- Description: Create the warehouse schema for parts catalog, stock levels,
--              and stock movement tracking.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS warehouse;

COMMENT ON SCHEMA warehouse IS
    'Parts catalog, inventory stock levels, and stock movement transactions.';

-- App user: schema access
GRANT USAGE ON SCHEMA warehouse TO workshopadmin_app;

-- App user: DML on all current tables/sequences/functions in this schema
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA warehouse TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA warehouse TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA warehouse TO workshopadmin_app;

-- App user: auto-grant on future objects created by workshopadmin_admin
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA warehouse
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA warehouse
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA warehouse
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
