# Plan F1 — Skeleton novog backenda

**Status:** ✅ završeno 2026-07-19 — svi testovi zeleni lokalno (19 unit + 3 arch + 16 integracionih kroz lokalni PG18 režim); Testcontainers putanja se verifikuje na prvom CI runu
**Dopuna 2026-07-19:** odlukom o izmeni D1 (samo EF Core) Dapper je naknadno uklonjen iz F1 koda — obrisan `DapperTypeHandlers`, paket izbačen iz props/csproj; JSONB mapiranje ostaje kroz EF value converter
**Datum plana:** 2026-07-19
**Referenca:** [backend-plan.md](../backend-plan.md) §11 (F1) · povezano: [2.2-rls.md](../../database/plans/2.2-rls.md)

Cilj faze: **prazan modular monolith koji se builduje, testira i drži RLS kontekst** — bez ijednog domenskog endpointa. Sve kasnije faze samo dodaju module po obrascu koji ovde nastaje. Stavke idu redom; svaka ima definiciju „gotovo“ (DoD).

---

## 1. Premeštanje legacy backenda (odluka D7)

- [x] `git mv src/backend/WorkshopAdmin src/backend/legacy`
- [x] Ažurirati korenski `CLAUDE.md` (struktura repoa: `src/backend/legacy` = referenca, `src/backend/WorkshopAdmin` = novi backend) i eventualne druge reference na staru putanju (docs, launch konfiguracije)
- [x] Legacy se **ne dira dalje** — ostaje samo kao referenca za portovanje (sekcija 13 backend plana)

**DoD:** repo bez visećih referenci na staru putanju; legacy i dalje otvara/builduje se kao do sada.

## 2. Solucija i projekti

- [x] `src/backend/WorkshopAdmin/WorkshopAdmin.slnx` + struktura iz backend plana §3
- [x] `Directory.Build.props`: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`
- [x] `Directory.Packages.props`: port verzija iz legacy + dodaci za F1: `Microsoft.EntityFrameworkCore` (tranzitivno kroz Npgsql provider), `EFCore.NamingConventions`, `FluentValidation.AspNetCore` filter deo, test paketi (`xunit`, `Testcontainers.PostgreSql`, `NetArchTest.Rules`, `Microsoft.AspNetCore.Mvc.Testing`). `Konscious.*` se **ne prenosi** (Sodium.Core dolazi tek u F3)
- [x] `.editorconfig` (port iz legacy ako postoji, inače standardni .NET)
- [x] Projekti: `WorkshopAdmin.Api`, `WorkshopAdmin.SharedKernel`, 8 praznih modula (`Modules/WorkshopAdmin.Modules.{Tenants,Auth,Codebook,Customers,Hr,Workshop,Warehouse,Notifications}`), testovi (`WorkshopAdmin.UnitTests`, `WorkshopAdmin.IntegrationTests`, `WorkshopAdmin.ArchitectureTests`)
- [x] Reference: moduli → SharedKernel; Api → svi moduli + SharedKernel (composition root); testovi → ono što testiraju

**DoD:** `dotnet build` zelen; `dotnet test` zelen (0 testova je ok).

## 3. CI (minimalni, odmah posle builda)

- [x] GitHub Actions workflow: restore + build + test na push/PR (ubuntu runner — Testcontainers radi out-of-the-box)
- [x] Postavlja se odmah da svaka sledeća stavka ulazi na zelen pipeline; širi se sam od sebe kako testovi pristižu

**DoD:** workflow zelen na grani.

## 4. IModule + kompozicija hosta

- [x] `IModule` u SharedKernel: `AddModule(IServiceCollection, IConfiguration)` + `MapEndpoints(IEndpointRouteBuilder)`
- [x] Svaki modul dobija svoju `XxxModule` klasu (prazna registracija za sada)
- [x] `Program.cs`: eksplicitna lista modula (bez refleksije/skeniranja — 8 redova, čitljivo, kompajler čuva), route group po modulu (`/api/...`)

**DoD:** host se podiže sa svih 8 registrovanih praznih modula.

## 5. Serilog + OpenAPI

- [x] Serilog port iz legacy: bootstrap logger, `UseSerilog`, request logging; enrichment `UserId`/`TenantId` iz `ICurrentUser` (stub dok Auth ne stigne — F3)
- [x] `Microsoft.AspNetCore.OpenApi` + Scalar UI (port konfiguracije)

**DoD:** strukturirani logovi u konzoli; `/scalar` prikazuje (prazan) API.

## 6. SharedKernel — HTTP osnova

- [x] `Result`/`Error` tipovi + mapiranje na `ProblemDetails` (uspeh/ne-uspeh iz handlera bez izuzetaka za očekivane greške)
- [x] Exception → ProblemDetails middleware (port iz legacy, prerada na `IProblemDetailsService`)
- [x] FluentValidation endpoint filter: automatski 400 + ProblemDetails sa greškama po polju
- [x] Paginacija: `PagedRequest`/`PagedResponse` (port modela)
- [x] `ICurrentUser` interfejs (user id, tenant id, permisije iz claimova) + stub implementacija za F1/F2

**DoD:** unit testovi za Result mapiranje i validacioni filter (test endpoint u IntegrationTests hostu).

## 7. IDbSession — srce RLS discipline

Najvažnija stavka faze (backend plan §6, zahtev iz plana 2.2):

- [x] `IDbSession` u SharedKernel: request-scoped, lazy — pri prvom pristupu otvara konekciju (`workshopadmin_app`), `BEGIN`, pa tenant kontekst
- [x] Kontekst preko `set_config('app.current_tenant_id', @tid, true)` odnosno `set_config('app.is_platform_admin', 'true', true)` — parametrizovano, nikad konkatenacija; vrednosti iz `ICurrentUser`
- [x] Endpoint filter/middleware: commit na uspeh, rollback na izuzetak; dispose vraća konekciju
- [x] Zahtevi bez tenant konteksta (pre-login auth tok) prolaze bez `SET LOCAL` — fail-closed ih štiti od domenskih šema

**DoD:** integracioni testovi nad pravom bazom (stavka 9): bez konteksta → 0 redova iz tenant-scoped tabele; sa kontekstom tenanta A → samo A-redovi; platform-admin kontekst → svi redovi. (Seed direktno SQL-om kao admin u testu — domenski moduli još ne postoje.)

## 8. SharedKernel — EF Core osnova

- [x] Bazni entitet(i): audit kolone (`CreatedAt/CreatedBy/UpdatedAt/UpdatedBy`), `IsDeleted`; varijanta za append-only tabele (samo `CreatedAt/CreatedBy`)
- [x] Bazna `DbContext` klasa koja se kači na `IDbSession` konekciju + transakciju (`UseNpgsql(session.Connection)` + `Database.UseTransaction`), snake_case preko `EFCore.NamingConventions`, `ValueGeneratedOnAdd` za DB defaulte (`uuidv7()`, `NOW()`)
- [x] Audit `SaveChanges` interceptor: puni `created_by`/`updated_at`/`updated_by` iz `ICurrentUser`
- [x] Soft-delete: helper za globalni `HasQueryFilter(e => !e.IsDeleted)` po baznom entitetu
- [x] JSONB label tip (`{"en": ..., "sr": ...}`): EF value converter + Dapper `JsonbTypeHandler` (port iz legacy)
- [x] **Bez EF migracija** — DB-first, model se održava ručno

**DoD:** integracioni smoke test: mini test-DbContext nad postojećom tabelom (npr. `codebook` šifarnik) — čitanje kroz `IDbSession`, snake_case mapiranje i JSONB labela rade.

## 9. Testcontainers fixture + C# runner migracija

- [x] Collection fixture: `postgres:18` kontejner, jednom po test run-u
- [x] Init korak = C# reimplementacija setup skripte: kreira `workshopadmin` bazu + `workshopadmin_admin`/`workshopadmin_app` role (kao `database/initial_setup/`)
- [x] C# runner migracija: izvršava sve `database/scripts/*.sql` **po imenu fajla**, kao admin, sa stop-on-failure (bez checksum tracking-a — svaka test baza je sveža)
- [x] Posebna konekcija kao `workshopadmin_app` za RLS testove (napomena iz plana 2.2: admin ne može `SET ROLE` na app)
- [x] `WebApplicationFactory` integracija: API testovi gađaju host povezan na kontejnersku bazu

**DoD:** fixture podigne bazu sa svih ~117 skripti < ~30s; RLS testovi iz stavke 7 zeleni kroz nju.

> **Dopuna (2026-07-19) — lokalni režim bez Dockera:** fixture ima i opt-in lokalni režim: ako postoji `tests/WorkshopAdmin.IntegrationTests/testsettings.local.json` (git-ignored; v. `.example`), umesto kontejnera koristi **namensku lokalnu test bazu** (`workshopadmin_test` na lokalnom PG 18): obriše sve šeme pa izvrši sve migracije. Ime baze mora sadržati „test“ (zaštita od brisanja prave baze). Jednokratni setup: `CREATE DATABASE workshopadmin_test OWNER workshopadmin_admin` kao superuser + popuniti lozinke u json. CI i mašine sa Dockerom i dalje idu kroz Testcontainers (default).

## 10. Domenski događaji

- [x] Bazni tipovi (`IDomainEvent`, handler interfejs) + in-process dispatcher u SharedKernel
- [x] Izvršavanje handlera **u istoj transakciji, pre commita** (outbox preduslov — backend plan §8.3)

**DoD:** unit test dispatch-a; integracioni test da handler deli transakciju (rollback poništi i efekat handlera).

## 11. EF drift test (mehanizam)

- [x] Test koji refleksijom nađe sve registrovane DbContext-e i izvrši trivijalan upit nad svakim `DbSet`-om na sveže migriranoj bazi — hvata raskorak ručnog EF modela i SQL šeme
- [x] U F1 hvata samo test-DbContext iz stavke 8; pravu vrednost dobija od F2 nadalje bez ikakve izmene

**DoD:** test zelen; namerno pokvaren mapping (privremeno, u toku razvoja testa) obara test.

## 12. Arhitekturni testovi

- [x] NetArchTest: modul ne sme referencirati druge module (samo SharedKernel)
- [x] Public tipovi u modulu smeju biti samo u `Contracts/` namespace-u
- [ ] Best-effort regex provera ručnog SQL-a (Dapper stringovi gađaju samo svoju šemu + `codebook`) — **odloženo za F2**, kad prvi ručni SQL nastane

**DoD:** testovi zeleni; namerno dodata među-modulska referenca (privremeno) obara test.

---

## Redosled i zavisnosti

```
1 (git mv) → 2 (solucija) → 3 (CI)
                └→ 4 (IModule) → 5 (Serilog/OpenAPI) → 6 (HTTP osnova)
                                                          └→ 7 (IDbSession) ⇄ 9 (Testcontainers)
                                                                └→ 8 (EF osnova) → 10 (events) → 11 (drift) → 12 (arch)
```

Stavke 7 i 9 se rade zajedno (RLS testovi su DoD za IDbSession, a fixture je preduslov za testove). Stavke 10–12 su međusobno nezavisne.

## Izlaz faze (ukupni DoD)

Prazan monolit: builduje se, CI zelen, host se diže sa 8 modula, Scalar prikazuje API, `IDbSession` drži RLS kontekst dokazano testovima nad pravom migracionom šemom, EF osnova (audit, soft-delete, snake_case, JSONB) pokrivena smoke testovima, events/drift/arch mehanizmi spremni. Time je F2 (Codebook + Tenants) čisto „popunjavanje obrasca“.
