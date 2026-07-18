-- Migration: 20260523_1000_S_rls_policies_DDL.sql
-- Description: Enable Row Level Security and create tenant isolation policies
--              on all tenant-scoped domain tables. Defense in depth: composite
--              FKs protect the write side; RLS protects the read side against
--              a forgotten WHERE tenant_id = ... in the backend.
--
--              The policies apply to workshopadmin_app only. The backend sets
--              the tenant context per transaction from the JWT claim:
--                  SET LOCAL app.current_tenant_id = '<uuid>';
--              and for platform-admin operations:
--                  SET LOCAL app.is_platform_admin = 'true';
--              Fail-closed: without context current_setting(..., true) yields
--              NULL and no rows are visible. workshopadmin_admin owns the
--              tables and bypasses RLS (no FORCE) — migrations and manual
--              admin queries are unaffected.
--
--              Out of scope: auth and tenant schemas (login and tenant
--              selection happen BEFORE the context is set; users is a global
--              identity), codebook (shared data), migration.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-18
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

-- ---------------------------------------------------------------------------
-- Standard policy: direct tenant_id match (all tables with a tenant_id column)
-- ---------------------------------------------------------------------------

DO $$
DECLARE
    t TEXT;
    tables TEXT[] := ARRAY[
        'customer.customers',
        'customer.vehicles',
        'hr.employees',
        'hr.time_entries',
        'hr.leave_balances',
        'hr.leave_requests',
        'workshop.suppliers',
        'workshop.appointments',
        'workshop.work_orders',
        'workshop.work_order_parts',
        'workshop.invoices',
        'workshop.invoice_lines',
        'workshop.invoice_counters',
        'workshop.payments',
        'workshop.expenses',
        'warehouse.parts_catalog',
        'warehouse.stock',
        'warehouse.stock_transactions',
        'document.attachments'
    ];
BEGIN
    FOREACH t IN ARRAY tables LOOP
        EXECUTE format('ALTER TABLE %s ENABLE ROW LEVEL SECURITY', t);

        IF NOT EXISTS (
            SELECT 1 FROM pg_policies
            WHERE schemaname = split_part(t, '.', 1)
              AND tablename  = split_part(t, '.', 2)
              AND policyname = 'tenant_isolation'
        ) THEN
            EXECUTE format(
                'CREATE POLICY tenant_isolation ON %s
                     FOR ALL TO workshopadmin_app
                     USING (
                         tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid
                         OR current_setting(''app.is_platform_admin'', true) = ''true''
                     )', t);
        END IF;
    END LOOP;
END $$;

-- ---------------------------------------------------------------------------
-- Child tables without tenant_id: policy via EXISTS on the parent
-- ---------------------------------------------------------------------------

ALTER TABLE workshop.appointment_services ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'workshop' AND tablename = 'appointment_services'
          AND policyname = 'tenant_isolation'
    ) THEN
        CREATE POLICY tenant_isolation ON workshop.appointment_services
            FOR ALL TO workshopadmin_app
            USING (
                EXISTS (
                    SELECT 1 FROM workshop.appointments a
                    WHERE a.id = appointment_services.appointment_id
                      AND (
                          a.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                          OR current_setting('app.is_platform_admin', true) = 'true'
                      )
                )
            );
    END IF;
END $$;

ALTER TABLE workshop.supplier_contacts ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'workshop' AND tablename = 'supplier_contacts'
          AND policyname = 'tenant_isolation'
    ) THEN
        CREATE POLICY tenant_isolation ON workshop.supplier_contacts
            FOR ALL TO workshopadmin_app
            USING (
                EXISTS (
                    SELECT 1 FROM workshop.suppliers s
                    WHERE s.id = supplier_contacts.supplier_id
                      AND (
                          s.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                          OR current_setting('app.is_platform_admin', true) = 'true'
                      )
                )
            );
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- notification.email_outbox: nullable tenant_id — platform-level rows
-- (tenant_id IS NULL) are visible only to the platform-admin context
-- ---------------------------------------------------------------------------

ALTER TABLE notification.email_outbox ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'notification' AND tablename = 'email_outbox'
          AND policyname = 'tenant_isolation'
    ) THEN
        CREATE POLICY tenant_isolation ON notification.email_outbox
            FOR ALL TO workshopadmin_app
            USING (
                current_setting('app.is_platform_admin', true) = 'true'
                OR (
                    tenant_id IS NOT NULL
                    AND tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                )
            );
    END IF;
END $$;

COMMIT;
