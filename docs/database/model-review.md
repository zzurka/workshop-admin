# Analiza modela baze — nalazi i plan rešavanja

**Datum analize:** 2026-07-16
**Obuhvat:** sve tabele u `database/scripts/` (tenant, codebook, auth, customer, hr, workshop, warehouse, notification) upoređene sa funkcionalnim zahtevima po modulima.
**Način rada:** stavke rešavamo jednu po jednu; skripte se smeju menjati u mestu (aplikacija još nije u upotrebi), a dev baza se posle izmene rebuild-uje (drop šema + pun runner, jer runner odbija izmenjene skripte po checksumu).

**Statusi:** ✅ rešeno · 🔶 delimično · ⬜ otvoreno

---

## Opšta ocena

Model je zanatski solidan: konvencije se dosledno poštuju (UUID v7, audit kolone, soft delete, JSONB labele, imenovanje constrainta, idempotentne skripte, komentari), codebook pristup je čist, outbox pattern za mejlove je ispravan, refresh token rotacija sa detekcijom krađe je odlična. Nalazi ispod su rupe u odnosu na zahteve i sistemski rizici — ne loš dizajn postojećeg.

---

## 1. Kritično — blokira navedene zahteve

### 1.1 Mušterija u dva servisa (tenanta) — ✅ REŠENO (2026-07-16)

**Problem:** `auth.users.tenant_id` (1:1), globalno UNIQUE `email` i `UNIQUE (user_id)` na `customer.customers` sprečavali su da ista osoba bude mušterija u dva servisa.

**Rešenje (varijanta B — membership tabela):**
- Nova tabela `auth.user_tenants` (M:N članstva, `is_active` za suspenziju pristupa po servisu) — `20260317_1030_T_user_tenants_DDL.sql`
- `auth.users` — uklonjen `tenant_id`; user je globalni identitet
- `auth.user_roles` — surrogate PK, dodat `tenant_id` (NULL = platform-scope dodela, npr. `platform_admin`), kompozitni FK `(user_id, tenant_id) → user_tenants`, partial unique `(user_id, role_id, tenant_id) NULLS NOT DISTINCT WHERE NOT is_deleted`
- `customer.customers` i `hr.employees` — `UNIQUE (tenant_id, user_id)` + kompozitni FK na članstvo
- CLAUDE.md ažuriran (M:N model dokumentovan)

**Verifikovano:** pun rebuild (89 skripti), pozitivni test (ista osoba mušterija u 2 tenanta + rola po tenantu) i 3 negativna testa (FK/unique odbijanja) prolaze.

**Posledica — otvoreno:** backend refaktor (vidi stavku 4.1).

### 1.2 Reklamacije — ⬜

**Problem:** Zahtev iz Workshop modula; ne postoji nijedna tabela.

**Predlog:** `workshop.complaints` — FK na `invoice_id`/`work_order_id`, `customer_id`, `tenant_id`, opis, status (novi codebook `complaint_statuses`: submitted, in_review, accepted, rejected, resolved), `resolution` tekst, `resolved_by/at`, i `resolution_work_order_id` (rešavanje reklamacije često generiše novi radni nalog).

### 1.3 Plate (payroll) — ⬜

**Problem:** `hr.employees` ima samo `hourly_rate` — zaposleni tipa "salaried" nema ni iznos plate nigde. Zahtev traži "kompletno vođenje zaposlenih, kao što su plate".

**Predlog:**
- `hr.employee_compensations` — istorija zarada (`amount`, `valid_from`, `valid_to`); zamenjuje/dopunjuje `hourly_rate` (plata se menja kroz vreme, istorija je potrebna za obračun)
- `hr.payroll_runs` + `hr.payslips` — obračunski period, bruto/neto, dodaci/odbici, status isplate

### 1.4 Redosled zakazivanja — ⬜

**Problem:** Zahtev: redosled zakazivanja se poštuje i po njemu se vozila uzimaju u rad; poseban slučaj je dan zakazan mnogo unapred koji se naknadno raspoređuje. `workshop.appointments` ima samo `preferred_date` i `scheduled_at` — nema mehanizma za redosled unutar dana.

**Predlog:**
- `queue_position` (redosled unutar dana po tenantu) — preslaganje bez menjanja vremena; ili svesna odluka da je redosled = `scheduled_at` + `created_at` tie-break (dokumentovati)
- `source` kolona (walk_in / phone / portal) — dva kanala zakazivanja postoje u zahtevima
- `estimated_duration_min` (ili default trajanje na `codebook.service_types`) — bez toga se kapacitet dana ne planira
- Statusi: dodati `in_progress` i `no_show` u `codebook.appointment_statuses`

### 1.5 Brze popravke (walk-in) vs. radni nalozi — ⬜

**Problem:** Faktura može bez appointmenta (`invoices.appointment_id` NULL — dobro), ali `work_orders.appointment_id` je NOT NULL. Brza popravka zato ne može imati radni nalog → nema mehaničara, nema `work_order_parts`, izdavanje sa lagera ostaje nevezano.

**Predlog (odabrati jedno):**
- `work_orders.appointment_id` nullable + direktni `customer_id`/`vehicle_id` na work orderu, ili
- uvek kreirati "implicitni" appointment sa `source = walk_in` (oslanja se na 1.4)

### 1.6 Poručivanje delova (purchase orders) — ⬜

**Problem:** Warehouse zahtev navodi "porucivanje". `work_order_parts.part_status` ima ordered/received, ali ne postoji entitet narudžbenice — "šta je naručeno, od koga, kad stiže" se ne može voditi, a `stock_transactions.receipt` nema na šta da se veže.

**Predlog:** `warehouse.purchase_orders` (supplier, status, ordered_at, expected_at) + `warehouse.purchase_order_lines` (catalog_part, količina, nabavna cena). Vezati `stock_transactions` (receipt) i `work_order_parts` (ordered) na PO liniju.

### 1.7 Faktura — zakonski i računovodstveni elementi — ⬜

**Problem:** `workshop.invoices` nema:
- **`invoice_number`** — sekvencijalni broj po tenantu (i godini) je zakonska obaveza u Srbiji; potreban i mehanizam dodele (tabela brojača po tenantu/godini — ne oslanjati se na UUID)
- **PDV**: ni `tax_rate` na linijama ni `subtotal` / `tax_amount` / `total` na fakturi
- **valutu**: tenant ima default, ali faktura mora snapshot-ovati `currency_id` (promena defaulta ne sme menjati istoriju)
- **plaćanja**: samo `paid_at` — nema delimičnih uplata ni načina plaćanja → `workshop.payments` (invoice_id, amount, method, paid_at); dodati i `due_date`

---

## 2. Sistemski rizik

### 2.1 Cross-tenant integritet — 🔶 (rešeno samo za auth veze)

**Problem:** Trigger `tr_vehicles_tenant_check` štiti samo vehicles↔customers. Isti problem svuda: appointment može referencirati customer/vehicle drugog tenanta, work_order tuđ appointment ili employee, invoice tuđeg customera, `work_order_parts` tuđ `parts_catalog`/`supplier`, itd.

**Urađeno (2026-07-16):** kompozitni FK-ovi `(user_id, tenant_id) → auth.user_tenants` na `customers`, `employees`, `user_roles`.

**Ostaje:** sistemski pristup za domenske tabele — dodati `UNIQUE (tenant_id, id)` na roditelje i kompozitne FK-ove `(tenant_id, <parent>_id)` na decu (appointments, work_orders, invoices, work_order_parts, stock, stock_transactions, expenses, leave_*, time_entries...). Trigger za vehicles tada može da se zameni kompozitnim FK-om.

### 2.2 Row Level Security — ⬜ (odluka)

**Problem:** Row-based multi-tenancy se oslanja isključivo na aplikativno filtriranje — jedan zaboravljen `WHERE tenant_id = ...` u backendu je direktan data leak.

**Predlog:** razmotriti PostgreSQL RLS sa `app.current_tenant_id` session settingom kao odbranu u dubinu. Odluka (da/ne + obim) pre nego što backend naraste.

---

## 3. Manji nalazi

### 3.1 Auth — ⬜
- `uq_auth_users_email` nije partial index — soft-obrisan user zauvek zaključava email; `roles` to rešavaju sa `WHERE NOT is_deleted` (nekonzistentno)
- Nema `email_verified_at` — nužno za samoregistraciju mušterija
- Nema lockout kolona (`failed_login_count`, `locked_until`) iako `login_history.failure_reason` pominje `account_locked`
- `login_history.user_id NOT NULL` — ne može se zabeležiti pokušaj sa nepostojećim emailom; predlog: nullable + `attempted_email`
- `mfa_method` nema CHECK (totp/sms/email); `users.email` VARCHAR(255) vs `external_logins.email` VARCHAR(320) — ujednačiti

### 3.2 Workshop / Warehouse — ⬜
- `work_order_parts.quantity` SMALLINT, a stock i invoice linije NUMERIC(10,2) — ne može 1,5 l ulja na nalog; ujednačiti na NUMERIC
- Nedostaje index na `vehicles.license_plate` — a denormalizacija `tenant_id` je obrazložena baš plate/VIN pretragom (VIN ima index, tablica nema)
- Nema strukture za sate rada po nalogu (`work_order_labor`: employee, sati, satnica) — labor linije na fakturi nemaju izvor; `hr.time_entries` nije vezan za naloge
- Nema odobrenja mušterije za dodatne popravke (`approved_at`/`approved_by` na work orderu) — scenario iz zahteva
- Stock nema koncept rezervacije (deo "in_stock" na nalogu ne umanjuje raspoloživo)

### 3.3 Customer — ⬜
- `first_name`/`last_name` NOT NULL — nema podrške za pravna lica (naziv firme, PIB/MB); flotni B2B klijenti su česti kod servisa

### 3.4 Tenant / ostalo — ⬜
- Nema istorije pretplate (`tenant_subscriptions`: plan, period, trial, status) — promene plana i naplata platforme se ne mogu pratiti
- `subscription_plans.max_storage_mb` implicira attachmente, a tabela za dokumente/slike ne postoji
- `email_outbox` nema vezu ka izvornom entitetu (`related_entity_type/id`) ni `template_code` — otežava dijagnostiku i idempotentnost slanja

---

## 4. Backend

### 4.1 Novi backend od nule — modular monolith — ⬜

**Odluka (2026-07-16):** postojeći backend u `src/backend/` se NE refaktoriše — piše se novi od nule, sa arhitekturom modular monolith. Stari backend referencira uklonjeni `auth.users.tenant_id` i ne radi uz novu šemu; ostaje u repou samo kao referenca dok ga novi ne zameni.

**Zahtevi za novi backend (iz izmena šeme):**
- Login tok: autentifikacija → učitavanje članstava iz `auth.user_tenants` → izbor servisa (ako ih je više) → JWT nosi claim aktivnog tenanta
- `user_roles` upiti imaju tenant dimenziju (`tenant_id` NULL = platform-scope dodela)
- Moduli monolita prirodno prate DB šeme: tenant, auth, codebook, customer, hr, workshop, warehouse, notification

---

## Predlog redosleda rešavanja

Baza (redosled stavki):

1. ~~Multi-tenant identitet mušterije~~ ✅
2. Fakture: broj, PDV, valuta, plaćanja (1.7) — zakonska strana
3. Cross-tenant kompozitni FK-ovi (2.1) + odluka o RLS (2.2)
4. Nove tabele: reklamacije (1.2), purchase orders (1.6), payroll (1.3), work_order_labor (3.2)
5. Appointments: queue, source, trajanje, no_show (1.4) + odluka o walk-in nalozima (1.5)
6. Manji nalazi (3.1–3.4)

Backend ide kao zaseban kolosek: novi modular monolith od nule (4.1) — ima smisla krenuti tek kada se šema stabilizuje (bar stavke 2–4), da se novi kod ne piše dva puta.
