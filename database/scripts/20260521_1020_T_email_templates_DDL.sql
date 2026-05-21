-- Migration: 20260521_1020_T_email_templates_DDL.sql
-- Description: Email templates keyed by stable code. Subject/body stored as
--              JSONB per locale ({"en":"...","sr":"..."}). Templates support
--              simple {{Placeholder}} substitution; the rendered output is
--              stored on the outbox row at enqueue time.
-- Author: WorkshopAdmin Team
-- Date: 2026-05-21
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE TABLE IF NOT EXISTS notification.email_templates (
    id          UUID         NOT NULL DEFAULT uuidv7(),
    code        VARCHAR(50)  NOT NULL,
    subject     JSONB        NOT NULL,
    body_text   JSONB        NOT NULL,
    body_html   JSONB,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    is_deleted  BOOLEAN      NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_by  UUID,
    updated_at  TIMESTAMPTZ,
    updated_by  UUID,

    CONSTRAINT pk_notification_email_templates       PRIMARY KEY (id),
    CONSTRAINT uq_notification_email_templates_code  UNIQUE (code)
);

COMMENT ON TABLE  notification.email_templates           IS 'Transactional email templates. Multi-language: subject and body stored as JSONB keyed by locale.';
COMMENT ON COLUMN notification.email_templates.code      IS 'Stable machine-readable identifier (e.g. ''welcome'', ''password_reset'').';
COMMENT ON COLUMN notification.email_templates.subject   IS 'Per-locale subject, e.g. {"en":"Welcome","sr":"Dobrodošli"}. Supports {{Placeholder}} substitution.';
COMMENT ON COLUMN notification.email_templates.body_text IS 'Per-locale plain-text body. Required (fallback when HTML not provided).';
COMMENT ON COLUMN notification.email_templates.body_html IS 'Optional per-locale HTML body.';

CREATE INDEX IF NOT EXISTS ix_notification_email_templates_is_deleted
    ON notification.email_templates (is_deleted);

COMMIT;
