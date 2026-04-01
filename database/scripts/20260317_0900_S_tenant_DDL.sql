-- Migration: 20260317_1100_S_tenant_DDL.sql
-- Description: Create the tenant schema for multi-tenancy objects
--              (tenants / workshop organizations).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS tenant;

COMMENT ON SCHEMA tenant IS
    'Multi-tenancy: tenant (workshop/organization) records and related configuration.';

-- App user: schema access
GRANT USAGE ON SCHEMA tenant TO workshopadmin_app;

-- App user: DML on all current tables/sequences/functions in this schema
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA tenant TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA tenant TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA tenant TO workshopadmin_app;

-- App user: auto-grant on future objects created by workshopadmin_admin
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA tenant
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA tenant
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA tenant
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
