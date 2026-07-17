-- Migration: 20260317_1002_T_tenants_DDL.sql
-- Description: Create the tenant.tenants table. Each tenant represents one
--              workshop / organization using the system. Includes the seller's
--              fiscal data (PIB, matični broj, VAT status, bank account)
--              required on issued invoices.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS tenant.tenants (
    id                          UUID         NOT NULL DEFAULT uuidv7(),
    name                        VARCHAR(255) NOT NULL,
    slug                        VARCHAR(100) NOT NULL,
    contact_email               VARCHAR(255),
    contact_phone               VARCHAR(50),
    subscription_plan_id        UUID         NOT NULL,
    default_currency_id         SMALLINT     NOT NULL,
    tax_id                      VARCHAR(20),
    company_registration_number VARCHAR(20),
    is_vat_registered           BOOLEAN      NOT NULL DEFAULT FALSE,
    bank_account_number         VARCHAR(50),
    address_line1               VARCHAR(255),
    address_line2               VARCHAR(255),
    city                        VARCHAR(100),
    postal_code                 VARCHAR(20),
    country                     VARCHAR(100),
    is_active                   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by                  UUID,
    updated_at                  TIMESTAMPTZ,
    updated_by                  UUID,
    is_deleted                  BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tenant_tenants                       PRIMARY KEY (id),
    CONSTRAINT uq_tenant_tenants_slug                  UNIQUE (slug),
    CONSTRAINT fk_tenant_tenants_subscription_plan_id  FOREIGN KEY (subscription_plan_id) REFERENCES tenant.subscription_plans(id),
    CONSTRAINT fk_tenant_tenants_default_currency_id   FOREIGN KEY (default_currency_id)  REFERENCES codebook.currencies(id)
    -- created_by / updated_by FKs added in 20260317_1020_T_users_tenants_fk_DDL.sql (circular dependency)
);

COMMENT ON TABLE  tenant.tenants                             IS 'Each row represents one workshop / organization (tenant) using the system.';
COMMENT ON COLUMN tenant.tenants.id                          IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN tenant.tenants.slug                        IS 'URL-friendly unique identifier, e.g. ''workshop-kragujevac''.';
COMMENT ON COLUMN tenant.tenants.subscription_plan_id        IS 'FK to tenant.subscription_plans. Current subscription tier.';
COMMENT ON COLUMN tenant.tenants.default_currency_id         IS 'FK to codebook.currencies. The tenant''s operational currency, used as the default for invoices, expenses, and parts pricing. Independent of the subscription plan billing currency.';
COMMENT ON COLUMN tenant.tenants.tax_id                      IS 'Serbian PIB (tax identification number, 9 digits). Legally required on issued invoices — the application must require it before the first invoice is issued. Nullable so onboarding does not demand it upfront. Not UNIQUE: multiple tenants (locations) of the same legal entity are legitimate.';
COMMENT ON COLUMN tenant.tenants.company_registration_number IS 'Serbian matični broj (company registration number).';
COMMENT ON COLUMN tenant.tenants.is_vat_registered           IS 'TRUE = tenant is in the VAT system. FALSE = small business outside the VAT system (čl. 33 ZPDV) — all invoice lines then use the ''exempt'' tax rate and invoices carry a vat_note.';
COMMENT ON COLUMN tenant.tenants.bank_account_number         IS 'Bank account (tekući račun) printed on invoices as the payment instruction.';
COMMENT ON COLUMN tenant.tenants.is_active                   IS 'FALSE = tenant suspended. Active users of this tenant cannot log in.';
COMMENT ON COLUMN tenant.tenants.updated_at                  IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN tenant.tenants.is_deleted                  IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_tenant_tenants_is_active
    ON tenant.tenants (is_active);

CREATE INDEX IF NOT EXISTS ix_tenant_tenants_is_deleted
    ON tenant.tenants (is_deleted);

COMMIT;
