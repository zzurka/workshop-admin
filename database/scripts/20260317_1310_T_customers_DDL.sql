-- Migration: 20260317_1310_T_customers_DDL.sql
-- Description: Create the customer.customers table. Each customer represents one
--              client of a workshop (tenant). Scoped to a single tenant. The
--              same person (auth.users identity) can be a customer at multiple
--              tenants — one customer row per tenant, linked via user_id.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS customer.customers (
    id                           UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id                    UUID         NOT NULL,
    customer_type                VARCHAR(10)  NOT NULL DEFAULT 'person',
    first_name                   VARCHAR(100),
    last_name                    VARCHAR(100),
    company_name                 VARCHAR(255),
    tax_id                       VARCHAR(20),
    company_registration_number  VARCHAR(20),
	user_id       UUID,
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

    CONSTRAINT pk_customer_customers                    PRIMARY KEY (id),
    CONSTRAINT uq_customer_customers_tenant_id_id       UNIQUE (tenant_id, id),
    CONSTRAINT uq_customer_customers_tenant_id_user_id  UNIQUE (tenant_id, user_id),
    CONSTRAINT fk_customer_customers_tenant_id          FOREIGN KEY (tenant_id)  REFERENCES tenant.tenants(id),
    CONSTRAINT fk_customer_customers_user_id            FOREIGN KEY (user_id)    REFERENCES auth.users(id),
    CONSTRAINT fk_customer_customers_user_id_tenant_id  FOREIGN KEY (user_id, tenant_id) REFERENCES auth.user_tenants(user_id, tenant_id),
    CONSTRAINT fk_customer_customers_created_by    FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_customer_customers_updated_by    FOREIGN KEY (updated_by) REFERENCES auth.users(id),
    CONSTRAINT ck_customer_customers_type          CHECK (customer_type IN ('person', 'company')),
    CONSTRAINT ck_customer_customers_identity      CHECK (
        (customer_type = 'person'  AND first_name IS NOT NULL AND last_name IS NOT NULL)
     OR (customer_type = 'company' AND company_name IS NOT NULL)
    )
);

COMMENT ON TABLE  customer.customers               IS 'Workshop clients. Each customer belongs to exactly one tenant (workshop). Covers both individuals and companies (B2B/fleet) via customer_type.';
COMMENT ON COLUMN customer.customers.id            IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN customer.customers.tenant_id     IS 'The tenant (workshop) this customer belongs to.';
COMMENT ON COLUMN customer.customers.customer_type IS '''person'' or ''company''. Determines which identity fields are required — see ck_customer_customers_identity. The application derives the display name (company_name, or first_name + last_name).';
COMMENT ON COLUMN customer.customers.first_name    IS 'Required when customer_type = ''person''. For a company, first_name/last_name (if set) are the contact person, not the legal identity.';
COMMENT ON COLUMN customer.customers.last_name     IS 'Required when customer_type = ''person''. See first_name.';
COMMENT ON COLUMN customer.customers.company_name  IS 'Legal/trade name. Required when customer_type = ''company''; NULL for individuals. Used as invoices.billed_to_name for B2B invoices.';
COMMENT ON COLUMN customer.customers.tax_id        IS 'Serbian PIB (tax identification number). Company customers only. No format/checksum validation in the DB — application-level. Snapshotted onto invoices.billed_to_tax_id at issue time.';
COMMENT ON COLUMN customer.customers.company_registration_number IS 'Serbian matični broj (company registration number). Company customers only.';
COMMENT ON COLUMN customer.customers.user_id       IS 'Optional link to auth.users. Set when the customer is granted portal access. Unique per (tenant_id, user_id) — the same person can be a customer at multiple tenants. When set, (user_id, tenant_id) must be an auth.user_tenants membership (composite FK; check is skipped while user_id IS NULL).';
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
