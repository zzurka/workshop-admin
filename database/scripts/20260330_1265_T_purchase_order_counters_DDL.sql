-- Migration: 20260330_1265_T_purchase_order_counters_DDL.sql
-- Description: Create the warehouse.purchase_order_counters table. Per-tenant,
--              per-year counter for purchase order numbers. Same pattern as
--              workshop.invoice_counters, with one difference: the number is
--              assigned at PO creation (not issue) — PO numbering is an
--              internal reference and does not need to be gapless.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS warehouse.purchase_order_counters (
    tenant_id   UUID        NOT NULL,
    year        SMALLINT    NOT NULL,
    last_number INTEGER     NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT pk_warehouse_purchase_order_counters           PRIMARY KEY (tenant_id, year),
    CONSTRAINT fk_warehouse_purchase_order_counters_tenant_id FOREIGN KEY (tenant_id) REFERENCES tenant.tenants(id)
);

COMMENT ON TABLE  warehouse.purchase_order_counters             IS 'Per-tenant, per-year purchase order number counter. Atomic upsert (INSERT ... ON CONFLICT DO UPDATE ... RETURNING) assigns the next number at PO creation; the row lock serializes concurrent creations. Gaps are acceptable (internal document).';
COMMENT ON COLUMN warehouse.purchase_order_counters.tenant_id   IS 'The tenant this counter belongs to.';
COMMENT ON COLUMN warehouse.purchase_order_counters.year        IS 'Numbering year.';
COMMENT ON COLUMN warehouse.purchase_order_counters.last_number IS 'Last assigned PO number for this tenant and year.';

COMMIT;
