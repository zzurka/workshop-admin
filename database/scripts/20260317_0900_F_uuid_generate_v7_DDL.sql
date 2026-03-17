-- Migration: 20260317_0900_F_uuid_generate_v7_DDL.sql
-- Description: Create a utility function for generating UUID v7 (time-ordered UUIDs).
--              All domain tables use this as the default for primary key columns.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-17
--
-- UUID v7 encodes the current Unix timestamp (ms) in the first 48 bits, making
-- UUIDs naturally sortable by creation time without a separate created_at index.
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

CREATE OR REPLACE FUNCTION public.uuid_generate_v7()
RETURNS uuid
LANGUAGE sql
AS $$
    SELECT encode(
        set_bit(
            set_bit(
                overlay(uuid_send(gen_random_uuid())
                    placing substring(int8send(floor(extract(epoch from clock_timestamp()) * 1000)::bigint) from 3)
                    from 1 for 6
                ),
                52, 1
            ),
            53, 1
        ),
        'hex'
    )::uuid
$$;

COMMENT ON FUNCTION public.uuid_generate_v7() IS
    'Generates a UUID v7 (time-ordered). First 48 bits encode Unix timestamp in milliseconds, '
    'remaining bits are random. UUIDs are monotonically increasing within the same millisecond.';

COMMIT;
