# Analiza modela baze — nalazi i plan rešavanja

**Datum analize:** 2026-07-16
**Obuhvat:** sve tabele u `database/scripts/` (tenant, codebook, auth, customer, hr, workshop, warehouse, notification) upoređene sa funkcionalnim zahtevima po modulima.
**Način rada:** stavke rešavamo jednu po jednu; skripte se smeju menjati u mestu (aplikacija još nije u upotrebi), a dev baza se posle izmene rebuild-uje (drop šema + pun runner, jer runner odbija izmenjene skripte po checksumu).

**Statusi:** ✅ rešeno · 🔶 delimično · 📝 plan spreman · ⬜ otvoreno

Detaljni planovi po stavkama žive u [plans/](plans/) — jedan fajl po stavci; pravimo plan, potvrdimo odluke, pa implementiramo.

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

### 1.2 Reklamacije — 📝

**Problem:** Zahtev iz Workshop modula; ne postoji nijedna tabela.

**Predlog:** `workshop.complaints` — FK na `invoice_id`/`work_order_id`, `customer_id`, `tenant_id`, opis, status (novi codebook `complaint_statuses`: submitted, in_review, accepted, rejected, resolved), `resolution` tekst, `resolved_by/at`, i `resolution_work_order_id` (rešavanje reklamacije često generiše novi radni nalog).

> **Plan:** [plans/1.2-reklamacije.md](plans/1.2-reklamacije.md) — odluke potvrđene 2026-07-17, spreman za implementaciju. Šifarnik complaint_statuses, tabela complaints sa dvokoračnim tokom (odluka → rešavanje) i garantnim radnim nalogom.

### 1.3 Plate (payroll) — 📝

**Problem:** `hr.employees` ima samo `hourly_rate` — zaposleni tipa "salaried" nema ni iznos plate nigde. Zahtev traži "kompletno vođenje zaposlenih, kao što su plate".

**Predlog:**
- `hr.employee_compensations` — istorija zarada (`amount`, `valid_from`, `valid_to`); zamenjuje/dopunjuje `hourly_rate` (plata se menja kroz vreme, istorija je potrebna za obračun)
- `hr.payroll_runs` + `hr.payslips` — obračunski period, bruto/neto, dodaci/odbici, status isplate

> **Plan:** [plans/1.3-payroll.md](plans/1.3-payroll.md) — odluke potvrđene 2026-07-17, spreman za implementaciju. Compensations istorija (mesečna/satnica, jedna otvorena po zaposlenom), mesečni payroll_runs + payslips; obim v1 = evidencija bez poreskog obračuna; briše se `employees.hourly_rate`.

### 1.4 Redosled zakazivanja — 📝

**Problem:** Zahtev: redosled zakazivanja se poštuje i po njemu se vozila uzimaju u rad; poseban slučaj je dan zakazan mnogo unapred koji se naknadno raspoređuje. `workshop.appointments` ima samo `preferred_date` i `scheduled_at` — nema mehanizma za redosled unutar dana.

**Predlog:**
- `queue_position` (redosled unutar dana po tenantu) — preslaganje bez menjanja vremena; ili svesna odluka da je redosled = `scheduled_at` + `created_at` tie-break (dokumentovati)
- `source` kolona (walk_in / phone / portal) — dva kanala zakazivanja postoje u zahtevima
- `estimated_duration_min` (ili default trajanje na `codebook.service_types`) — bez toga se kapacitet dana ne planira
- Statusi: dodati `in_progress` i `no_show` u `codebook.appointment_statuses`

> **Plan:** [plans/1.4-appointments-queue.md](plans/1.4-appointments-queue.md) — odluke potvrđene 2026-07-17, spreman za implementaciju. Zakazuje se DAN + eksplicitna queue_position (scheduled_at → scheduled_date/time), source, arrived_at, default trajanja na service_types, statusi in_progress/no_show.

### 1.5 Brze popravke (walk-in) vs. radni nalozi — 📝

**Problem:** Faktura može bez appointmenta (`invoices.appointment_id` NULL — dobro), ali `work_orders.appointment_id` je NOT NULL. Brza popravka zato ne može imati radni nalog → nema mehaničara, nema `work_order_parts`, izdavanje sa lagera ostaje nevezano.

**Predlog (odabrati jedno):**
- `work_orders.appointment_id` nullable + direktni `customer_id`/`vehicle_id` na work orderu, ili
- uvek kreirati "implicitni" appointment sa `source = walk_in` (oslanja se na 1.4)

> **Plan:** [plans/1.5-walk-in.md](plans/1.5-walk-in.md) — odluka (opcija B) potvrđena 2026-07-17. Implicitni walk-in appointment; appointment = univerzalni „prijem vozila", nalog ostaje NOT NULL, brza popravka može appointment → invoice bez naloga. Bez novih izmena šeme — sve donosi 1.4.

### 1.6 Poručivanje delova (purchase orders) — 📝

**Problem:** Warehouse zahtev navodi "porucivanje". `work_order_parts.part_status` ima ordered/received, ali ne postoji entitet narudžbenice — "šta je naručeno, od koga, kad stiže" se ne može voditi, a `stock_transactions.receipt` nema na šta da se veže.

**Predlog:** `warehouse.purchase_orders` (supplier, status, ordered_at, expected_at) + `warehouse.purchase_order_lines` (catalog_part, količina, nabavna cena). Vezati `stock_transactions` (receipt) i `work_order_parts` (ordered) na PO liniju.

> **Plan:** [plans/1.6-purchase-orders.md](plans/1.6-purchase-orders.md) — odluke potvrđene 2026-07-17, spreman za implementaciju. Šifarnik statusa, PO brojač po (tenant, godina), purchase_orders + lines (delimični prijemi, veza ka work_order_parts), stock_transactions dobija purchase_order_line_id.

### 1.7 Faktura — zakonski i računovodstveni elementi — ✅ REŠENO (2026-07-18)

> **Plan:** [plans/1.7-fakture.md](plans/1.7-fakture.md) — implementirano 2026-07-18. Šifarnici tax_rates i payment_methods, fiskalni podaci na tenantu (PIB, MB, PDV status, račun), gapless brojač po (tenant, godina), PDV snapshot na linijama, totali i billed_to snapshot na zaglavlju, tabela payments, status partially_paid.

**Verifikovano:** pun rebuild (95 skripti), gapless numeracija (rollback + paralelne sesije), unique/CHECK odbijanja, tax_rate snapshot, payments FK — svi testovi iz plana prolaze.

**Problem:** `workshop.invoices` nema:
- **`invoice_number`** — sekvencijalni broj po tenantu (i godini) je zakonska obaveza u Srbiji; potreban i mehanizam dodele (tabela brojača po tenantu/godini — ne oslanjati se na UUID)
- **PDV**: ni `tax_rate` na linijama ni `subtotal` / `tax_amount` / `total` na fakturi
- **valutu**: tenant ima default, ali faktura mora snapshot-ovati `currency_id` (promena defaulta ne sme menjati istoriju)
- **plaćanja**: samo `paid_at` — nema delimičnih uplata ni načina plaćanja → `workshop.payments` (invoice_id, amount, method, paid_at); dodati i `due_date`

---

## 2. Sistemski rizik

### 2.1 Cross-tenant integritet — ✅ REŠENO (2026-07-18)

> **Plan:** [plans/2.1-cross-tenant-integritet.md](plans/2.1-cross-tenant-integritet.md) — implementirano 2026-07-18. 9 roditelja dobilo `UNIQUE (tenant_id, id)`, 14 dece prešlo na kompozitne FK-ove (uključujući `payments → invoices`, dopuna van prvobitne tabele plana — tabela je nastala u 1.7 posle pisanja plana), `work_order_parts` i `invoice_lines` dobili denormalizovan `tenant_id`, vehicles trigger obrisan (zamenjen kompozitnim FK-om). CLAUDE.md pravilo dopunjeno.

**Verifikovano:** pun rebuild (98 skripti); 10 negativnih testova (svaka cross-tenant veza odbijena, uključujući stari trigger slučaj za vehicles) + pozitivan kompletan tok oba tenanta.

**Prvobitni problem:** Trigger `tr_vehicles_tenant_check` je štitio samo vehicles↔customers; svuda drugde je appointment mogao referencirati customer/vehicle drugog tenanta, work_order tuđ appointment ili employee, itd. Kompozitni FK-ovi `(user_id, tenant_id) → auth.user_tenants` za `customers`/`employees`/`user_roles` su bili urađeni ranije (2026-07-16, stavka 1.1).

### 2.2 Row Level Security — 🔶 (DB strana rešena 2026-07-18; backend deo uz novi backend)

> **Plan:** [plans/2.2-rls.md](plans/2.2-rls.md) — odluka DA potvrđena 2026-07-17; DB strana implementirana 2026-07-18: skripta `20260523_1000_S_rls_policies_DDL.sql` — ENABLE RLS + `tenant_isolation` policy za `workshopadmin_app` na 22 tabele (19 direktnih po `tenant_id` uključujući `document.attachments`, 2 preko EXISTS ka roditelju: `appointment_services`, `supplier_contacts`, i `email_outbox` sa NULL-tenant redovima samo za platformu). Fail-closed; admin/migracije nezahvaćene.

**Verifikovano:** testovi kroz pravu konekciju kao `workshopadmin_app` — bez konteksta 0 redova, izolacija po tenantu (uklj. EXISTS child tabele i outbox), write-side odbijanje cross-tenant INSERT-a, platform-admin kontekst vidi sve, admin bypass.

**Ostaje (uz novi backend):** `SET LOCAL app.current_tenant_id` interceptor u DB sloju — fail-closed dizajn ga čini tvrdim zahtevom od prvog dana.

---

## 3. Manji nalazi

### 3.1 Auth — 📝

> **Plan:** [plans/3.1-auth-sitnice.md](plans/3.1-auth-sitnice.md) — odluke potvrđene 2026-07-17, spreman za implementaciju; dopuna E potvrđena 2026-07-18. Partial unique na LOWER(email), email verifikacija (kolona + token tabela + template), lockout kolone, login_history za nepostojeće emaile, MFA checkovi. **+ E:** RLS za `roles`/`role_permissions`/`user_roles` (custom role tenanta su tenant-scoped podaci — razdvojene policy po komandi; globalne role čitljive kao katalog, pisive samo platformi; transakcija izdavanja tokena u backendu radi sa platform kontekstom).
- `uq_auth_users_email` nije partial index — soft-obrisan user zauvek zaključava email; `roles` to rešavaju sa `WHERE NOT is_deleted` (nekonzistentno)
- Nema `email_verified_at` — nužno za samoregistraciju mušterija
- Nema lockout kolona (`failed_login_count`, `locked_until`) iako `login_history.failure_reason` pominje `account_locked`
- `login_history.user_id NOT NULL` — ne može se zabeležiti pokušaj sa nepostojećim emailom; predlog: nullable + `attempted_email`
- `mfa_method` nema CHECK (totp/sms/email); `users.email` VARCHAR(255) vs `external_logins.email` VARCHAR(320) — ujednačiti

### 3.2 Workshop / Warehouse — 📝

> **Plan (ostatak sekcije):** [plans/3.2b-workshop-warehouse-sitnice.md](plans/3.2b-workshop-warehouse-sitnice.md) — odluke potvrđene 2026-07-17, spreman za implementaciju. Quantity → NUMERIC, index na tablicu, awaiting_approval status + approval kolone na nalogu, rezervacije kao view v_stock_availability, part_status `issued`.
- `work_order_parts.quantity` SMALLINT, a stock i invoice linije NUMERIC(10,2) — ne može 1,5 l ulja na nalog; ujednačiti na NUMERIC
- Nedostaje index na `vehicles.license_plate` — a denormalizacija `tenant_id` je obrazložena baš plate/VIN pretragom (VIN ima index, tablica nema)
- Nema strukture za sate rada po nalogu (`work_order_labor`: employee, sati, satnica) — labor linije na fakturi nemaju izvor; `hr.time_entries` nije vezan za naloge
  > **Plan:** [plans/3.2-work-order-labor.md](plans/3.2-work-order-labor.md) — odluke potvrđene 2026-07-17, spreman za implementaciju. Tabela work_order_labor (sati × naplatna satnica, snapshot), invoice_lines.work_order_labor_id, default_labor_rate na tenantu.
- Nema odobrenja mušterije za dodatne popravke (`approved_at`/`approved_by` na work orderu) — scenario iz zahteva
- Stock nema koncept rezervacije (deo "in_stock" na nalogu ne umanjuje raspoloživo)

### 3.3 Customer — 📝

> **Plan:** [plans/3.3-b2b-kupci.md](plans/3.3-b2b-kupci.md) — odluke potvrđene 2026-07-17, spreman za implementaciju. Customer_type diskriminator (person/company), company_name + PIB/MB, CHECK identiteta; otključava billed_to_tax_id iz 1.7.
- `first_name`/`last_name` NOT NULL — nema podrške za pravna lica (naziv firme, PIB/MB); flotni B2B klijenti su česti kod servisa

### 3.4 Tenant / ostalo — 🔶 (attachments rešeni 2026-07-18)

> **Plan:** [plans/3.4-tenant-ostalo.md](plans/3.4-tenant-ostalo.md) — odluke potvrđene 2026-07-17. **Deo B (šema document + attachments) implementiran 2026-07-18** — izvučen ranije zbog čuvanja PDF-a izdate fakture (`invoices.pdf_attachment_id`); RLS (2.2) će novu šemu pokriti u istom prolazu. Ostaje: tenant_subscriptions istorija (jedna tekuća po tenantu) i email_outbox template_code + related_entity.
- Nema istorije pretplate (`tenant_subscriptions`: plan, period, trial, status) — promene plana i naplata platforme se ne mogu pratiti
- `subscription_plans.max_storage_mb` implicira attachmente, a tabela za dokumente/slike ne postoji
- `email_outbox` nema vezu ka izvornom entitetu (`related_entity_type/id`) ni `template_code` — otežava dijagnostiku i idempotentnost slanja

---

## 4. Backend

### 4.1 Novi backend od nule — modular monolith — 📝

> **Plan:** [../architecture/backend-plan.md](../architecture/backend-plan.md) — detaljan arhitekturni plan (2026-07-17), odluke potvrđene 2026-07-18 (EF Core + Dapper, libsodium hasher). Spreman za implementaciju — F1 može odmah, ostatak kad šema uđe u batch.

**Odluka (2026-07-16):** postojeći backend u `src/backend/` se NE refaktoriše — piše se novi od nule, sa arhitekturom modular monolith. Stari backend referencira uklonjeni `auth.users.tenant_id` i ne radi uz novu šemu; ostaje u repou samo kao referenca dok ga novi ne zameni.

**Zahtevi za novi backend (iz izmena šeme):**
- Login tok: autentifikacija → učitavanje članstava iz `auth.user_tenants` → izbor servisa (ako ih je više) → JWT nosi claim aktivnog tenanta
- `user_roles` upiti imaju tenant dimenziju (`tenant_id` NULL = platform-scope dodela)
- Moduli monolita prirodno prate DB šeme: tenant, auth, codebook, customer, hr, workshop, warehouse, notification

---

## Predlog redosleda rešavanja

Baza (redosled stavki):

1. ~~Multi-tenant identitet mušterije~~ ✅
2. ~~Fakture: broj, PDV, valuta, plaćanja (1.7)~~ ✅
3. ~~Cross-tenant kompozitni FK-ovi (2.1) + RLS DB strana (2.2)~~ ✅ (backend deo 2.2 uz novi backend)
4. Nove tabele: reklamacije (1.2), purchase orders (1.6), payroll (1.3), work_order_labor (3.2)
5. Appointments: queue, source, trajanje, no_show (1.4) + odluka o walk-in nalozima (1.5)
6. Manji nalazi (3.1–3.4)

Backend ide kao zaseban kolosek: novi modular monolith od nule (4.1) — ima smisla krenuti tek kada se šema stabilizuje (bar stavke 2–4), da se novi kod ne piše dva puta.
