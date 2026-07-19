-- Migration: 20260317_1040_T_tenant_subscriptions_DDL.sql
-- Description: Create the tenant.tenant_subscriptions table. History of
--              subscription plan periods per tenant (plan changes, trials).
--              tenants.subscription_plan_id stays as a denormalized pointer
--              to the current plan for fast feature-gating lookups; the
--              application keeps it in sync whenever a new period starts.
--              Platform billing for subscriptions is out of scope for v1 —
--              this is period history only.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS tenant.tenant_subscriptions (
    id                    UUID        NOT NULL DEFAULT uuidv7(),
    tenant_id             UUID        NOT NULL,
    subscription_plan_id  UUID        NOT NULL,
    valid_from            DATE        NOT NULL,
    valid_to              DATE,
    trial_until           DATE,
    notes                 TEXT,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by            UUID,
    updated_at            TIMESTAMPTZ,
    updated_by            UUID,
    is_deleted            BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tenant_tenant_subscriptions                     PRIMARY KEY (id),
    CONSTRAINT fk_tenant_tenant_subscriptions_tenant_id            FOREIGN KEY (tenant_id)             REFERENCES tenant.tenants(id),
    CONSTRAINT fk_tenant_tenant_subscriptions_subscription_plan_id FOREIGN KEY (subscription_plan_id)  REFERENCES tenant.subscription_plans(id),
    CONSTRAINT fk_tenant_tenant_subscriptions_created_by           FOREIGN KEY (created_by)            REFERENCES auth.users(id),
    CONSTRAINT fk_tenant_tenant_subscriptions_updated_by           FOREIGN KEY (updated_by)            REFERENCES auth.users(id),
    CONSTRAINT ck_tenant_tenant_subscriptions_valid_range          CHECK (valid_to IS NULL OR valid_to >= valid_from)
);

COMMENT ON TABLE  tenant.tenant_subscriptions                    IS 'History of subscription plan periods per tenant. One row per period (initial signup, upgrade, downgrade, trial). Application rule: creating a new period closes the previous one (sets its valid_to) and updates tenants.subscription_plan_id to match.';
COMMENT ON COLUMN tenant.tenant_subscriptions.id                 IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN tenant.tenant_subscriptions.tenant_id          IS 'The tenant this subscription period belongs to.';
COMMENT ON COLUMN tenant.tenant_subscriptions.subscription_plan_id IS 'FK to tenant.subscription_plans — the plan active during this period.';
COMMENT ON COLUMN tenant.tenant_subscriptions.valid_from         IS 'First day this period applies.';
COMMENT ON COLUMN tenant.tenant_subscriptions.valid_to           IS 'Last day this period applies (inclusive). NULL = the current period — at most one per tenant, enforced by uq_tenant_tenant_subscriptions_open.';
COMMENT ON COLUMN tenant.tenant_subscriptions.trial_until        IS 'End of the trial window within this period. NULL = not a trial.';
COMMENT ON COLUMN tenant.tenant_subscriptions.notes               IS 'Internal notes about this period (e.g. reason for plan change).';
COMMENT ON COLUMN tenant.tenant_subscriptions.created_by          IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN tenant.tenant_subscriptions.updated_at          IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN tenant.tenant_subscriptions.is_deleted          IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

-- At most one current (valid_to IS NULL) subscription period per tenant.
CREATE UNIQUE INDEX IF NOT EXISTS uq_tenant_tenant_subscriptions_open
    ON tenant.tenant_subscriptions (tenant_id)
    WHERE valid_to IS NULL AND NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_tenant_tenant_subscriptions_tenant_id
    ON tenant.tenant_subscriptions (tenant_id);

CREATE INDEX IF NOT EXISTS ix_tenant_tenant_subscriptions_subscription_plan_id
    ON tenant.tenant_subscriptions (subscription_plan_id);

CREATE INDEX IF NOT EXISTS ix_tenant_tenant_subscriptions_is_deleted
    ON tenant.tenant_subscriptions (is_deleted);

COMMIT;
