-- Migration: 20260317_0903_S_customer_DDL.sql
-- Description: Create the customer schema for customer and vehicle objects
--              (customers / workshop clients and their vehicles).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS customer;

COMMENT ON SCHEMA customer IS
    'Customer data: workshop clients and their associated vehicles.';

-- App user: schema access
GRANT USAGE ON SCHEMA customer TO workshopadmin_app;

-- App user: DML on all current tables/sequences/functions in this schema
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA customer TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA customer TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA customer TO workshopadmin_app;

-- App user: auto-grant on future objects created by workshopadmin_admin
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA customer
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA customer
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA customer
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
