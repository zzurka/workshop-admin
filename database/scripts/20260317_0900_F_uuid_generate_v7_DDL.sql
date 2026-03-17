-- Migration: 20260317_0900_F_uuid_generate_v7_DDL.sql
-- Description: Drop the custom uuid_generate_v7() function — superseded by the
--              built-in uuidv7() available since PostgreSQL 18.
--              All tables use uuidv7() directly as the primary key default.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

DROP FUNCTION IF EXISTS public.uuid_generate_v7();

COMMIT;
