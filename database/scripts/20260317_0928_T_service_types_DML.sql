-- Migration: 20260317_0928_T_service_types_DML.sql
-- Description: Seed data for codebook.service_types.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.service_types (code, label, default_duration_min, sort_order) VALUES
    ('oil_change',           '{"en": "Oil Change",                   "sr": "Zamena ulja"}',                     30,   1),
    ('air_filter',           '{"en": "Air Filter Replacement",       "sr": "Zamena filtera vazduha"}',          15,   2),
    ('fuel_filter',          '{"en": "Fuel Filter Replacement",      "sr": "Zamena filtera goriva"}',           30,   3),
    ('cabin_filter',         '{"en": "Cabin Filter Replacement",     "sr": "Zamena filtera kabine"}',           15,   4),
    ('spark_plugs',          '{"en": "Spark Plugs Replacement",      "sr": "Zamena svećica"}',                  60,   5),
    ('transmission_fluid',   '{"en": "Transmission Fluid Change",    "sr": "Zamena ulja u menjaču"}',           60,   6),
    ('timing_belt_chain',    '{"en": "Timing Belt/Chain Replacement","sr": "Zamena zupčastog kaiša/lanca"}',    240,  7),
    ('coolant',              '{"en": "Coolant Change",               "sr": "Zamena rashladne tečnosti"}',       45,   8),
    ('brake_fluid',          '{"en": "Brake Fluid Change",           "sr": "Zamena kočione tečnosti"}',         30,   9),
    ('brake_pads_front',     '{"en": "Front Brake Pads Replacement", "sr": "Zamena kočionih pločica napred"}',  60,   10),
    ('brake_discs_front',    '{"en": "Front Brake Discs Replacement","sr": "Zamena kočionih diskova napred"}',  90,   11),
    ('brake_pads_rear',      '{"en": "Rear Brake Pads Replacement",  "sr": "Zamena kočionih pločica pozadi"}',  60,   12),
    ('brake_discs_rear',     '{"en": "Rear Brake Discs Replacement", "sr": "Zamena kočionih diskova pozadi"}',  90,   13)
ON CONFLICT (code) DO NOTHING;

COMMIT;
