-- Migration: 20260317_1110_T_tenants_DDL.sql
-- Description: Create the tenant.tenants table. Each tenant represents one
--              workshop / organization using the system.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS tenant.tenants (
    id                UUID        NOT NULL DEFAULT uuidv7(),
    name              VARCHAR(255) NOT NULL,
    slug              VARCHAR(100) NOT NULL,
    contact_email     VARCHAR(255),
    contact_phone     VARCHAR(50),
    subscription_plan_id SMALLINT   NOT NULL,
    address_line1     VARCHAR(255),
    address_line2     VARCHAR(255),
    city              VARCHAR(100),
    postal_code       VARCHAR(20),
    country           VARCHAR(100),
    is_active         BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by        UUID,
    updated_at        TIMESTAMPTZ,
    updated_by        UUID,
    is_deleted        BOOLEAN      NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tenant_tenants                        PRIMARY KEY (id),
    CONSTRAINT uq_tenant_tenants_slug                   UNIQUE (slug),
    CONSTRAINT fk_tenant_tenants_subscription_plan_id   FOREIGN KEY (subscription_plan_id) REFERENCES codebook.subscription_plans(id),
    CONSTRAINT fk_tenant_tenants_created_by             FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_tenant_tenants_updated_by             FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);

COMMENT ON TABLE  tenant.tenants                    IS 'Each row represents one workshop / organization (tenant) using the system.';
COMMENT ON COLUMN tenant.tenants.id                 IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN tenant.tenants.slug               IS 'URL-friendly unique identifier, e.g. ''workshop-zagreb''.';
COMMENT ON COLUMN tenant.tenants.subscription_plan_id IS 'FK to codebook.subscription_plans. Current subscription tier.';
COMMENT ON COLUMN tenant.tenants.is_active          IS 'FALSE = tenant suspended. Active users of this tenant cannot log in.';
COMMENT ON COLUMN tenant.tenants.updated_at         IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN tenant.tenants.is_deleted         IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_tenant_tenants_is_active
    ON tenant.tenants (is_active);

CREATE INDEX IF NOT EXISTS ix_tenant_tenants_is_deleted
    ON tenant.tenants (is_deleted);

COMMIT;
