-- Migration: 20260523_1010_S_rls_policies_auth_DDL.sql
-- Description: Row Level Security for auth.roles, auth.role_permissions, and
--              auth.user_roles. These tables mix platform-shared and
--              tenant-scoped rows (unlike the domain tables covered by
--              20260523_1000), so each gets command-split policies instead
--              of one FOR ALL:
--                - auth.roles: tenant_id IS NULL is a shared, readable
--                  catalog (system roles) but writable only in platform
--                  context; a single FOR ALL policy would apply the same
--                  USING clause to read AND write, which is wrong here since
--                  the two have different eligibility.
--                - auth.role_permissions: no tenant_id column; scoped via
--                  EXISTS against the owning role.
--                - auth.user_roles: tenant_id IS NULL is a platform-scope
--                  role assignment, NOT a shared catalog row — visible and
--                  writable only in platform context (same shape as
--                  notification.email_outbox in 20260523_1000), so a single
--                  FOR ALL policy is enough here.
--
--              Token issuance (building JWT claims, which reads these tables
--              before a JWT exists) runs with
--              SET LOCAL app.is_platform_admin = 'true' in the auth module —
--              one privileged, audited place. See
--              docs/database/plans/3.1-auth-sitnice.md §E.
-- Author: WorkshopAdmin Team
-- Date: 2026-07-19
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

-- ---------------------------------------------------------------------------
-- auth.roles — read: global catalog + own tenant's custom roles; write: own
-- tenant's custom roles only, global roles are platform-only.
-- ---------------------------------------------------------------------------

ALTER TABLE auth.roles ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'roles' AND policyname = 'roles_select'
    ) THEN
        CREATE POLICY roles_select ON auth.roles FOR SELECT TO workshopadmin_app
            USING (
                tenant_id IS NULL
                OR tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                OR current_setting('app.is_platform_admin', true) = 'true'
            );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'roles' AND policyname = 'roles_insert'
    ) THEN
        CREATE POLICY roles_insert ON auth.roles FOR INSERT TO workshopadmin_app
            WITH CHECK (
                tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                OR current_setting('app.is_platform_admin', true) = 'true'
            );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'roles' AND policyname = 'roles_update'
    ) THEN
        CREATE POLICY roles_update ON auth.roles FOR UPDATE TO workshopadmin_app
            USING (
                tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                OR current_setting('app.is_platform_admin', true) = 'true'
            )
            WITH CHECK (
                tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                OR current_setting('app.is_platform_admin', true) = 'true'
            );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'roles' AND policyname = 'roles_delete'
    ) THEN
        CREATE POLICY roles_delete ON auth.roles FOR DELETE TO workshopadmin_app
            USING (
                tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                OR current_setting('app.is_platform_admin', true) = 'true'
            );
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- auth.role_permissions — no tenant_id; scoped via EXISTS against the owning
-- role. Read follows roles_select's visibility; write follows roles' write
-- eligibility (assigning a permission to a role requires write access to
-- that role).
-- ---------------------------------------------------------------------------

ALTER TABLE auth.role_permissions ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'role_permissions' AND policyname = 'role_permissions_select'
    ) THEN
        CREATE POLICY role_permissions_select ON auth.role_permissions FOR SELECT TO workshopadmin_app
            USING (
                EXISTS (
                    SELECT 1 FROM auth.roles r
                    WHERE r.id = role_permissions.role_id
                      AND (
                          r.tenant_id IS NULL
                          OR r.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                          OR current_setting('app.is_platform_admin', true) = 'true'
                      )
                )
            );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'role_permissions' AND policyname = 'role_permissions_insert'
    ) THEN
        CREATE POLICY role_permissions_insert ON auth.role_permissions FOR INSERT TO workshopadmin_app
            WITH CHECK (
                EXISTS (
                    SELECT 1 FROM auth.roles r
                    WHERE r.id = role_permissions.role_id
                      AND (
                          r.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                          OR current_setting('app.is_platform_admin', true) = 'true'
                      )
                )
            );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'role_permissions' AND policyname = 'role_permissions_update'
    ) THEN
        CREATE POLICY role_permissions_update ON auth.role_permissions FOR UPDATE TO workshopadmin_app
            USING (
                EXISTS (
                    SELECT 1 FROM auth.roles r
                    WHERE r.id = role_permissions.role_id
                      AND (
                          r.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                          OR current_setting('app.is_platform_admin', true) = 'true'
                      )
                )
            )
            WITH CHECK (
                EXISTS (
                    SELECT 1 FROM auth.roles r
                    WHERE r.id = role_permissions.role_id
                      AND (
                          r.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                          OR current_setting('app.is_platform_admin', true) = 'true'
                      )
                )
            );
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'role_permissions' AND policyname = 'role_permissions_delete'
    ) THEN
        CREATE POLICY role_permissions_delete ON auth.role_permissions FOR DELETE TO workshopadmin_app
            USING (
                EXISTS (
                    SELECT 1 FROM auth.roles r
                    WHERE r.id = role_permissions.role_id
                      AND (
                          r.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid
                          OR current_setting('app.is_platform_admin', true) = 'true'
                      )
                )
            );
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- auth.user_roles — tenant_id IS NULL is a platform-scope role assignment,
-- not a shared catalog row: visible/writable only in platform context. Same
-- shape as notification.email_outbox in 20260523_1000.
-- ---------------------------------------------------------------------------

ALTER TABLE auth.user_roles ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_policies
        WHERE schemaname = 'auth' AND tablename = 'user_roles' AND policyname = 'tenant_isolation'
    ) THEN
        CREATE POLICY tenant_isolation ON auth.user_roles FOR ALL TO workshopadmin_app
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
