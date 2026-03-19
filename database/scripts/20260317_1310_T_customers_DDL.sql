-- Migration: 20260317_1310_T_customers_DDL.sql
-- Description: Create the customer.customers table. Each customer represents one
--              client of a workshop (tenant). Scoped to a single tenant.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS customer.customers (
    id            UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id     UUID         NOT NULL,
    user_id       UUID,
    first_name    VARCHAR(100) NOT NULL,
    last_name     VARCHAR(100) NOT NULL,
    email         VARCHAR(255),
    phone_number  VARCHAR(50),
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city          VARCHAR(100),
    postal_code   VARCHAR(20),
    country       VARCHAR(100),
    notes         TEXT,
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by    UUID,
    updated_at    TIMESTAMPTZ,
    updated_by    UUID,
    is_deleted    BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_customer_customers               PRIMARY KEY (id),
    CONSTRAINT uq_customer_customers_user_id       UNIQUE (user_id),
    CONSTRAINT fk_customer_customers_tenant_id     FOREIGN KEY (tenant_id)  REFERENCES tenant.tenants(id),
    CONSTRAINT fk_customer_customers_user_id       FOREIGN KEY (user_id)    REFERENCES auth.users(id),
    CONSTRAINT fk_customer_customers_created_by    FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_customer_customers_updated_by    FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  customer.customers               IS 'Workshop clients. Each customer belongs to exactly one tenant (workshop).';
COMMENT ON COLUMN customer.customers.id            IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN customer.customers.tenant_id     IS 'The tenant (workshop) this customer belongs to.';
COMMENT ON COLUMN customer.customers.user_id       IS 'Optional link to auth.users. Set when the customer is granted portal access. UNIQUE — one auth account per customer.';
COMMENT ON COLUMN customer.customers.email         IS 'Customer email address. Nullable — some customers may not have one.';
COMMENT ON COLUMN customer.customers.notes         IS 'Internal workshop notes about this customer. Not visible to the customer.';
COMMENT ON COLUMN customer.customers.is_active     IS 'FALSE = customer record suspended / inactive.';
COMMENT ON COLUMN customer.customers.created_by    IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN customer.customers.updated_at    IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN customer.customers.is_deleted    IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_customer_customers_tenant_id
    ON customer.customers (tenant_id);

CREATE INDEX IF NOT EXISTS ix_customer_customers_is_deleted
    ON customer.customers (is_deleted);

CREATE INDEX IF NOT EXISTS ix_customer_customers_email
    ON customer.customers (email);

COMMIT;
