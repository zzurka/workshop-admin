# Database conventions

## Script naming

Format: `YYYYMMDD_HHMM_<type>_<name>_<DDL|DML>.sql`

Type prefixes:
- `S_` = schema, `T_` = table, `F_` = function, `TR_` = trigger
- `V_` = view, `IX_` = index, `SP_` = stored procedure, `SQ_` = sequence

DDL (structure) and DML (seed data) are always separate files.

## Script structure

- All scripts wrapped in `BEGIN; ... COMMIT;`
- All scripts must be idempotent (`IF NOT EXISTS`, `ON CONFLICT DO NOTHING`)
- Header comment: migration name, description, author, date

## Table conventions

- UUID v7 primary keys: `id UUID NOT NULL DEFAULT uuidv7()`
- Audit columns: `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`, `created_by UUID`, `updated_at TIMESTAMPTZ`, `updated_by UUID`
- Soft delete: `is_deleted BOOLEAN NOT NULL DEFAULT FALSE`
- Active flag: `is_active BOOLEAN NOT NULL DEFAULT TRUE`
- `updated_at` is NULL on creation, set on any update including soft-delete
- All timestamps use `TIMESTAMPTZ`

## Constraint naming

- PK: `pk_<schema>_<table>`
- FK: `fk_<schema>_<table>_<column>`
- UQ: `uq_<schema>_<table>_<columns>`
- CK: `ck_<schema>_<table>_<description>`

## Index naming

- Format: `ix_<schema>_<table>_<column>`
- Always create indexes on: `tenant_id`, `is_deleted`, FK columns

## Codebook tables

- One table per type in `codebook` schema (not a generic table)
- Structure: `SMALLSERIAL PK`, `code VARCHAR(50) UNIQUE`, `label JSONB`, `sort_order SMALLINT`, `is_active BOOLEAN`
- Labels are JSONB: `{"en": "English", "sr": "Serbian"}`
- `code` is the stable machine-readable key — never changes
- FK columns on domain tables use `_id` suffix (e.g., `fuel_type_id`)

## Schemas

- `migration` — migration history tracking
- `auth` — users, roles, permissions, authentication
- `tenant` — tenant/workshop organizations
- `codebook` — shared lookup/reference tables
- `customer` — customers and vehicles
- `hr` — employees, time tracking, leave management
- `workshop` — appointments, work orders, invoices, expenses, suppliers
- `warehouse` — parts catalog, stock levels, stock transactions

## Roles

- `workshopadmin_admin` — owns all objects (runs migrations)
- `workshopadmin_app` — DML only (used by backend)
- Grants live in schema scripts (`S_` files), NOT in table scripts
- Schema scripts include `GRANT USAGE`, `GRANT ON ALL TABLES/SEQUENCES/FUNCTIONS`, and `ALTER DEFAULT PRIVILEGES`

## Circular FK dependencies

When two tables reference each other (e.g., `auth.users` ↔ `tenant.tenants`), the FK that would create a forward reference is deferred to a separate script that runs after both tables exist. The deferred FK script uses `DO $$ IF NOT EXISTS ... END $$` for idempotency.

## Immutable event log tables

Tables like `login_history` and `stock_transactions` are append-only: they have `created_at` and `created_by` but no `updated_at`, `updated_by`, `is_deleted`, or `is_active`.
