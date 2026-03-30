-- Migration: 20260330_1101_T_service_types_DML.sql
-- Description: Seed data for codebook.service_types.
-- Author: WorkshopAdmin Team
-- Date: 2026-03-30
--
-- This script is idempotent. Safe to run multiple times.

BEGIN;

INSERT INTO codebook.service_types (code, label, sort_order) VALUES
    ('oil_change',          '{"en": "Oil Change",              		"sr": "Zamena ulja"}',              		1),
    ('air_filter',          '{"en": "Air Filter Replacement",   	"sr": "Zamena filtera vazduha"}',  			2),
	('fuel_filter',         '{"en": "Fuel Filter Replacement",  	"sr": "Zamena filtera goriva"}',   			3),
	('cabin_filter',        '{"en": "Cabin Filter Replacement", 	"sr": "Zamena filtera kabine"}',   			4),
	('spark_plugs',         '{"en": "Spark Plugs Replacement",  	"sr": "Zamena svećica"}',         			5),
	('transmission_fluid',  '{"en": "Transmission Fluid Change",	"sr": "Zamena ulja u menjaču"}',  			6),
	('timing_belt_chain',   '{"en": "Timing Belt/Chain Replacement","sr": "Zamena zupčastog kaiša/lanca"}', 	7),	
    ('coolant',             '{"en": "Coolant Change",           	"sr": "Zamena rashladne tečnosti"}',		8),
    ('brake_fluid',         '{"en": "Brake Fluid Change",      		"sr": "Zamena kočione tečnosti"}',  		9),    
    ('brake_pads_front',    '{"en": "Front Brake Pads Replacement", "sr": "Zamena kočionih pločica napred"}',	10),
    ('brake_discs_front',   '{"en": "Front Brake Discs Replacement","sr": "Zamena kočionih diskova napred"}',	11),
	('brake_pads_rear',    	'{"en": "Rear Brake Pads Replacement",  "sr": "Zamena kočionih pločica pozadi"}',	12),
    ('brake_discs_rear',   	'{"en": "Rear Brake Discs Replacement", "sr": "Zamena kočionih diskova pozadi"}',	13)
ON CONFLICT (code) DO NOTHING;

COMMIT;
