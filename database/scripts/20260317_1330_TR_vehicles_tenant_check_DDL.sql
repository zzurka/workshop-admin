-- Migration: 20260317_1330_TR_vehicles_tenant_check_DDL.sql
-- Description: Trigger function + trigger to enforce that vehicles.tenant_id always
--              matches the tenant_id of the referenced customer.customers row.
--              A CHECK constraint cannot span tables, so a BEFORE INSERT OR UPDATE
--              trigger is used instead.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE OR REPLACE FUNCTION customer.fn_vehicles_tenant_check()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_customer_tenant_id UUID;
BEGIN
    SELECT tenant_id
      INTO v_customer_tenant_id
      FROM customer.customers
     WHERE id = NEW.customer_id;

    IF v_customer_tenant_id IS NULL THEN
        RAISE EXCEPTION
            'vehicles_tenant_check: customer_id % does not exist in customer.customers.',
            NEW.customer_id;
    END IF;

    IF NEW.tenant_id <> v_customer_tenant_id THEN
        RAISE EXCEPTION
            'vehicles_tenant_check: vehicles.tenant_id (%) must match customer.tenant_id (%) for customer_id %.',
            NEW.tenant_id, v_customer_tenant_id, NEW.customer_id;
    END IF;

    RETURN NEW;
END;
$$;

COMMENT ON FUNCTION customer.fn_vehicles_tenant_check() IS
    'Enforces that vehicles.tenant_id equals the tenant_id of the owning customer. Called by tr_vehicles_tenant_check.';

DROP TRIGGER IF EXISTS tr_vehicles_tenant_check ON customer.vehicles;

CREATE TRIGGER tr_vehicles_tenant_check
    BEFORE INSERT OR UPDATE ON customer.vehicles
    FOR EACH ROW
    EXECUTE FUNCTION customer.fn_vehicles_tenant_check();

COMMENT ON TRIGGER tr_vehicles_tenant_check ON customer.vehicles IS
    'Rejects any INSERT or UPDATE where vehicles.tenant_id does not match customer.customers.tenant_id for the referenced customer_id.';

COMMIT;
