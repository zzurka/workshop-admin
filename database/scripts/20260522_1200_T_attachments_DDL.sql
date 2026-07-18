-- Migration: 20260522_1200_T_attachments_DDL.sql
-- Description: Create the document.attachments table. One shared table for
--              file attachments across all modules (vehicle photos, work order
--              documents, complaint photos, issued invoice PDFs, ...). The
--              database stores metadata only; file content lives on the
--              filesystem (dev) or S3 (prod), selected per row via
--              storage_provider so both backends can coexist during migration.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS document.attachments (
    id               UUID          NOT NULL DEFAULT uuidv7(),
    tenant_id        UUID          NOT NULL,
    entity_type      VARCHAR(50)   NOT NULL,
    entity_id        UUID          NOT NULL,
    file_name        VARCHAR(255)  NOT NULL,
    content_type     VARCHAR(100)  NOT NULL,
    size_bytes       BIGINT        NOT NULL,
    storage_provider VARCHAR(20)   NOT NULL,
    storage_key      VARCHAR(500)  NOT NULL,
    created_at       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by       UUID,
    updated_at       TIMESTAMPTZ,
    updated_by       UUID,
    is_deleted       BOOLEAN       NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_document_attachments                  PRIMARY KEY (id),
    CONSTRAINT fk_document_attachments_tenant_id        FOREIGN KEY (tenant_id)  REFERENCES tenant.tenants(id),
    CONSTRAINT fk_document_attachments_created_by       FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT fk_document_attachments_updated_by       FOREIGN KEY (updated_by) REFERENCES auth.users(id),
    CONSTRAINT ck_document_attachments_size_bytes       CHECK (size_bytes > 0),
    CONSTRAINT ck_document_attachments_storage_provider CHECK (storage_provider IN ('filesystem', 's3'))
);

COMMENT ON TABLE  document.attachments                  IS 'File attachments for all modules. Polymorphic reference (entity_type + entity_id) intentionally has no FK: one table serves every module, referential integrity is enforced by the application''s attachment service, and entity deletion is soft everywhere so orphaning cannot occur through cascades. Tenant isolation is guarded by the real tenant_id FK.';
COMMENT ON COLUMN document.attachments.id               IS 'UUID v7 primary key (time-ordered).';
COMMENT ON COLUMN document.attachments.tenant_id        IS 'The tenant (workshop) this attachment belongs to. Sum of size_bytes per tenant is checked against subscription_plans.max_storage_mb at the application level.';
COMMENT ON COLUMN document.attachments.entity_type      IS 'Type of the owning entity: ''vehicle'', ''work_order'', ''complaint'', ''invoice'', ''expense'', ... No CHECK constraint — the list grows with new modules without a migration.';
COMMENT ON COLUMN document.attachments.entity_id        IS 'Id of the owning entity row. Polymorphic — interpreted according to entity_type; no FK.';
COMMENT ON COLUMN document.attachments.file_name        IS 'Original file name as uploaded, used for display and download.';
COMMENT ON COLUMN document.attachments.content_type     IS 'MIME type of the file (e.g. image/jpeg, application/pdf).';
COMMENT ON COLUMN document.attachments.size_bytes       IS 'File size in bytes. Always positive.';
COMMENT ON COLUMN document.attachments.storage_provider IS 'Where the file content physically lives: ''filesystem'' (dev) or ''s3'' (prod). Per row, not global — files written to the filesystem stay readable after switching to S3, and fs-to-S3 migration can proceed row by row.';
COMMENT ON COLUMN document.attachments.storage_key      IS 'Key relative to the provider: filesystem path or S3 object key. Bucket/region and filesystem root are application configuration. Key convention (application-level, same for both providers): {tenant_id}/{entity_type}/{attachment_id}.';
COMMENT ON COLUMN document.attachments.created_by       IS 'User who uploaded this file. NULL for system-generated files (e.g. issued invoice PDFs).';
COMMENT ON COLUMN document.attachments.updated_at       IS 'NULL on creation. Set on any update, including soft-delete.';
COMMENT ON COLUMN document.attachments.is_deleted       IS 'Soft delete flag. When TRUE, updated_at holds the deletion timestamp. The underlying file is retained (or cleaned up by a background job) — the row is the source of truth.';

CREATE INDEX IF NOT EXISTS ix_document_attachments_tenant_id
    ON document.attachments (tenant_id);

CREATE INDEX IF NOT EXISTS ix_document_attachments_entity
    ON document.attachments (entity_type, entity_id);

CREATE INDEX IF NOT EXISTS ix_document_attachments_is_deleted
    ON document.attachments (is_deleted);

COMMIT;
