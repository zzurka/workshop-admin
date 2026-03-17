-- Migration: 20260317_1000_S_auth_DDL.sql
-- Description: Create the auth schema for authentication and authorization objects
--              (users, roles, permissions, and their junction tables).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS auth;

COMMENT ON SCHEMA auth IS
    'Authentication and authorization: users, roles, permissions, and their relationships.';

-- App user: schema access
GRANT USAGE ON SCHEMA auth TO workshopadmin_app;

-- App user: DML on all current tables/sequences/functions in this schema
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA auth TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA auth TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA auth TO workshopadmin_app;

-- App user: auto-grant on future objects created by workshopadmin_admin
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA auth
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA auth
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA auth
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
