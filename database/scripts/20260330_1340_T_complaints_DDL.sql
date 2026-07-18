-- Migration: 20260330_1340_T_complaints_DDL.sql
-- Description: Create the workshop.complaints table. Customer complaints
--              (reklamacije) on completed work: submission (in person or via
--              the portal), decision (accepted/rejected), and resolution —
--              often through a new warranty work order. Legal context: Zakon o
--              zaštiti potrošača, 8-day response deadline (tracked by the
--              application from submitted_at/reviewed_at).
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS workshop.complaints (
    id                       UUID        NOT NULL DEFAULT uuidv7(),
    tenant_id                UUID        NOT NULL,
    customer_id              UUID        NOT NULL,
    work_order_id            UUID,
    invoice_id               UUID,
    complaint_status_id      SMALLINT    NOT NULL,
    description              TEXT        NOT NULL,
    submitted_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    reviewed_by              UUID,
    reviewed_at              TIMESTAMPTZ,
    resolution               TEXT,
    resolution_work_order_id UUID,
    resolved_at              TIMESTAMPTZ,
    notes                    TEXT,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by               UUID,
    updated_at               TIMESTAMPTZ,
    updated_by               UUID,
    is_deleted               BOOLEAN     NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_workshop_complaints                          PRIMARY KEY (id),
    CONSTRAINT fk_workshop_complaints_tenant_id                FOREIGN KEY (tenant_id)                           REFERENCES tenant.tenants(id),
    CONSTRAINT fk_workshop_complaints_customer_id              FOREIGN KEY (tenant_id, customer_id)              REFERENCES customer.customers(tenant_id, id),
    CONSTRAINT fk_workshop_complaints_work_order_id            FOREIGN KEY (tenant_id, work_order_id)            REFERENCES workshop.work_orders(tenant_id, id),
    CONSTRAINT fk_workshop_complaints_invoice_id               FOREIGN KEY (tenant_id, invoice_id)               REFERENCES workshop.invoices(tenant_id, id),
    CONSTRAINT fk_workshop_complaints_complaint_status_id      FOREIGN KEY (complaint_status_id)                 REFERENCES codebook.complaint_statuses(id),
    CONSTRAINT fk_workshop_complaints_reviewed_by              FOREIGN KEY (tenant_id, reviewed_by)              REFERENCES hr.employees(tenant_id, id),
    CONSTRAINT fk_workshop_complaints_resolution_work_order_id FOREIGN KEY (tenant_id, resolution_work_order_id) REFERENCES workshop.work_orders(tenant_id, id),
    CONSTRAINT fk_workshop_complaints_created_by               FOREIGN KEY (created_by)                          REFERENCES auth.users(id),
    CONSTRAINT fk_workshop_complaints_updated_by               FOREIGN KEY (updated_by)                          REFERENCES auth.users(id),
    CONSTRAINT ck_workshop_complaints_target                   CHECK (work_order_id IS NOT NULL OR invoice_id IS NOT NULL)
);

COMMENT ON TABLE  workshop.complaints                          IS 'Customer complaints on completed work. Flow (application rule): submitted -> in_review -> accepted -> (warranty work order via resolution_work_order_id) -> resolved; or rejected (terminal, reasoning in resolution); cancelled from submitted/in_review. The warranty work order is a regular work_order — whether it is invoiced at 0 is an application decision. No complaint number in v1 (not a legally numbered document; identified by UUID + date). Credit notes for refunds are out of scope (separate document type, see plan 1.7).';
COMMENT ON COLUMN workshop.complaints.id                       IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN workshop.complaints.tenant_id                IS 'The tenant (workshop) this complaint belongs to.';
COMMENT ON COLUMN workshop.complaints.customer_id              IS 'The customer filing the complaint. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN workshop.complaints.work_order_id            IS 'The work order the complaint refers to. At least one of work_order_id / invoice_id is required (ck_workshop_complaints_target). Composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.complaints.invoice_id               IS 'The invoice the complaint refers to. At least one of work_order_id / invoice_id is required. Composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.complaints.complaint_status_id      IS 'FK to codebook.complaint_statuses (submitted, in_review, accepted, rejected, resolved, cancelled).';
COMMENT ON COLUMN workshop.complaints.description              IS 'What the customer is complaining about, in their words.';
COMMENT ON COLUMN workshop.complaints.submitted_at             IS 'When the complaint was submitted. May differ from created_at when a verbal complaint is entered later. The legal 8-day response deadline counts from here (tracked by the application).';
COMMENT ON COLUMN workshop.complaints.reviewed_by              IS 'Employee who made the accept/reject decision. NULL while pending. Composite FK — must belong to the same tenant.';
COMMENT ON COLUMN workshop.complaints.reviewed_at              IS 'When the decision was made. NULL while pending.';
COMMENT ON COLUMN workshop.complaints.resolution               IS 'How the complaint was resolved, or the rejection reasoning.';
COMMENT ON COLUMN workshop.complaints.resolution_work_order_id IS 'Warranty work order created to fix the complaint. NULL if resolved without one. Composite FK check is skipped while NULL.';
COMMENT ON COLUMN workshop.complaints.resolved_at              IS 'When the complaint was fully resolved. NULL until then.';
COMMENT ON COLUMN workshop.complaints.notes                    IS 'Internal notes, not visible to the customer.';
COMMENT ON COLUMN workshop.complaints.created_by               IS 'User who created this record: the customer''s auth.users account for portal submissions, or the employee entering a verbal complaint. NULL for system/seed records.';
COMMENT ON COLUMN workshop.complaints.updated_at               IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN workshop.complaints.is_deleted               IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp.';

CREATE INDEX IF NOT EXISTS ix_workshop_complaints_tenant_id
    ON workshop.complaints (tenant_id);

CREATE INDEX IF NOT EXISTS ix_workshop_complaints_customer_id
    ON workshop.complaints (customer_id);

CREATE INDEX IF NOT EXISTS ix_workshop_complaints_work_order_id
    ON workshop.complaints (work_order_id);

CREATE INDEX IF NOT EXISTS ix_workshop_complaints_invoice_id
    ON workshop.complaints (invoice_id);

CREATE INDEX IF NOT EXISTS ix_workshop_complaints_complaint_status_id
    ON workshop.complaints (complaint_status_id);

CREATE INDEX IF NOT EXISTS ix_workshop_complaints_is_deleted
    ON workshop.complaints (is_deleted);

COMMIT;
