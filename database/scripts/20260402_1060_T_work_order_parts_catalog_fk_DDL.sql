-- Migration: 20260402_1060_T_work_order_parts_catalog_fk_DDL.sql
-- Description: Add deferred foreign key from workshop.work_order_parts to
--              warehouse.parts_catalog. Cannot live in the original CREATE TABLE
--              because the warehouse schema is created after the workshop schema.
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_workshop_work_order_parts_catalog_part_id'
    ) THEN
        ALTER TABLE workshop.work_order_parts
            ADD CONSTRAINT fk_workshop_work_order_parts_catalog_part_id
            FOREIGN KEY (catalog_part_id) REFERENCES warehouse.parts_catalog(id);
    END IF;
END $$;

COMMIT;
