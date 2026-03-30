-- Migration: 20260331_0960_T_permissions_codebook_DML.sql
-- Description: Seed codebook/lookup data permissions.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-31
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO auth.permissions (name, resource, action, description) VALUES
    ('codebook:read',   'codebook', 'read',   'View codebook/lookup data'),
    ('codebook:manage', 'codebook', 'manage', 'Create, edit, and deactivate codebook entries')
ON CONFLICT (name) DO NOTHING;

COMMIT;
