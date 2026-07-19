# Plan — Novi backend: modular monolith

**Status:** 🔶 u implementaciji — F1 (skeleton) ✅ i F2 (Codebook + Tenants) ✅ završeni 2026-07-19; sledeće: F3 (Notifications + Auth)
**Datum plana:** 2026-07-17 · **Odluke potvrđene:** 2026-07-18 (hasher — libsodium) · **D1 izmenjen 2026-07-19:** samo EF Core (Dapper izbačen)
**Referenca:** [model-review.md](../database/model-review.md) §4.1 · povezano: [2.2-rls.md](../database/plans/2.2-rls.md), [2.1-cross-tenant-integritet.md](../database/plans/2.1-cross-tenant-integritet.md)

---

## 1. Ciljevi i ograničenja

- **Novi backend od nule** — postojeći `src/backend/WorkshopAdmin` je legacy referenca (piše uz staru šemu sa `auth.users.tenant_id`); ne refaktoriše se, već se zamenjuje.
- **Modular monolith** — jedan proces, jedan deployment, ali moduli sa jasnim granicama koje kompajler i testovi čuvaju. Ako neki modul jednog dana treba da se izdvoji u servis, granica već postoji.
- **Moduli prate DB šeme** (zahtev iz model-review §4.1): tenant, auth, codebook, customer, hr, workshop, warehouse, notification.
- **Baza je izvor istine za šemu** — ručno pisane SQL migracije + postojeći runner ostaju; backend se prilagođava bazi (DB-first), ne obrnuto.
- **RLS od prvog dana** — DB strana ide u implementacioni batch (plan 2.2); novi backend mora postavljati tenant kontekst po transakciji ili ne vidi nijedan red (fail-closed).
- Backend implementacija kreće **kada se šema stabilizuje** (bar stavke 2–4 iz redosleda u model-review); skeleton (faza F1) može ranije jer ne zavisi od domenskih tabela.

## 2. Arhitektura na visokom nivou

Jedan ASP.NET Core host (`WorkshopAdmin.Api`) + osam modula kao class library projekata + zajedničko jezgro (`SharedKernel`). Svaki modul se sam registruje (DI + endpoints) preko `IModule` interfejsa.

| Modul | DB šema | Sadržaj (postojeće + planirano) | Kompleksnost |
|---|---|---|---|
| **Tenants** | `tenant` | tenants, subscription_plans, tenant_subscriptions (3.4) | mala |
| **Auth** | `auth` | users, user_tenants, roles, permissions, user_roles, refresh_tokens, external_logins, password_reset_tokens, login_history, MFA | velika (tehnička) |
| **Codebook** | `codebook` | svi šifarnici, keširanje, admin CRUD | mala |
| **Customers** | `customer` | customers (B2B — 3.3), vehicles | mala/srednja |
| **Hr** | `hr` | employees, compensations + payroll (1.3), time_entries, leave_* | srednja |
| **Workshop** | `workshop` | appointments + queue (1.4/1.5), work_orders + labor (3.2), invoices + payments (1.7), complaints (1.2), suppliers, expenses | velika |
| **Warehouse** | `warehouse` | parts_catalog, stock, purchase_orders (1.6), stock_transactions | srednja |
| **Notifications** | `notification` | email_outbox, email_templates, SMTP dispatcher (port iz starog backenda) | mala |

Budući modul: **Documents** (nova `document` šema iz plana 3.4 — attachments) — dodaje se kad šema nastane, po istom obrascu.

## 3. Struktura rešenja

```
src/backend/WorkshopAdmin/
├── WorkshopAdmin.slnx
├── Directory.Build.props            (net10.0, nullable, implicit usings)
├── Directory.Packages.props         (central package management — preuzeti iz starog)
├── .editorconfig
├── src/
│   ├── WorkshopAdmin.Api/           # host: Program.cs, composition root, middleware
│   ├── WorkshopAdmin.SharedKernel/  # zajedničko jezgro (v. sekcija 9)
│   └── Modules/
│       ├── WorkshopAdmin.Modules.Tenants/
│       ├── WorkshopAdmin.Modules.Auth/
│       ├── WorkshopAdmin.Modules.Codebook/
│       ├── WorkshopAdmin.Modules.Customers/
│       ├── WorkshopAdmin.Modules.Hr/
│       ├── WorkshopAdmin.Modules.Workshop/
│       ├── WorkshopAdmin.Modules.Warehouse/
│       └── WorkshopAdmin.Modules.Notifications/
└── tests/
    ├── WorkshopAdmin.UnitTests/          # handleri, domenska logika (po modulskim folderima)
    ├── WorkshopAdmin.IntegrationTests/   # Testcontainers PG18 + prave migracije + API testovi
    └── WorkshopAdmin.ArchitectureTests/  # pravila granica modula
```

**Sprovođenje granica — kombinacija dva mehanizma:**

1. **Compiler** *(precizirano 2026-07-19, F2)*: svaki modul je zaseban projekat i sve u njemu je `internal`; javna contract površina živi u **zasebnom projektu `WorkshopAdmin.Modules.X.Contracts`** (kreira se lenjo — tek kad prvi potrošač nastane). Modul sme da referencira samo `SharedKernel` i **tuđe Contracts projekte**, nikad drugi modul direktno — potrošač fizički ne vidi tuđe internale, a uzajamna potrošnja contract-a (npr. Workshop ⇄ Warehouse u F6/F7) ne pravi ciklus jer Contracts projekti ne referenciraju module. Cross-module pozivi idu preko contract interfejsa registrovanih u DI (implementacija je internal u modulu-vlasniku, host sve povezuje).
2. **Arhitekturni testovi** (NetArchTest): modul sme da zavisi samo od SharedKernel; eventualni ručni SQL (EF `SqlQuery`/`FromSql`) u modulu sme da gađa samo svoju šemu + `codebook` (regex provera je best-effort — primarna odbrana je code review + RLS). EF stranu granice čuva sam modulski DbContext — mapira isključivo svoju šemu.

## 4. Arhitektura unutar modula

**Preporuka: vertical slice kao podrazumevani stil u svim modulima, sa „postepenim“ domenskim slojem tamo gde kompleksnost to traži** — umesto binarnog izbora VSA vs. clean architecture po modulu. Razlozi:

- Jedan stil svuda = manje kognitivno opterećenje; razlika između „prostog“ i „složenog“ modula je samo prisustvo `Domain/` foldera, ne druga arhitektura.
- Clean architecture slojevi (Application/Infrastructure/Domain projekti po modulu) bi za 8 modula dali 30+ projekata — preskupo za solo razvoj bez srazmerne dobiti.
- VSA se prirodno mapira na postojeći stil starog backenda (Features folderi već postoje u Application projektu).

Struktura modula:

```
WorkshopAdmin.Modules.Workshop.Contracts/   # PUBLIC — jedini izlaz modula (zaseban projekat, v. §3.1)
├── IWorkOrderLookup.cs         #   interfejsi za druge module
└── WorkOrderRef.cs             #   DTO-ovi koje ti interfejsi vraćaju

WorkshopAdmin.Modules.Workshop/
├── Features/                   # internal — vertical slices, po funkcionalnim oblastima
│   ├── Appointments/
│   │   ├── ScheduleAppointment/   # Endpoint + Request/Response + Validator + Handler
│   │   ├── ReorderQueue/
│   │   └── ...
│   ├── WorkOrders/...
│   ├── Invoicing/...
│   └── Complaints/...
├── Domain/                     # internal — SAMO gde postoje netrivijalne invarijante
│   ├── WorkOrder.cs            #   npr. dozvoljeni prelazi statusa naloga
│   └── InvoiceTotals.cs        #   npr. obračun PDV totala
├── Infrastructure/             # internal — deljeni SQL/repoi za više slice-ova, event handleri
└── WorkshopModule.cs           # IModule: DI registracija + MapEndpoints + event subscribe
```

**Jedan slice = jedan use case:** endpoint (minimal API), request/response DTO, FluentValidation validator, handler klasa. Handler radi nad modulskim `DbContext`-om (LINQ); za složenije upite i izveštaje EF `SqlQuery<T>`/`FromSql` sa čistim SQL-om preko iste konekcije. Logika živi u handleru; u lokalni `Infrastructure/` repo se izvlači tek kad je dele 2+ slice-a. Bez mediatora — endpoint direktno poziva handler iz DI (v. odluku D3).

**Gde očekujemo `Domain/`:** Workshop (prelazi statusa naloga/reklamacija, obračun fakture, odobrenja), Warehouse (pravila izdavanja/prijema, raspoloživost), Auth (tok tokena je u servisima, ali pravila lockout/rotacije mogu u domen). Tenants, Codebook, Customers, Notifications, Hr (v1) su transaction-script slice-ovi bez domena.

## 5. Tehnološki izbor

| Oblast | Izbor | Napomena |
|---|---|---|
| Runtime | .NET 10 (LTS) | kao stari backend; Directory.Packages.props se prenosi |
| HTTP | **Minimal APIs** + route groups po modulu | idiomatski za VSA; stari je imao controllere (D2) |
| Pristup podacima | **EF Core (Npgsql)** | DbContext po modulu; za složene upite EF `SqlQuery`/`FromSql` — v. odluku D1 (izmenjena 2026-07-19: bez Dappera) |
| Validacija | FluentValidation preko endpoint filtera | automatski 400 + ProblemDetails |
| Mediator | **nema** | v. odluku D3 |
| Auth | JWT Bearer + Argon2id (libsodium) + refresh rotacija + OIDC | hasher nov — `Sodium.Core`; ostalo port iz starog backenda |
| Autorizacija | permission-based policy provider | port (`HasPermission`), permisije kao claims u access tokenu |
| Logging | Serilog (bootstrap + request logging, UserId/TenantId enrichment) | port konfiguracije |
| OpenAPI | Microsoft.AspNetCore.OpenApi + Scalar | kao stari |
| Email | MailKit + outbox dispatcher hosted service | port u Notifications modul |
| Testovi | xUnit + Testcontainers (postgres:18) + NetArchTest | novo — stari nema testove |

## 6. DB sesija, multi-tenancy i RLS

Centralni deo novog backenda — jedno mesto gde se otvara konekcija, transakcija i postavlja tenant kontekst (zahtev iz plana 2.2):

```csharp
// SharedKernel — request-scoped, lazy
public interface IDbSession : IAsyncDisposable
{
    Task<NpgsqlConnection> GetOpenConnectionAsync(CancellationToken ct); // BEGIN + SET LOCAL
    NpgsqlTransaction Transaction { get; }
    Task CommitAsync(CancellationToken ct);
}
```

- Pri prvom pristupu: otvori konekciju (`workshopadmin_app`), `BEGIN`, pa `SET LOCAL app.current_tenant_id = '<uuid>'` iz claima aktivnog tenanta, odnosno `SET LOCAL app.is_platform_admin = 'true'` za platform-scope zahteve. Parametri idu kroz `set_config(...)`, ne konkatenacijom.
- **EF Core se kači na istu sesiju:** svaki modul ima svoj `DbContext` (mapira isključivo svoju šemu + read-only šifarnike); pri kreiranju dobija konekciju i transakciju iz `IDbSession` (`UseNpgsql(session.Connection)` + `Database.UseTransaction(...)`). Eventualni čisti SQL (`SqlQuery`/`FromSql`) ide kroz isti DbContext — RLS kontekst važi uvek.
- **Jedna transakcija po HTTP zahtevu**, deljena kroz sve module (i sve DbContext-e) koje zahtev dotakne (cross-module konzistentnost „besplatno“ — v. D6). Commit na uspeh (endpoint filter/middleware), rollback na izuzetak.
- Auth slice-ovi pre logina (login, refresh, password reset) rade bez tenant konteksta — `auth`/`tenant` šeme su najvećim delom van RLS obuhvata, pa fail-closed ne smeta. Izuzetak (dopuna 2026-07-18, detalji u [plan 3.1 §E](../database/plans/3.1-auth-sitnice.md)): `roles`/`role_permissions`/`user_roles` dobijaju RLS zbog custom rola tenanta — transakcija izdavanja tokena (građenje claim-ova) se zato izvršava sa `SET LOCAL app.is_platform_admin = 'true'` kao jedno privilegovano, revidirano mesto u auth modulu.
- **DB-first mapiranje:** SQL skripte ostaju izvor istine; EF model se piše/održava ručno (inicijalni scaffold sme da posluži kao polazna tačka), **bez EF migracija**. snake_case preko `EFCore.NamingConventions`; JSONB labele preko value convertera iz SharedKernel; `uuidv7()`/`NOW()` defaulti su DB-generated (`ValueGeneratedOnAdd`).
- **Audit kolone:** `SaveChanges` interceptor puni `created_by`/`updated_at`/`updated_by` iz `ICurrentUser` — jedno mesto umesto discipline po upitu. Eventualni ručni SQL ih postavlja eksplicitno.
- **Soft delete:** globalni query filter `HasQueryFilter(e => !e.IsDeleted)` na svim entitetima — EF strana pokrivena automatski; eventualni ručni SQL i dalje nosi `AND NOT is_deleted` (stavka code-review checkliste + testovi).

## 7. Auth modul — tok prijave (membership model)

Zahtev iz model-review §4.1, srce novog backenda:

1. `POST /api/auth/login` — kredencijali → Argon2 verifikacija → učitavanje **aktivnih članstava** iz `auth.user_tenants`:
   - **platform admin** (nema članstva, ima platform-scope rolu, `user_roles.tenant_id IS NULL`) → tokeni sa `is_platform_admin` claimom, bez `tenant_id`;
   - **1 članstvo** → odmah puni tokeni sa `tenant_id` claimom;
   - **više članstava** → odgovor sa listom servisa + kratkotrajni **selection token** (poseban scope, ne važi ni za šta drugo).
2. `POST /api/auth/select-tenant` — selection token + izabrani `tenant_id` → provera članstva → puni tokeni.
3. `POST /api/auth/switch-tenant` — važeći access token + drugi `tenant_id` → novi tokeni (promena servisa bez ponovnog logina).
4. Refresh rotacija sa detekcijom krađe, logout, password reset, email verifikacija (3.1), OIDC external login — port postojeće logike, prilagođen membership modelu.

**JWT claims:** `sub`, `email`, `tenant_id`, `is_platform_admin`, role i permisije **za aktivni tenant** (upit nad `user_roles` uvek nosi tenant dimenziju: `tenant_id = @tid OR tenant_id IS NULL`). Permisije u tokenu → autorizacija bez DB round-tripa; access token kratak (~10 min) pa promene permisija brzo legnu.

## 8. Komunikacija između modula

Pravila (čuvaju ih arhitekturni testovi + review):

1. **SQL samo nad svojom šemom.** Izuzetak: `codebook` šema je **shared-read** za sve module (šifarnici se JOIN-uju u upitima — labele, statusi); upis u codebook ide samo kroz Codebook modul. FK-ovi između šema u bazi normalno postoje — granica modula je koncept koda, ne baze.
2. **Sinhrono — contract interfejsi:** modul objavi interfejs u `Contracts/`, drugi ga dobije kroz DI. Poziv je in-process i deli isti `IDbSession` (ista transakcija). Primeri: Warehouse → `ISupplierLookup` (Workshop poseduje suppliers), Workshop → `ICustomerLookup`, `IEmployeeLookup`, Auth → `ITenantLookup`.
3. **Asinhrono — domenski događaji:** in-process dispatcher iz SharedKernel; handleri se izvršavaju **u istoj transakciji, pre commita**. Time je upis u `notification.email_outbox` atomski sa izvornom promenom (outbox pattern), a stvarni SMTP send radi postojeći hosted dispatcher van transakcije. Primer: `WorkOrderCompleted` → Notifications enqueue mejla.
4. **Bez događaja za tok koji mora da vrati rezultat** — to je contract poziv. Događaji su za sporedne efekte.

## 9. SharedKernel — sadržaj

Namerno mali (nije „Common“ đubrište): `IModule`, `IDbSession` + implementacija, `ICurrentUser` (user id, tenant id, permisije iz claimova), zajednička EF Core osnova (bazni entitet sa audit kolonama, snake_case konvencije, audit `SaveChanges` interceptor, soft-delete filter helper, JSONB label value converter), Result/Error tipovi + mapiranje na ProblemDetails, exception → ProblemDetails middleware, FluentValidation endpoint filter, paginacija (`PagedRequest`/`PagedResponse`), JSONB label tip (EF value converter), bazni tipovi domenskih događaja + dispatcher, permission autorizacija (`HasPermission` atribut/ekstenzija, policy provider — port).

## 10. Testiranje

- **Integracioni (težište):** Testcontainers `postgres:18`; fixture izvršava **prave** migracione skripte iz `database/scripts/` (redosled po imenu fajla — mala C# reimplementacija runnera), pa API testovi kroz `WebApplicationFactory`. Obavezni scenariji: **RLS izolacija** (dva tenanta, korisnik A ne vidi redove tenanta B ni jednim endpointom), login tok sa 0/1/N članstava, soft-delete filtriranje, **EF drift test** (upit nad svakim DbSet-om na sveže migriranoj bazi — hvata raskorak ručno održavanog EF modela i SQL šeme).
- **Unit:** handleri sa fake contract-ovima, domenske invarijante (prelazi statusa, obračuni).
- **Arhitekturni:** moduli ne referenciraju druge module; Contracts tipovi su jedini public; imenovanje.
- Svaka faza (sekcija 11) se završava zelenim testovima za svoj obim.

## 11. Faze implementacije

Preduslov: implementacioni batch šeme iz model-review (bar stavke 2–4: fakture 1.7, cross-tenant FK 2.1 + RLS 2.2, nove tabele 1.2/1.6/1.3/3.2, appointments 1.4/1.5). F1 ne zavisi od domenskih tabela i može ranije/paralelno.

| Faza | Obim | Izlaz |
|---|---|---|
| **F1 — Skeleton** ✅ *(završeno 2026-07-19)* ([detaljan plan](plans/f1-skeleton.md)) | Solucija, SharedKernel (IDbSession + RLS `SET LOCAL`, EF osnova — konvencije/interceptori, ProblemDetails, validacioni filter, events), host sa IModule registracijom, Serilog, OpenAPI, arch testovi, Testcontainers fixture sa runnerom migracija + EF drift test, CI | prazan monolit koji se builduje, testira i drži RLS kontekst |
| **F2 — Codebook + Tenants** ✅ *(završeno 2026-07-19)* ([detaljan plan](plans/f2-codebook-tenants.md)) | najprostiji moduli — dokaz obrasca: šifarnici (list + admin CRUD, keš), tenant CRUD, subscription plans/istorija | prvi end-to-end slice-ovi |
| **F3 — Notifications + Auth** | outbox + dispatcher port; ceo auth tok iz sekcije 7 (login/select/switch, rotacija, reset, verifikacija, OIDC, permisije) | prijava sa izborom servisa; mejlovi rade |
| **F4 — Customers** | customers (person/company — B2B 3.3), vehicles, pretraga (tablica/VIN) | evidencija mušterija |
| **F5 — Hr osnovno + Appointments** | employees CRUD + kompenzacije (bez payroll obračuna); appointments: zakazivanje, dnevni queue + preslaganje, source, walk-in prijem (1.4/1.5), statusi, prisustvo vozila + podsetnik za dovoz (v. napomenu ispod) | prijem vozila funkcioniše |
| **F6 — Warehouse** | parts_catalog, stock, purchase orders + prijem (1.6), stock_transactions | magacin i nabavka |
| **F7 — Work orders** | nalozi iz appointmenta, mehaničari, labor (3.2), delovi sa lagera (veza ka Warehouse), odobrenja, complaints (1.2) | radionica funkcioniše |
| **F8 — Invoicing** | fakture (broj, PDV, valuta — 1.7), payments, expenses | naplata |
| **F9 — Hr puno** | time_entries (veza ka nalozima), leave, payroll obračun (1.3) | HR kompletan |
| **F10 — Cutover** | Angular frontend prelazi na novi API; brisanje legacy backenda; ažuriranje CLAUDE.md/DOCS.md; hardening (rate limiting, health checks, CORS) | stari backend obrisan |

Napomena za F5 — prisustvo vozila i podsetnik za dovoz *(dodato 2026-07-19)*:

- **„Vozilo u servisu" nije poseban boolean** — izvedeno je iz `appointments.arrived_at IS NOT NULL`. Portal zakazivanje nikad ne postavlja `arrived_at` (uvek „nije u servisu"); pri ličnom zakazivanju, ako vozilo nije u voznom stanju i odmah ostaje u servisu, UI nudi opciju „vozilo ostaje" koja postavlja `arrived_at = NOW()` već pri kreiranju termina. Dnevni queue prikazuje koja vozila su već tu.
- **Podsetnik za dovoz:** hosted service (Workshop modul) jednom dnevno nalazi termine sa `scheduled_date = danas + tenants.arrival_reminder_lead_days` (konfigurabilno po tenantu; `NULL` = isključeno, default 1 dan), `arrived_at IS NULL` i `arrival_reminder_sent_at IS NULL`, pa u **istoj transakciji** upisuje mejl (`appointment_arrival_reminder` šablon) u `notification.email_outbox` i postavlja `arrival_reminder_sent_at` (at-most-once). DB strana urađena 2026-07-19: migracije `20260719_1300`–`1320`.

Frontend napomena *(izmenjena 2026-07-19)*: postojeći Angular kod se tretira kao prototip — **nema obaveze kompatibilnosti sa starim API-jem**; rute i oblici odgovora novog backenda se dizajniraju slobodno, a frontend se piše/prilagođava na njih u F10.

## 12. Odluke — ✅ potvrđene 2026-07-18

D2–D7 potvrđeni po preporuci; **D1 dvaput izmenjen** — 2026-07-18: EF Core + Dapper; **2026-07-19: samo EF Core** (Dapper izbačen iz steka — frontend/API još ne postoje, pa je pojednostavljenje bilo bezbolno; implementirano u F1 kodu istog dana). Dodatno potvrđeno: password hashing prelazi na **libsodium (Argon2id)** umesto Konscious biblioteke (v. sekciju 13).

| # | Pitanje | Odluka | Alternativa |
|---|---|---|---|
| **D1** | Pristup podacima | **Samo EF Core (Npgsql)** — change tracking, globalni query filteri (soft delete), audit interceptor. Za eventualne složene upite/izveštaje: EF `SqlQuery<T>`/`FromSql` preko iste konekcije i transakcije (isti RLS kontekst). *Izmena 2026-07-19: Dapper izbačen iz steka — jedan način pristupa podacima umesto dva.* | EF + Dapper (odluka od 2026-07-18 — zamenjena); čist Dapper (prvobitna preporuka — odbačeno) |
| **D2** | HTTP sloj | **Minimal APIs** + route groups, endpoint po slice-u | Controllers (kao stari backend) |
| **D3** | Mediator | **Bez mediatora** — endpoint → handler direktno iz DI; cross-cutting kroz endpoint filtere. MediatR je od v13 komercijalan; apstrakcija ne donosi ništa na ovoj veličini | Wolverine / MediatR |
| **D4** | Granice modula | **Projekat po modulu**, `internal` sve osim `Contracts/` | jedan projekat + samo arch testovi (slabija garancija) |
| **D5** | Granularnost Workshop modula | **Jedan modul** (prati šemu), unutra feature oblasti (Appointments/WorkOrders/Invoicing/Complaints); podela tek ako zaboli | odmah izdvojiti Billing modul |
| **D6** | Transakcije preko modula | **Jedna deljena transakcija po zahtevu** (pragmatičan monolit) | stroga transakcija po modulu + eventual consistency (trošak bez potrebe dok je monolit) |
| **D7** | Lokacija koda | `git mv` starog u `src/backend/legacy/`, novi čist na `src/backend/WorkshopAdmin/` | novi pored starog pod drugim imenom (ružno, ostaje zauvek) |

## 13. Šta se prenosi iz starog backenda (port, ne referenca)

Proverena tehnička rešenja koja se kopiraju/prilagođavaju u nove module: JWT token service, refresh rotacija sa detekcijom krađe, permission policy provider/handler + `HasPermission`, email outbox + MailKit dispatcher + template renderer, OIDC external auth klijent (state/handoff cache), exception middleware (prerada na ProblemDetails), Serilog konfiguracija, JSONB label mapiranje (legacy Dapper `JsonbTypeHandler` → EF value converter), paging modeli, `Directory.Packages.props` verzije (uz dodatak `EFCore.NamingConventions`; `Npgsql.EntityFrameworkCore.PostgreSQL` već postoji u props-u).

**Ne prenosi se:** Argon2 hasher (Konscious) — novi backend koristi **libsodium** preko `Sodium.Core` paketa: `crypto_pwhash_str` (Argon2id), standardni PHC format hasha, verifikacija + needs-rehash provera pri loginu. Kompatibilnost sa starim hashovima nije potrebna (nema produkcijskih podataka).
