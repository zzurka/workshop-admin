-- Migration: 20260331_0906_T_permissions_codebook_DML.sql
-- Description: Seed codebook/lookup data permissions.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description, scope) VALUES
    ('codebook:read',   'codebook', 'read',   'View codebook/lookup data',                     'tenant'),
    ('codebook:manage', 'codebook', 'manage', 'Create, edit, and deactivate codebook entries', 'platform')
ON CONFLICT (name) DO NOTHING;

COMMIT;
