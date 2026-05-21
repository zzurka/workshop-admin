-- Migration: 20260521_1010_T_email_outbox_DDL.sql
-- Description: Transactional email outbox. Rows are enqueued inside the
--              originating business transaction (atomic with the action that
--              triggers the email). A background dispatcher claims pending
--              rows, sends them via SMTP, and updates status. Rendered subject
--              and body are persisted on enqueue so later template edits do
--              not retroactively change queued messages.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-21
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS notification.email_outbox (
    id               UUID         NOT NULL DEFAULT uuidv7(),
    tenant_id        UUID,
    to_address       VARCHAR(320) NOT NULL,
    to_name          VARCHAR(200),
    subject          VARCHAR(500) NOT NULL,
    body_text        TEXT         NOT NULL,
    body_html        TEXT,
    status           VARCHAR(20)  NOT NULL DEFAULT 'pending',
    attempts         INT          NOT NULL DEFAULT 0,
    max_attempts     INT          NOT NULL DEFAULT 5,
    last_error       TEXT,
    next_attempt_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    sent_at          TIMESTAMPTZ,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by       UUID,

    CONSTRAINT pk_notification_email_outbox            PRIMARY KEY (id),
    CONSTRAINT fk_notification_email_outbox_tenant_id  FOREIGN KEY (tenant_id) REFERENCES tenant.tenants(id),
    CONSTRAINT ck_notification_email_outbox_status     CHECK (status IN ('pending','sending','sent','failed'))
);

COMMENT ON TABLE  notification.email_outbox                 IS 'Transactional email outbox. Append-only-style: rows are created in the business transaction; status, attempts, sent_at are updated by the dispatcher.';
COMMENT ON COLUMN notification.email_outbox.tenant_id       IS 'Tenant the email relates to. NULL for platform-level emails (e.g. tenant welcome).';
COMMENT ON COLUMN notification.email_outbox.status          IS 'pending = awaiting dispatch; sending = claimed by dispatcher; sent = delivered to SMTP; failed = exceeded max_attempts.';
COMMENT ON COLUMN notification.email_outbox.subject         IS 'Rendered subject (template + placeholders applied at enqueue time).';
COMMENT ON COLUMN notification.email_outbox.body_text       IS 'Rendered plain-text body.';
COMMENT ON COLUMN notification.email_outbox.body_html       IS 'Rendered HTML body (optional).';
COMMENT ON COLUMN notification.email_outbox.next_attempt_at IS 'When the dispatcher may next try this row. Increases exponentially after each failure.';

CREATE INDEX IF NOT EXISTS ix_notification_email_outbox_dispatch
    ON notification.email_outbox (next_attempt_at)
    WHERE status = 'pending';

CREATE INDEX IF NOT EXISTS ix_notification_email_outbox_tenant_id
    ON notification.email_outbox (tenant_id);

COMMIT;
