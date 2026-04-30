-- Migration: 20260319_0900_S_codebook_DDL.sql
-- Description: Create the codebook schema for shared reference/lookup tables
--              (fuel types, transmission types, statuses, etc.).
-- Author: WorkshopAdmin Team
-- Date: 2026-03-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS codebook;

COMMENT ON SCHEMA codebook IS
    'Reference data: shared lookup tables used across all domain schemas (fuel types, transmission types, statuses, etc.).';

-- App user: schema access
GRANT USAGE ON SCHEMA codebook TO workshopadmin_app;

-- App user: DML on all current tables/sequences/functions in this schema
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA codebook TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA codebook TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA codebook TO workshopadmin_app;

-- App user: auto-grant on future objects created by workshopadmin_admin
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA codebook
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA codebook
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA codebook
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
