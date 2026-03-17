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

COMMIT;
