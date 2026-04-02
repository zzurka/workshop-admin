# WorkshopAdmin

Multi-tenant workshop management application for auto repair shops.

## Repository structure

```
database/           Database migrations, setup scripts, runners
src/backend/        .NET backend (WorkshopAdmin)
src/frontend/       Angular frontend (workshop-admin-app)
docs/               Documentation
```

## Branching

- `master` — production branch, target for PRs
- `develop` — active development branch

## Multi-tenancy model

- User-tenant is 1:1: `tenant_id` lives on `auth.users` (nullable)
- `tenant_id = NULL` means platform-level super admin
- Most domain tables have `tenant_id` for direct filtering
- Child tables accessed only through their parent skip `tenant_id` (e.g., `invoice_lines`, `supplier_contacts`)

## Database

- PostgreSQL 18
- See `database/CLAUDE.md` for DB-specific conventions
- See `database/DATABASE.md` for migration runner and folder structure

## General conventions

- Soft deletes everywhere: `is_deleted BOOLEAN DEFAULT FALSE`
- Audit columns on all domain tables: `created_at`, `created_by`, `updated_at`, `updated_by`
- UUID v7 primary keys on all domain tables (PostgreSQL 18 built-in `uuidv7()`)
- Multi-language support via JSONB labels: `{"en": "...", "sr": "..."}`
