-- Migration: 20260402_1050_T_stock_transactions_DDL.sql
-- Description: Create the warehouse.stock_transactions table. Immutable log of
--              all stock movements (receipts, issues, returns, adjustments).
-- Author: WorkshopAdmin Team
-- Date: 2026-04-02
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS warehouse.stock_transactions (
    id                    UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id             UUID          NOT NULL,
    catalog_part_id       UUID          NOT NULL,
    transaction_type_id   SMALLINT      NOT NULL,
    quantity              NUMERIC(10,2) NOT NULL,
    work_order_id         UUID,
    supplier_id           UUID,
    notes                 TEXT,
    created_at            TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by            UUID        NOT NULL,

    CONSTRAINT pk_warehouse_stock_transactions                       PRIMARY KEY (id),
    CONSTRAINT fk_warehouse_stock_transactions_tenant_id             FOREIGN KEY (tenant_id)           REFERENCES tenant.tenants(id),
    CONSTRAINT fk_warehouse_stock_transactions_catalog_part_id       FOREIGN KEY (catalog_part_id)     REFERENCES warehouse.parts_catalog(id),
    CONSTRAINT fk_warehouse_stock_transactions_transaction_type_id   FOREIGN KEY (transaction_type_id) REFERENCES codebook.stock_transaction_types(id),
    CONSTRAINT fk_warehouse_stock_transactions_work_order_id         FOREIGN KEY (work_order_id)       REFERENCES workshop.work_orders(id),
    CONSTRAINT fk_warehouse_stock_transactions_supplier_id           FOREIGN KEY (supplier_id)         REFERENCES workshop.suppliers(id),
    CONSTRAINT fk_warehouse_stock_transactions_created_by            FOREIGN KEY (created_by)          REFERENCES auth.users(id),
    CONSTRAINT ck_warehouse_stock_transactions_quantity               CHECK (quantity <> 0)
);

COMMENT ON TABLE  warehouse.stock_transactions                          IS 'Immutable audit log of all stock movements. Positive quantity = stock in, negative = stock out.';
COMMENT ON COLUMN warehouse.stock_transactions.id                       IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN warehouse.stock_transactions.tenant_id                IS 'The tenant (workshop) this transaction belongs to.';
COMMENT ON COLUMN warehouse.stock_transactions.catalog_part_id          IS 'FK to warehouse.parts_catalog. The part being moved.';
COMMENT ON COLUMN warehouse.stock_transactions.transaction_type_id      IS 'FK to codebook.stock_transaction_types (receipt, issue, return, adjustment_in, adjustment_out).';
COMMENT ON COLUMN warehouse.stock_transactions.quantity                 IS 'Quantity moved. Positive for stock in (receipt, return, adjustment_in), negative for stock out (issue, adjustment_out). Cannot be zero.';
COMMENT ON COLUMN warehouse.stock_transactions.work_order_id            IS 'FK to workshop.work_orders. Set when parts are issued to or returned from a work order. NULL otherwise.';
COMMENT ON COLUMN warehouse.stock_transactions.supplier_id              IS 'FK to workshop.suppliers. Set when parts are received from a supplier. NULL otherwise.';
COMMENT ON COLUMN warehouse.stock_transactions.notes                    IS 'Optional notes about this transaction (e.g. reason for adjustment).';
COMMENT ON COLUMN warehouse.stock_transactions.created_by               IS 'User who performed this transaction.';

CREATE INDEX IF NOT EXISTS ix_warehouse_stock_transactions_tenant_id
    ON warehouse.stock_transactions (tenant_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_stock_transactions_catalog_part_id
    ON warehouse.stock_transactions (catalog_part_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_stock_transactions_work_order_id
    ON warehouse.stock_transactions (work_order_id);

CREATE INDEX IF NOT EXISTS ix_warehouse_stock_transactions_created_at
    ON warehouse.stock_transactions (created_at);

COMMIT;
