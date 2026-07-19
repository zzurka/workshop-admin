-- Migration: 20260330_1350_V_stock_availability_DDL.sql
-- Description: warehouse.v_stock_availability — derived view of available
--              stock, accounting for parts reserved by live work orders.
--              A part on a work order with status in_stock/received reduces
--              what other work orders can draw on, without a dual-write
--              quantity_reserved column (always consistent by construction).
--              Reservation ends once the part is issued (stock_transactions
--              already reduced quantity_on_hand at that point) or the work
--              order reaches a terminal status.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent (CREATE OR REPLACE VIEW). Safe to run multiple times.

BEGIN;

CREATE OR REPLACE VIEW warehouse.v_stock_availability AS
SELECT s.tenant_id, s.catalog_part_id, s.quantity_on_hand,
       COALESCE(r.reserved, 0) AS quantity_reserved,
       s.quantity_on_hand - COALESCE(r.reserved, 0) AS quantity_available
FROM warehouse.stock s
LEFT JOIN (
    SELECT wop.tenant_id, wop.catalog_part_id, SUM(wop.quantity) AS reserved
    FROM workshop.work_order_parts wop
    JOIN workshop.work_orders wo ON wo.id = wop.work_order_id
    JOIN codebook.part_statuses ps ON ps.id = wop.part_status_id
    JOIN codebook.work_order_statuses ws ON ws.id = wo.work_order_status_id
    WHERE NOT wop.is_deleted AND NOT wo.is_deleted
      AND wop.catalog_part_id IS NOT NULL
      AND ps.code IN ('in_stock', 'received')      -- earmarked for the order, not yet pulled from warehouse
      AND ws.code NOT IN ('completed', 'cancelled') -- order still live
    GROUP BY wop.tenant_id, wop.catalog_part_id
) r USING (tenant_id, catalog_part_id);

COMMENT ON VIEW warehouse.v_stock_availability IS 'Derived stock availability per (tenant, part): quantity_on_hand minus what is reserved by live work orders (part_status in_stock/received on a work order not yet completed/cancelled). No stored quantity_reserved column, so this is always consistent — never a dual-write to drift.';

COMMIT;
