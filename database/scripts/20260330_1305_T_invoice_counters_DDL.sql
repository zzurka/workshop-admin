-- Migration: 20260330_1305_T_invoice_counters_DDL.sql
-- Description: Create the workshop.invoice_counters table. Gapless sequential
--              invoice numbering per (tenant, year) — a legal requirement in
--              Serbia. A PostgreSQL sequence is NOT used because sequences are
--              non-transactional (a rollback would leave a gap in the series).
--
--              The number is assigned at issue time, in the same transaction
--              as the invoice status change, via an atomic upsert:
--
--                INSERT INTO workshop.invoice_counters (tenant_id, year, last_number)
--                VALUES ($1, $2, 1)
--                ON CONFLICT (tenant_id, year)
--                DO UPDATE SET last_number = invoice_counters.last_number + 1,
--                              updated_at  = NOW()
--                RETURNING last_number;
--
--              The upsert holds the row lock until the transaction ends, so
--              concurrent issues get consecutive numbers and a rollback
--              releases the number for the next caller (no gaps).
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.invoice_counters (
    tenant_id   UUID        NOT NULL,
    year        SMALLINT    NOT NULL,
    last_number INTEGER     NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ,

    CONSTRAINT pk_workshop_invoice_counters             PRIMARY KEY (tenant_id, year),
    CONSTRAINT fk_workshop_invoice_counters_tenant_id   FOREIGN KEY (tenant_id) REFERENCES tenant.tenants(id),
    CONSTRAINT ck_workshop_invoice_counters_last_number CHECK (last_number >= 0)
);

COMMENT ON TABLE  workshop.invoice_counters             IS 'Gapless invoice number allocation per (tenant, year). Internal infrastructure table — no audit user columns or soft delete. See the migration header for the atomic upsert that assigns numbers.';
COMMENT ON COLUMN workshop.invoice_counters.tenant_id   IS 'The tenant (workshop) this counter belongs to.';
COMMENT ON COLUMN workshop.invoice_counters.year        IS 'Numbering year. Each tenant restarts numbering at 1 every year.';
COMMENT ON COLUMN workshop.invoice_counters.last_number IS 'Last assigned invoice number for this tenant and year. 0 = no invoices issued yet.';
COMMENT ON COLUMN workshop.invoice_counters.updated_at  IS 'NULL until the first number is assigned after row creation.';

COMMIT;
