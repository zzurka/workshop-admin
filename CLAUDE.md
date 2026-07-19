# WorkshopAdmin

Multi-tenant workshop management application for auto repair shops.

## Repository structure

```
database/           Database migrations, setup scripts, runners
src/backend/
    WorkshopAdmin/  New .NET backend — modular monolith (see docs/architecture/backend-plan.md)
    legacy/         Old .NET backend — reference only, do not modify (targets the dropped auth.users.tenant_id schema)
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
- Cross-tenant integrity is enforced declaratively: tenant-scoped parents carry `UNIQUE (tenant_id, id)` and children reference them via composite FKs `(tenant_id, <parent>_id)` — a row can never point at another tenant's row
- Child tables accessed only through their parent skip `tenant_id` (e.g., `appointment_services`, `supplier_contacts`) — but a child that references more than one tenant-scoped table gets a denormalized `tenant_id` so composite FKs can protect every reference (e.g., `work_order_parts`, `invoice_lines`)
- Row Level Security (defense in depth) is enabled for the app role on all tenant-scoped domain tables; the backend sets `SET LOCAL app.current_tenant_id` per transaction (fail-closed: no context = no rows). `auth`, `tenant`, `codebook`, `migration` schemas are outside RLS

## Database

- PostgreSQL 18
- See `database/CLAUDE.md` for DB-specific conventions
- See `database/DATABASE.md` for migration runner and folder structure

## General conventions

- Soft deletes everywhere: `is_deleted BOOLEAN DEFAULT FALSE`
- Audit columns on all domain tables: `created_at`, `created_by`, `updated_at`, `updated_by`
- UUID v7 primary keys on all domain tables (PostgreSQL 18 built-in `uuidv7()`)
- Multi-language support via JSONB labels: `{"en": "...", "sr": "..."}`
