# Plan F3 — Notifications + Auth moduli

**Status:** 📝 plan potvrđen — spreman za implementaciju
**Datum plana:** 2026-07-19 · **Odluke potvrđene:** 2026-07-20 (O1–O8 po predlogu; O9 izmenjen — CLI umesto startup seeder-a)
**Referenca:** [backend-plan.md](../backend-plan.md) §7 (auth tok), §11 (F3) · prethodi: [f2-codebook-tenants.md](f2-codebook-tenants.md) · povezano: [3.1-auth-sitnice.md](../../database/plans/3.1-auth-sitnice.md) (§E — RLS za role), [2.2-rls.md](../../database/plans/2.2-rls.md)

Cilj faze: **prijava sa izborom servisa radi end-to-end; mejlovi rade** — ceo auth tok iz backend plana §7 (login/select/switch, refresh rotacija, reset, verifikacija, OIDC, permisije) na membership modelu, plus Notifications modul (outbox + dispatcher). Od ove faze API više nije anoniman: autorizacija se primenjuje i na postojeće F2 endpointe.

**Kontekst iz šeme:** `auth` ima 12 tabela (users, user_tenants, roles, permissions, role_permissions, user_roles, refresh_tokens, password_reset_tokens, email_verification_tokens, external_logins, login_history, mfa_recovery_codes); `notification` ima email_outbox + email_templates. Plan 3.1 je implementiran u šemi (partial unique na `LOWER(email)`, lockout kolone, verifikacioni tokeni, `login_history.attempted_email`). **RLS:** `roles`/`role_permissions`/`user_roles` i `email_outbox` SU pod RLS-om (3.1 §E; platform redovi vidljivi samo platform kontekstu) — ostatak auth šeme je van RLS-a.

**Šta se portuje iz legacy backenda** (`src/backend/legacy/`, port ne referenca): `AuthService` (tok prijave, rotacija sa detekcijom krađe, reset, OIDC state/PKCE/handoff), `JwtTokenService`, `PermissionPolicyProvider/Handler + HasPermission`, `EmailOutbox` + `SimpleTemplateRenderer` + `MailKitSmtpClient` + `EmailDispatcherHostedService`, `OidcExternalAuthClient` + memory state/handoff keševi, `FrontendUrlProvider`. Sve se prilagođava membership modelu (legacy je pisan uz staru `users.tenant_id` šemu) i EF Core-u (legacy je Dapper).

---

## 0. Odluke faze (potvrditi pre početka)

| # | Pitanje | Predlog | Obrazloženje |
|---|---|---|---|
| **O1** | MFA u F3? | **Ne** — kolone i `mfa_recovery_codes` postoje u šemi i mapiraju se u EF (drift pokrivenost), ali tok se ne implementira | §7 ga ne pominje; solo razvoj — MFA ima smisla tek uz produkciju. Posebna mini-faza kasnije |
| **O2** | Kako refresh zna aktivni tenant? | **Opcioni `tenant_id` u RefreshRequest** — server validira članstvo pa izdaje tokene za taj tenant; bez njega tokeni bez tenant claima (kao login sa više članstava) | `refresh_tokens` nema tenant kolonu (ispravno — sesija je globalna); frontend drži aktivni tenant i šalje ga. Bez izmene šeme |
| **O3** | Selection token (login sa N članstava) | **Kratkotrajni JWT** (~5 min) sa claimom `scope=tenant_select`, isti ključ potpisa; bez DB stanja | Ne važi ni za šta drugo (policy odbija scope); stateless |
| **O4** | Privilegovani DB pristup (bypass RLS-a za `workshopadmin_app`) | **`IPrivilegedDbSession` u SharedKernel** — otvara konekciju + transakciju sa `SET LOCAL app.is_platform_admin='true'`; koriste ga **tačno tri revidirana mesta**: građenje claimova pri izdavanju tokena (3.1 §E), pre-login upis u outbox, email dispatcher | `email_outbox` i role tabele su pod RLS-om, a ova mesta rade pre/van tenant konteksta. Jedna imenovana apstrakcija umesto raštrkanih `set_config` poziva |
| **O5** | Lockout pragovi | **5 uzastopnih promašaja → 15 min** (`AuthOptions`, konfigurabilno); reset brojača na uspeh | Plan 3.1: pragovi u config-u, ne u bazi |
| **O6** | Da li neverifikovan email blokira login? | **Ne u F3** — mehanizam (slanje + potvrda) se implementira ceo, ali se login ne uslovljava | Samoregistracija mušterija stiže tek sa portalom; tada se uvodi enforcement politika |
| **O7** | Admin CRUD (users/memberships/roles/permisije) u F3? | **Da, ceo** — bez toga F4+ nema kome da izdaje naloge niti kako da dodeljuje role | Legacy već ima User/Role/Permissions kontrolere kao referencu; ovo je poslednji trenutak pre nego što domenske faze počnu da zavise od korisnika |
| **O8** | OIDC provajderi | **Samo Google** u konfiguraciji; klijent je generički OIDC (port), pa je dodavanje provajdera samo config | Legacy registry je već generički |
| **O9** | Bootstrap prvog platform admina | **CLI projekat `WorkshopAdmin.Cli`** sa komandom `seed-admin` — port legacy `WorkshopAdmin.CLI` (interaktivni unos email/ime/maskirana lozinka, idempotentan upis) prilagođen novoj šemi i libsodium hasheru | *Izmenjeno 2026-07-20 (korisnik):* umesto startup seeder-a — isti pristup kao u legacy backendu. Hash se pravi u aplikaciji (libsodium), ne može u SQL seed |

## 1. Notifications — EF model + enqueue contract

- [ ] `NotificationsDbContext : ModuleDbContext` — `EmailOutboxMessage` (`notification.email_outbox`; create-once + dispatcher update polja, bez soft delete) i `EmailTemplate` (`notification.email_templates`; subject/body_text/body_html kao JSONB per-locale rečnici preko postojećeg konvertera)
- [ ] **Contracts projekat `WorkshopAdmin.Modules.Notifications.Contracts`** (konvencija iz F2): `IEmailEnqueuer.EnqueueAsync(EmailRequest, ct)` — renderuje šablon (template code + placeholderi + locale) i upisuje red u outbox **kroz ambijentalnu `IDbSession` transakciju** (outbox pattern: atomski sa izvornom promenom); `EmailRequest(TemplateCode, ToAddress, ToName?, Placeholders, TenantId?, RelatedEntityType?, RelatedEntityId?, Locale = "sr")`
- [ ] Template renderer: port `SimpleTemplateRenderer` ({{Placeholder}} zamena) + izbor locale-a sa fallback na `en`; renderovani subject/body se upisuju u outbox red (šema to zahteva — kasnije izmene šablona ne diraju već uvrštene poruke)
- [ ] Idempotencija po potrebi: pre upisa provera `(related_entity_type, related_entity_id, template_code)` za slučajeve gde duplikat ne sme (koristi F5 podsetnik; u F3 samo helper)

**DoD:** integracioni test: enqueue u transakciji koja se rollback-uje ne ostavlja red (outbox atomicnost); enqueue sa nepoznatim template code → greška; render bira `sr` pa pada na `en`.

## 2. Notifications — SMTP + dispatcher hosted service

- [ ] Port `ISmtpClient` + `MailKitSmtpClient` + `EmailOptions` (host, port, TLS, kredencijali, from) — paket `MailKit` već u props
- [ ] Port `EmailDispatcherHostedService`: petlja sa intervalom (config), claim pending redova (`status='pending' AND next_attempt_at <= NOW()`, `FOR UPDATE SKIP LOCKED`, batch), slanje, `sent`/increment `attempts` + eksponencijalni `next_attempt_at` / `failed` posle `max_attempts`
- [ ] Dispatcher radi **van HTTP zahteva**: svoj DI scope po ciklusu + **`IPrivilegedDbSession`** (O4) — mora da vidi i redove sa `tenant_id IS NULL` i tuđe tenant redove (RLS)
- [ ] U testovima/dev bez SMTP-a: `ISmtpClient` fake registrovan u test hostu (dispatcher se testira bez mreže); u dev okruženju config prekidač da se dispatcher ne pokreće

**DoD:** integracioni test kroz fake SMTP: enqueued red → dispatcher ga označi `sent` i fake primi poruku; simuliran SMTP pad → `attempts++`, `next_attempt_at` u budućnosti, posle max_attempts → `failed`.

## 3. SharedKernel — `IPrivilegedDbSession` (O4)

- [ ] Ista mehanika kao `DbSession` (konekcija `workshopadmin_app`, BEGIN, `set_config` parametrizovano), ali uvek `app.is_platform_admin='true'`; nezavisan od `ICurrentUser`
- [ ] Registracija transient/factory (nije request-scoped — koristi se i iz hosted service-a); XML-doc eksplicitno nabraja tri dozvoljena upotrebna mesta (O4) — svaka nova upotreba je code-review stavka

**DoD:** integracioni test: kroz privilegovanu sesiju vidljivi outbox redovi dva različita tenanta + NULL-tenant red; kroz običnu sesiju bez konteksta — nijedan (fail-closed dokaz).

## 4. Auth — EF model + DbContext

- [ ] `AuthDbContext : ModuleDbContext` — mapira **samo** `auth` šemu: `User`, `UserTenant` (composite PK), `Role`, `Permission`, `RolePermission` (composite PK), `UserRole`, `RefreshToken` (append-only + revoked polja), `PasswordResetToken`, `EmailVerificationToken`, `LoginHistoryEntry` (append-only, `attempted_email`), `ExternalLogin`, `MfaRecoveryCode` (samo mapiranje — O1)
- [ ] Email se normalizuje na lowercase pri upisu (partial unique je na `LOWER(email)`); value-level: `User.Email` setter/handler disciplina + validatori
- [ ] Navigacije unutar šeme (User↔UserTenants, Role↔RolePermissions…); ka `tenant` šemi **nema navigacija** — imena/status tenanta idu kroz novi `ITenantLookup` (stavka 6)

**DoD:** EF drift test zelen sa `AuthDbContext` (pokriva svih 12 tabela); build zelen.

## 5. Auth — hasher + JWT + `ITokenIssuer`

- [ ] **`Sodium.Core`** u props + `IPasswordHasher` u **SharedKernel** (`Security/`): `crypto_pwhash_str` (Argon2id, PHC format), `Verify` + `NeedsRehash` (rehash pri uspešnom loginu ako parametri ojačani) — **novo, ne port** (odluka iz sekcije 13: Konscious se ne prenosi). U SharedKernel, a ne u Auth modulu, jer ga koristi i CLI (O9) i test helper
- [ ] Port `JwtOptions` + `JwtTokenService`: access token (HS256, ~10 min, config) sa claimovima `sub`, `email`, `tenant_id?`, `is_platform_admin?`, role + permisije **za aktivni tenant**; refresh token = 64B random, SHA-256 hash u bazi, ~30 dana (config); selection token (O3)
- [ ] `ITokenIssuer` (interni servis): za (user, tenant?) učitava članstva, role i permisije — upit nad `user_roles` uvek `tenant_id = @tid OR tenant_id IS NULL` — i **izvršava se kroz `IPrivilegedDbSession`** (3.1 §E: čita RLS-ovane role tabele pre nego što JWT postoji; utvrđuje i platform-admin status kroz NULL-tenant dodele)
- [ ] Konzistentnost claimova: `is_platform_admin` samo iz platform-scope dodele; `tenant_id` claim samo uz aktivno članstvo (`user_tenants.is_active AND NOT is_deleted` + tenant aktivan)

**DoD:** unit testovi hashera (hash/verify/needs-rehash) i token servisa (claims, isteci, selection scope); integracioni test `ITokenIssuer`-a: custom rola tenanta ulazi u claims samo za svoj tenant.

## 6. Login tok (backend plan §7)

- [ ] **Tenants modul dobija `ITenantLookup`** u postojećem `Tenants.Contracts`: `GetRefsAsync(ids)` → (Id, Name, Slug, IsActive) — Auth ne sme SQL-om u `tenant` šemu
- [ ] `POST /api/auth/login` (anonimno): normalizacija emaila → user lookup → lockout provera (`locked_until`) → Argon2 verify → **lockout knjigovodstvo** (O5: increment/reset `failed_login_count`, postavljanje `locked_until`) → aktivna članstva:
  - platform admin (0 članstava + platform dodela) → puni tokeni sa `is_platform_admin`
  - 1 članstvo → puni tokeni sa `tenant_id`
  - N članstava → lista servisa (`ITenantLookup` imena) + **selection token** (O3)
- [ ] `login_history` upis za svaki ishod; nepoznat email → red sa `attempted_email` (novo u odnosu na legacy); odgovor je uvek identičan „Invalid email or password" (bez user enumeration — port legacy discipline)
- [ ] `POST /api/auth/select-tenant` (selection token) i `POST /api/auth/switch-tenant` (važeći access token): provera članstva → puni tokeni kroz `ITokenIssuer`; obe izdaju i **novi refresh token** (rotacija konteksta ne produžava stari)
- [ ] Neaktivan user / neaktivno članstvo / neaktivan tenant → isti odbijajući odgovor + tačan razlog u `login_history`

**DoD:** integracioni testovi: 0/1/N članstava tokovi; pogrešna lozinka 5× → 6. pokušaj odbijen i sa tačnom lozinkom (lockout), posle isteka prolazi; suspendovan tenant blokira login u taj tenant ali ne i u drugi; `login_history` redovi tačni (uklj. `attempted_email`).

## 7. Refresh rotacija + logout

- [ ] `POST /api/auth/refresh` (anonimno): hash lookup → istekao/nepoznat → 401; **revoked → detekcija krađe: revoke cele aktivne familije user-a** (port); validan → rotacija u jednoj transakciji (novi red + `revoked_at`/`replaced_by_token_id` na starom) + novi access token za `tenant_id` iz zahteva (O2, validirano članstvo)
- [ ] `POST /api/auth/logout` (revoke jednog, idempotentno) i `POST /api/auth/logout-all` (autentifikovano — revoke svih)
- [ ] Reset lozinke i deaktivacija naloga revokuju sve refresh tokene (veza sa stavkom 9 i 11)

**DoD:** integracioni testovi: rotacija radi; replay starog tokena → 401 + cela familija mrtva (sledeći refresh novim tokenom takođe 401); logout idempotentan; O2 — refresh sa tenant_id bez članstva → 401.

## 8. Host autorizacija + primena na F2 endpointe

- [ ] JWT bearer u `WorkshopAdmin.Api` (validacija potpisa/isteka, mapiranje claimova); `ClaimsCurrentUser` stub iz F1 postaje stvarni izvor (`sub`, `tenant_id`, `is_platform_admin`, permisije)
- [ ] Port permission autorizacije na minimal APIs: policy provider (`permission:<code>`) + `RequirePermission("x")` ekstenzija na `RouteHandlerBuilder`/grupi; platform-scope endpointi dodatno `RequirePlatformAdmin()`
- [ ] **Zameniti sve `// TODO(F3): permission` markere iz F2** stvarnim pravilima: codebook read = autentifikovan (`codebook:read`), codebook write = `codebook:manage`; subscription plans list ostaje **anonimna** (onboarding izlog), plans write = platform admin + `subscription_plans:*`; tenants CRUD = platform admin + `tenants:*` (imena permisija već seedovana u bazi)
- [ ] **Test infrastruktura za autentifikovane pozive** (ključni deliverable za sve buduće faze): `AuthApiClient` helper — seed user + membership + rola kroz admin konekciju (privilegovano), login kroz pravi API, keš tokena po fixtue-u; **postojeći F2 testovi se ažuriraju** da ga koriste (i dobijaju po jedan 401/403 negativan slučaj)

**DoD:** anonimni poziv zaštićenog endpointa → 401; autentifikovan bez permisije → 403; sa permisijom → 200; F2 testovi zeleni kroz auth helper; platform-admin endpointi odbijaju tenant korisnika.

## 9. Password reset + email verifikacija

- [ ] Port reset toka na membership model: `POST /api/auth/forgot-password` (uvek 204 — bez enumeration; token 32B, SHA-256 hash, 60 min) + enqueue `password_reset` mejla **u istoj transakciji** (`IEmailEnqueuer`); pre-login kontekst → transakcija kroz `IPrivilegedDbSession` (O4, RLS na outbox)
- [ ] `POST /api/auth/reset-password` (token + nova lozinka): validacija tokena (hash, `used_at`, istek) → novi hash → `used_at` + **revoke svih refresh tokena**
- [ ] Verifikacija: `POST /api/auth/send-verification` (autentifikovano) + `POST /api/auth/verify-email` (token → `email_verified_at`) — obrazac identičan resetu, template `email_verification` već seedovan; bez login enforcementa (O6)
- [ ] `FrontendOptions`/`IFrontendUrlProvider` port (reset/verify URL-ovi ka Angular aplikaciji)

**DoD:** integracioni test e2e: forgot → red u outbox-u sadrži validan URL/token → reset menja lozinku, stari refresh mrtav, token jednokratan; verify postavlja `email_verified_at`; nepoznat email na forgot → 204 bez outbox reda.

## 10. OIDC external login

- [ ] Port generičkog OIDC klijenta + registry + `ExternalAuthOptions` (Google config — O8): `GET /api/auth/external/{provider}/start` (PKCE + state cache) → `GET .../callback` (state validacija, code exchange, match: postojeći link → user; verifikovan email → auto-link; inače odbij) → handoff kod → `POST /api/auth/external/exchange`
- [ ] Prilagođenje membership modelu: posle uspešnog external identiteta ishod je **isti kao login stavke 6** (0/1/N članstava → tokeni ili selection token); `login_history` metod = provider code
- [ ] Memory state/handoff keševi port (TTL); `external_logins.last_login_at` update

**DoD:** integracioni testovi sa fake `IExternalAuthClient`: postojeći link → tokeni; nepoznat identitet bez naloga → odbijen; neverifikovan email kod provajdera → odbijen; auto-link kreira `external_logins` red; N članstava → selection token.

## 11. Admin — users + memberships (O7)

- [ ] `GET/POST/PUT /api/users` + `GET /api/users/{id}` + `POST /{id}/activation` — **globalni identitet** (platform admin: svi; tenant admin: samo useri sa članstvom u svom tenantu — RLS ne pokriva `users`, filter je aplikativni i testira se)
- [ ] Kreiranje user-a od strane tenant admina: postojeći email → **dodaje se samo članstvo** (bez diranja naloga — ista osoba u više servisa, model-review §4.1); novi email → user + članstvo + welcome/verifikacioni mejl
- [ ] Membership upravljanje: `POST/DELETE /api/users/{id}/tenants/{tenantId}` (+ aktivacija članstva); brisanje je soft, re-join un-delete (natural PK obrazac iz šeme)
- [ ] `POST /api/users/{id}/reset-password` (admin-inicirano — mejl korisniku, ne vraća lozinku); deaktivacija revokuje refresh tokene

**DoD:** integracioni testovi: tenant admin ne vidi usere van svog tenanta; postojeći email → drugo članstvo bez novog user reda; deaktiviran user ne može login ni refresh.

## 12. Admin — roles, permisije, dodele (O7)

- [ ] `GET /api/permissions` (katalog, grupisano po resursu); `GET/POST/PUT/DELETE /api/roles` — platform admin: globalne (`tenant_id NULL`); tenant admin: **custom role svog tenanta** (RLS iz 3.1 §E štiti, aplikacija postavlja `tenant_id` iz konteksta); `is_system` role nepromenjive
- [ ] `PUT /api/roles/{id}/permissions` (dodela permisija roli — custom rola samo scope=tenant permisije); `PUT /api/users/{id}/roles` (dodela rola po tenantu; aplikativno pravilo: custom rola tenanta X samo uz `user_roles.tenant_id = X`)
- [ ] Promene rola/permisija se vide pri sledećem izdavanju tokena (access ~10 min — prihvaćeno u §7); revoke refresh tokena pri oduzimanju rola nije potreban (odluka §7)

**DoD:** integracioni testovi: RLS izolacija custom rola (tenant B ne vidi/ne menja rolu tenanta A — test dopuna iz 3.1); dodela custom role van njenog tenanta → 400; nova permisija u tokenu posle ponovnog logina.

## 13. Prvi event pretplatnik — `TenantSubscriptionChanged`

- [ ] Notifications modul registruje handler (in-process, ista transakcija — obrazac §8.3): enqueue mejla vlasniku tenanta (kontakt email tenanta) o promeni plana
- [ ] Nova seed migracija šablona `subscription_changed` (en/sr, obrazac postojećih)

**DoD:** integracioni test: promena plana kroz API → red u outbox-u sa tačnim šablonom, atomski sa promenom (rollback scenario iz F1 event testa važi).

## 14. Bootstrap platform admina — CLI (O9)

- [ ] Novi projekat `src/WorkshopAdmin.Cli` (dodaje se u slnx, referencira samo SharedKernel): port legacy `WorkshopAdmin.CLI` — komanda `seed-admin`, interaktivni unos (email, ime, prezime, maskirana lozinka + potvrda), konekcija iz appsettings + user-secrets, sve u jednoj transakciji
- [ ] Prilagođenja novoj šemi (zamke porta):
  - `auth.users` više nema `tenant_id` kolonu — insert bez nje
  - legacy `ON CONFLICT (email)` više ne radi — unique je **partial index** `LOWER(email) WHERE NOT is_deleted`; umesto toga eksplicitni lookup po `LOWER(email)` pa insert (email se normalizuje na lowercase)
  - `user_roles` conflict target je partial index `(user_id, role_id, tenant_id) NULLS NOT DISTINCT WHERE NOT is_deleted` — idempotentna dodela preko lookup + insert/undelete, `tenant_id = NULL` (platform-scope)
  - hash preko `IPasswordHasher` iz SharedKernel (libsodium — stavka 5), ne legacy Argon2 klase
- [ ] Postojeći „repair" tok iz legacy ostaje: user već postoji → samo osiguraj platform_admin dodelu
- [ ] Integracioni testovi **ne pozivaju CLI** — test helper seeduje platform admina direktnim SQL-om kroz admin konekciju, koristeći isti `IPasswordHasher`

**DoD:** na svežoj migriranoj bazi `dotnet run --project src/WorkshopAdmin.Cli -- seed-admin` kreira admina i login kroz API radi; ponovno pokretanje ne duplira ništa (repair tok); arch testovi i dalje zeleni (CLI nije modul — sme da radi SQL kao poseban executable, kao i legacy).

## 15. Završno

- [ ] Ažurirati status u ovom fajlu + `backend-plan.md` F3 red (✅) i statusnu liniju
- [ ] `docs/DOCS.md`/memorija po potrebi; CI zelen na grani

---

## Redosled i zavisnosti

```
3 (IPrivilegedDbSession) ──┬→ 1 (outbox+enqueuer) → 2 (dispatcher) ──→ 13 (event → mejl)
                           └→ 4 (EF model) → 5 (hasher+JWT+issuer) → 6 (login) → 7 (refresh)
                                                                        └→ 8 (host auth + F2) → 9 (reset/verify) → 10 (OIDC)
                                                                                └→ 11 (users) → 12 (roles) → 14 (bootstrap) → 15
```

Stavke 1–2 (Notifications) i 4–7 (auth jezgro) mogu paralelno posle stavke 3. Stavka 9 zavisi od 1 (enqueuer). Stavka 8 je prelomna tačka — od nje su svi testovi autentifikovani. Stavka 14 (CLI) zavisi samo od stavke 5 (hasher u SharedKernel) i izvršenih migracija — može bilo kad posle nje; u grafu je pred kraj samo zato što njen DoD (login kroz API) traži stavku 6.

## Izlaz faze (ukupni DoD)

Prijava sa izborom servisa radi end-to-end protiv prave baze: login (0/1/N članstava), select/switch tenant, refresh rotacija sa detekcijom krađe, lockout, password reset i email verifikacija preko outbox mejlova koje dispatcher stvarno šalje (fake SMTP u testovima), OIDC prijava, users/memberships/roles/permisije admin API. Sva tri privilegovana RLS mesta izolovana iza `IPrivilegedDbSession`. F2 endpointi zaštićeni permisijama, testna infrastruktura za autentifikovane pozive spremna za F4+. MFA ostaje jedini neimplementirani deo auth šeme (O1).
