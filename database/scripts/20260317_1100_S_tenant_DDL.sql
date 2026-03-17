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

COMMIT;
