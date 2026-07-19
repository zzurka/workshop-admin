# Plan F2 — Codebook + Tenants moduli

**Status:** 📝 plan — spreman za implementaciju
**Datum plana:** 2026-07-19 (revizija istog dana: samo EF Core — bez Dappera; bez obaveze kompatibilnosti sa legacy API-jem)
**Referenca:** [backend-plan.md](../backend-plan.md) §11 (F2) · prethodi: [f1-skeleton.md](f1-skeleton.md)

Cilj faze: **prvi end-to-end slice-ovi — dokaz obrasca** na dva najprostija modula. Sve što ovde nastane (folder struktura slice-a, endpoint + validator + handler, EF kroz `IDbSession`, keš, testovi po slice-u) postaje šablon za F3–F9. Stavke idu redom; svaka ima DoD.

**Kontekst iz šeme:** `codebook` ima 20 tabela identičnog oblika (`id SMALLSERIAL, code, label JSONB, sort_order, is_active`), uz dva odstupanja: `tax_rates` (+`rate NUMERIC`), `service_types` (+`default_duration_min`). `tenant` ima `tenants`, `subscription_plans`, `tenant_subscriptions` (istorija pretplata, 3.4). Obe šeme su **van RLS obuhvata** — rade i bez tenant konteksta, što F2 čini izvodljivim pre Auth modula (F3).

---

## 0. Odluke faze (potvrditi pre početka)

| # | Pitanje | Predlog | Obrazloženje |
|---|---|---|---|
| **O1** | Codebook EF model | **Bazna klasa `CodebookEntry`** (Id/Code/Label/SortOrder/IsActive) **+ 20 jednolinijskih podklasa** (po jedna po tabeli, svaka mapirana na svoju tabelu) + **registry**: `type slug → CLR tip + typed accessor` | Generički slice-ovi rade nad `CodebookEntry` preko registry-ja; podklase su trivijalne, a drift test automatski pokriva svih 20 tabela. Nema dinamičkog SQL-a — `{type}` iz rute se mapira kroz registry, nikad u SQL |
| **O2** | Tabele sa ekstra kolonama | `tax_rates` i `service_types` — podklase sa dodatnim svojstvima + **dedicirani slice-ovi** za upis (tipizirana polja); generički read ih i dalje lista (bazna polja) | 2 izuzetka ne opravdavaju generički „extras" mehanizam |
| **O3** | Autorizacija u F2 | Endpoints se mapiraju **bez enforcementa** (JWT dolazi u F3); svaki endpoint nosi `// TODO(F3): permission "x:y"` komentar | F2 < F3 po redosledu plana; ništa se ne deploy-uje pre F3 |
| **O4** | Dizajn API-ja | **Slobodan** — frontend još ne postoji (potvrđeno 2026-07-19), nema obaveze prema legacy rutama. Legacy služi samo kao inspiracija (npr. `POST {id}/activation` obrazac je dobar) | jednostavniji i konzistentniji API bez tereta kompatibilnosti |
| **O5** | Codebook API rute | `/api/codebook/{type}` — `{type}` se validira protiv registry-ja | jedan generički kontroler-slice za 18 tabela |

## 1. Codebook — registry + read slice

- [ ] `CodebookDbContext : ModuleDbContext` — `CodebookEntry` bazna klasa + 20 podklasa mapiranih na svoje tabele (`codebook` šema); `Label` preko `HasJsonbLabel()`
- [ ] `CodebookRegistry`: kompajlirana mapa `type slug → (CLR tip, typed accessor IQueryable<CodebookEntry>)` — jedini ulaz za generičke slice-ove
- [ ] Slice `ListCodebook`: `GET /api/codebook/{type}?includeInactive=false` — LINQ preko registry accessora, sortiranje po `sort_order, code`; nepoznat `type` → 404 (`Error.NotFound`)
- [ ] Keš: `IMemoryCache`, ključ po tipu, bez isteka (šifarnici se menjaju retko) — invalidacija na svaki upis (stavka 2)
- [ ] `GET /api/codebook` — lista dostupnih tipova (za admin UI)

**DoD:** integracioni testovi: poznat tip vraća seed redove sa JSONB labelom; `includeInactive` filtrira; nepoznat tip → 404; keš se koristi (verifikovano kroz invalidaciju u stavci 2); EF drift test automatski pokriva svih 20 tabela.

## 2. Codebook — admin CRUD

- [ ] `CreateCodebookEntry`: `POST /api/codebook/{type}` (code, label, sort_order) — duplikat koda → 409 (`Error.Conflict`); FluentValidation: code pattern (`[a-z0-9_]+`), label mora imati bar `en`
- [ ] `UpdateCodebookEntry`: `PUT /api/codebook/{type}/{id}` (label, sort_order)
- [ ] `SetCodebookEntryActivation`: `POST /api/codebook/{type}/{id}/activation` (`is_active` toggle — šifarnici se ne brišu, samo deaktiviraju; `code` je nepromenljiv)
- [ ] Svaki upis invalidira keš svog tipa
- [ ] Dedicirani slice-ovi za odstupanja (O2): `tax_rates` (+`rate`), `service_types` (+`default_duration_min`) — ista ruta, tipizirani request/response
- [ ] `// TODO(F3): permission "codebook:manage"` na admin endpointima

**DoD:** integracioni testovi za create/update/activation + 409 na duplikat koda + invalidacija keša (create → sledeći list vidi novi red); unit testovi validatora.

## 3. Codebook — Contracts za ostale module

- [ ] `Contracts/ICodebookLookup`: `Task<short?> GetIdByCodeAsync(string type, string code)` + `Task<CodebookEntryRef?> GetByIdAsync(string type, short id)` — čita kroz isti keš
- [ ] Registracija u DI kroz `CodebookModule` (implementacija internal)

**DoD:** unit/integracioni test lookup-a; arch testovi i dalje zeleni (Contracts je jedini public izlaz).

## 4. Tenants — EF model + DbContext

- [ ] Entiteti: `Tenant`, `SubscriptionPlan`, `TenantSubscription` (svi `AuditableEntity`); JSONB labele na `SubscriptionPlan.Label/Description` preko `HasJsonbLabel()`
- [ ] `TenantsDbContext : ModuleDbContext` — mapira **samo** `tenant` šemu; registracija kroz `AddModuleDbContext<TenantsDbContext>()` u `TenantsModule`
- [ ] EF drift test automatski pokupi novi kontekst (bez izmena testa)

**DoD:** drift test zelen sa novim kontekstom; build zelen.

## 5. Subscription plans slice-ovi

- [ ] `ListSubscriptionPlans`: `GET /api/subscription-plans` (aktivni, sortirani po `sort_order`; `?includeInactive` za admin) — javno čitljivo (onboarding)
- [ ] `CreateSubscriptionPlan` / `UpdateSubscriptionPlan`: admin CRUD (code, label, price, currency, billing period, limiti, features JSONB, is_public) — `currency_id`/`billing_period_id` se validiraju kroz `ICodebookLookup` (prva upotreba cross-module contracta!)
- [ ] `SetSubscriptionPlanActivation`: toggle
- [ ] `// TODO(F3): permission "subscription_plans:manage"`

**DoD:** integracioni testovi CRUD + validacija nepostojeće valute (400); seed planovi iz migracija vidljivi u listi.

## 6. Tenants CRUD slice-ovi

- [ ] `ListTenants`: `GET /api/tenants` — `PagedResponse`, pretraga po name/slug, filter `is_active`
- [ ] `GetTenantById`: `GET /api/tenants/{id}` — detalj + trenutni plan
- [ ] `CreateTenant`: `POST /api/tenants` — validacija: slug format (`[a-z0-9-]+`) + jedinstvenost (409), plan postoji i aktivan, valuta postoji (`ICodebookLookup`); upis tenanta **+ početni red u `tenant_subscriptions`** u istoj transakciji (dokaz deljene transakcije kroz EF)
- [ ] `UpdateTenant`: `PUT /api/tenants/{id}` — kontakt/fiskalna polja (tax_id, matični broj, PDV status, banka, labor rate…); slug se ne menja
- [ ] `SetTenantActivation`: `POST /api/tenants/{id}/activation` (suspenzija — `is_active`)
- [ ] `DeleteTenant`: `DELETE /api/tenants/{id}` — soft delete
- [ ] `// TODO(F3): platform-admin scope + permisije po uzoru na legacy imena (`tenants:read/create/update/deactivate/delete`)`

**DoD:** integracioni testovi po slice-u (kroz `WebApplicationFactory` + kontejnersku/lokalnu bazu): happy path, 404, 409 na duplikat slug-a, paged list, soft-delete nevidljiv u listi; audit kolone popunjene (interceptor); create upisuje i istoriju pretplate atomski.

## 7. Tenant subscriptions — istorija i promena plana

- [ ] `ListTenantSubscriptions`: `GET /api/tenants/{id}/subscriptions` — istorija (valid_from/valid_to/trial_until)
- [ ] `ChangeTenantSubscription`: `POST /api/tenants/{id}/subscriptions` — zatvara tekući red (`valid_to`), otvara novi, ažurira `tenants.subscription_plan_id` — sve u jednoj transakciji; CHECK `valid_to >= valid_from` poštovan
- [ ] Domenski događaj `TenantSubscriptionChanged` (prvi pravi event — zasad bez handlera; Notifications ga pretplaćuje u F3)

**DoD:** integracioni test promene plana: istorija konzistentna, tekući plan ažuriran, sve ili ništa na grešci.

## 8. Završno

- [ ] Ažurirati status u ovom fajlu + `backend-plan.md` F2 red
- [ ] CI zelen na grani

> **Napomena (revizija 2026-07-19):** dve ranije stavke su otpale — (a) regex arch provera ručnog SQL-a: bez Dappera u F2 nema ručnog SQL-a; provera se uvodi tek ako/kad neki modul počne da koristi EF `SqlQuery`/`FromSql`; (b) `api-deviations.md` evidencija: nema obaveze kompatibilnosti sa legacy API-jem (O4), pa nema ni odstupanja koja bi se pratila.

## Redosled i zavisnosti

```
1 (registry+read) → 2 (codebook CRUD) → 3 (Contracts)
                                           └→ 4 (EF model) → 5 (plans) → 6 (tenants CRUD) → 7 (subscriptions) → 8
```

Stavke 1–3 (Codebook) su nezavisne od 4–7 (Tenants) osim što stavka 5 koristi `ICodebookLookup` iz stavke 3. Codebook ide prvi jer je najmanji i postavlja slice obrazac.

## Izlaz faze (ukupni DoD)

Dva funkcionalna modula sa end-to-end slice-ovima kroz pravi HTTP + bazu: šifarnici (list, admin CRUD, keš, contract za druge module), tenanti (CRUD + pretplate sa istorijom), prvi cross-module contract poziv, prvi domenski događaj, prvi modulski DbContext-i u drift testu (svih 20 codebook tabela + 3 tenant tabele). Obrazac slice-a je dokazan i dokumentovan samim kodom — F3 (Auth) može da krene.
