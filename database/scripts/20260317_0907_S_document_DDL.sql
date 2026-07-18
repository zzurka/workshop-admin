-- Migration: 20260317_0907_S_document_DDL.sql
-- Description: Create the document schema. Hosts file attachments (vehicle
--              photos, work order documents, complaint photos, issued invoice
--              PDFs, ...) referenced polymorphically from all modules.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE SCHEMA IF NOT EXISTS document;

COMMENT ON SCHEMA document IS
    'File attachments for all modules: metadata in the database, file content on filesystem (dev) or S3 (prod).';

GRANT USAGE ON SCHEMA document TO workshopadmin_app;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA document TO workshopadmin_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA document TO workshopadmin_app;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA document TO workshopadmin_app;

ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA document
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA document
    GRANT USAGE, SELECT ON SEQUENCES TO workshopadmin_app;
ALTER DEFAULT PRIVILEGES FOR ROLE workshopadmin_admin IN SCHEMA document
    GRANT EXECUTE ON FUNCTIONS TO workshopadmin_app;

COMMIT;
