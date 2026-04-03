-- Migration: 20260401_1010_T_expenses_DDL.sql
-- Description: Create the workshop.expenses table. Tracks operational costs
--              of the workshop (rent, utilities, tools, parts, etc.).
-- Author: WorkshopAdmin Team
-- Date: 2026-04-01
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.expenses (
    id                  UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id           UUID          NOT NULL,
    expense_category_id SMALLINT      NOT NULL,
    supplier_id         UUID,
    employee_id         UUID,
    description         TEXT          NOT NULL,
    amount              NUMERIC(12,2) NOT NULL,
    expense_date        DATE          NOT NULL,
    notes               TEXT,
    created_at          TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by          UUID,
    updated_at          TIMESTAMPTZ,
    updated_by          UUID,
    is_deleted          BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_expenses                      PRIMARY KEY (id),
    CONSTRAINT fk_workshop_expenses_tenant_id            FOREIGN KEY (tenant_id)           REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_expenses_expense_category_id  FOREIGN KEY (expense_category_id) REFERENCES codebook.expense_categories(id),
    CONSTRAINT fk_workshop_expenses_supplier_id          FOREIGN KEY (supplier_id)         REFERENCES workshop.suppliers(id),
    CONSTRAINT fk_workshop_expenses_employee_id          FOREIGN KEY (employee_id)         REFERENCES hr.employees(id),
    CONSTRAINT fk_workshop_expenses_created_by           FOREIGN KEY (created_by)          REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_expenses_updated_by           FOREIGN KEY (updated_by)          REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_expenses_amount               CHECK (amount > 0)
);

COMMENT ON TABLE  workshop.expenses                          IS 'Operational expenses of the workshop. Each expense belongs to one tenant.';
COMMENT ON COLUMN workshop.expenses.id                       IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.expenses.tenant_id                IS 'The tenant (workshop) this expense belongs to.';
COMMENT ON COLUMN workshop.expenses.expense_category_id      IS 'FK to codebook.expense_categories (e.g. rent, utilities, tools).';
COMMENT ON COLUMN workshop.expenses.supplier_id              IS 'Optional FK to workshop.suppliers. Set when the expense is from a known supplier.';
COMMENT ON COLUMN workshop.expenses.employee_id              IS 'Optional FK to hr.employees. The employee who incurred or submitted the expense.';
COMMENT ON COLUMN workshop.expenses.description              IS 'Short description of what the expense is for.';
COMMENT ON COLUMN workshop.expenses.amount                   IS 'Expense amount. Must be positive.';
COMMENT ON COLUMN workshop.expenses.expense_date             IS 'The date the expense occurred.';
COMMENT ON COLUMN workshop.expenses.notes                    IS 'Optional internal notes about this expense.';
COMMENT ON COLUMN workshop.expenses.created_by               IS 'User who created this record. NULL for system/seed records.';
COMMENT ON COLUMN workshop.expenses.updated_at               IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.expenses.is_deleted               IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_expenses_tenant_id
    ON workshop.expenses (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_expenses_expense_category_id
    ON workshop.expenses (expense_category_id);

CREATE INDEX IF NOT EXISTS ix_workshop_expenses_expense_date
    ON workshop.expenses (expense_date);

CREATE INDEX IF NOT EXISTS ix_workshop_expenses_is_deleted
    ON workshop.expenses (is_deleted);

COMMIT;
