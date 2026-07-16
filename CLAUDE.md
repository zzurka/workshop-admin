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

- User-tenant is M:N: `auth.users` is a global identity (one row per person); tenant access lives in `auth.user_tenants` memberships
- The same person can be a customer (or employee) at multiple tenants — one `customer.customers` / `hr.employees` row per tenant, tied to a membership via composite FK `(user_id, tenant_id) → auth.user_tenants`
- Platform super admins have no memberships; they are recognized by a platform-scope role assignment (`auth.user_roles.tenant_id IS NULL`)
- Role assignments are per tenant: `auth.user_roles.tenant_id` (NULL = platform-scope assignment)
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
