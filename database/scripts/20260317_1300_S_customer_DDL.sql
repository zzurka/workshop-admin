-- Migration: 20260317_1300_S_customer_DDL.sql
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

GRANT USAGE ON SCHEMA customer TO workshopadmin_app;

COMMIT;
