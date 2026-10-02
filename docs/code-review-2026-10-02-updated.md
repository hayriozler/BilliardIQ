# Code review status: Scoreboard.WebApp and Scoreboard.Client

Updated status of every item in `code-review-2026-10-02.md`. Statuses: FIXED, PARTIAL, OPEN, FALSE POSITIVE, CANNOT VERIFY. A second, read-only agent re-checked each item against the working tree; items marked "tested" were also exercised against a real PostgreSQL. Nothing below is pushed yet (20 local commits on `feature/mobile-redesign`).

## Summary

| Section | FIXED | PARTIAL | OPEN | FALSE POSITIVE | CANNOT VERIFY |
|---|---|---|---|---|---|
| C HIGH (6) | 1 | 1 | 4 | 0 | 0 |
| C MEDIUM | 16 | 2 | 3 | 1 | 0 |
| C LOW (11) | 1 | 0 | 10 | 0 | 0 |
| A Unused code (16) | 10 | 3 | 2 | 0 | 1 |
| B Duplicate code (11) | 2 | 1 | 8 | 0 | 0 |
| D Other (17) | 6 | 0 | 9 | 0 | 2 |

## C. Bugs and risks

### HIGH (not started)
1. Photo upload: extension, size and content are not validated (`PlayerService.cs` `SavePhotoAsync`). OPEN. Stored XSS through `.html`/`.svg` under `wwwroot/Players` is real; classic `../` traversal is harder on Linux than first claimed.
2. Cookie session is never revalidated (`Program.cs`): deactivated staff keep access for up to 30 days. OPEN. (JWT sessions are revalidated now, see MEDIUM 18.)
3. `RemotePullService.cs:65` and `RemoteSyncService.cs:52`: one HttpClient timeout ends the loop silently. OPEN.
4. SQLite write transaction stays open while photos download (`RemotePullService.cs:125-140`). OPEN.
5. `Home.razor` `ShouldRender` and `LocalizationService` query the database on every render. PARTIAL: the shot clock now uses a Stopwatch; the per-render queries remain.
6. ClientId credential: the committed `appsettings.Production.json` has none, but the working copy does and the file is tracked. OPEN (risk): do not `git commit -a`.

### MEDIUM
- M1 `/Db/scoreboard.db3` downloadable: FALSE POSITIVE (returns 404, unknown file type). The database still lives under `wwwroot`, which is hygiene only.
- M2 `/api/stats`: PARTIAL. Delete endpoint removed and `Email` removed from `PlayerDto`. Still open: authentication is only the static client id, `LicenseNo`/`LicenseValidUntil` still go to kiosks, no throttling on those paths, `Api:AllowHttp` allows plaintext.
- M3 bucket and name limits: FIXED (index 0-35, names clamped, sum clamped against overflow).
- M4 change-log rows outside the transaction: FIXED (tested: a failing log write now rolls back the entity).
- M5 system players renamed for every organisation: FIXED (tested).
- M6 table open/close/settle races: FIXED and tested (6 simultaneous opens give 1 session, 4 closes give 1 closure, 5 settlements give 1 payment). Not locked: table status edits and order writes.
- M7 rate limiting and lockout: FIXED and tested (see new problems 1 and 2).
- M8 roles enforced only by hiding UI: PARTIAL. Players, clubs and teams pages and API writes require Owner/Manager; only an owner can deactivate a manager or another owner. Other pages (Tables, Pricing, Products, Tournaments...) are still plain `[Authorize]`.
- M9 shot-clock loop dies on exception: FIXED (built, not run).
- M10 one circuit-scoped `DbContext` shared by timer, render and event handlers in `Home.razor`: OPEN (see section 4).
- M11 match result and buckets saved separately, no idempotency key, permanent 4xx retried: OPEN.
- M12 `NewGame` leaks score events: FIXED (built, not run).
- M13 takeover does not stop the old screen: FIXED (built, not run).
- M14 `IsShuttingDown` never reset: PARTIAL. The flag is reset; `_heldKeys`/`_holdCts` are still unlocked in a singleton.
- M15 unbounded queries: FIXED for the reported cases. `Include(User)` removed from shared player lists; statistics pages default to the last 6 months with a custom date range.
- M16 Dashboard timer overlap and leak: FIXED.
- M17 unhandled handler exceptions: FIXED. Photo over 5 MB is refused with a message, unique violations on email, phone and shortcut map to normal messages, `ErrorBoundary` in the layout. (The "system player delete" example was unreachable from the UI.)
- M18 JWT valid after revoke: FIXED. Remaining: staff or organisation deactivation is not checked on that path.
- M19 mobile refresh race: OPEN. Two concurrent uses of the same refresh token trigger reuse detection and sign the user out everywhere. Planned fix: accept a just-rotated token for 30-60 seconds.

### LOW (all OPEN except the first)
- Mobile profile phone duplicate gave a 500: FIXED (now a message). Length checks on profile inputs: OPEN.
- RefreshToken and PlayerInvite rows are never purged.
- Email travels in redirect query strings (login and register error redirects).
- Antiforgery disabled on `/login`, `/logout`, `/register`, `/culture` (Lax cookie mitigates).
- `bq_pick` cookie lacks `Secure`.
- `ClubService.EnsureDefaultClubAsync` check-then-add race.
- `ShowNoticeAsync` uses `Task.Delay` without a token (can throw after disposal).
- `_kiosk ??=` can import the JS module twice.
- `WarmUp.razor` subtracts a fixed 0.1 per tick and never disposes its linked CTS.
- `CurrentPoints--` can go negative.
- Server data is trusted in `RemotePullService` (`ToDictionary` on duplicate slot, absolute `PhotoPath` overrides the base URI).
- Seed accounts run in every environment if configured; only `appsettings.Development.json` holds a password.

## A. Unused code
FIXED: `AuthService.LoginAsync`, `Format.PaymentMethod`, `AvatarGenerator.SeedFromName`/duplicate, `kiosk.js` `isFullscreen`, `Blazor.LocalStorage` package, `LanguageChanged` event, `RuleSet.ToSnapshot`, unused `HttpContext` parameters, unused usings, and most schema-only `DbSet`s plus the `AuditLog` entity (done by the owner).
PARTIAL: unused `var orgId` (`Cities.razor:129,141` remain); remaining unused DbSets `ReservationSet`, `CashRegisterShiftSet`, `MatchParticipantSet`, `MatchEventSet`, `StageGroupSet`; `ITenantScoped` marker.
OPEN: `ITenantScoped` (marker only), `User.IsPlatformAdmin` (written, never read), `RefreshToken.LastUsedAt` (set once), `PlayerDto.ClubId` (always null).
CANNOT VERIFY: unused localisation keys (about 60 old device-pairing, geo and region strings) need a scripted check. `MatchesSet` was a false positive: it is used.

## B. Duplicate code
FIXED: `AvatarGenerator` (one file, linked into the Client), entity-to-DTO mapping (endpoints reuse `ScoreboardDataService`).
PARTIAL: organisation language/org-fetch queries (still repeated in `StatsService`, `ClubService`, `PricingService`, `TableService`).
OPEN: `DefaultShortName` (Club/Team), currency symbol switch (`Format.cs`, left on purpose), Geo CRUD services, Razor `RunAsync` boilerplate (11 pages), endpoint try/catch to `BadRequest`, `IsLocal`, player name/avatar validation, Client sync HttpClient setup and get-or-add setting, `Home.razor`/`WarmUp.razor` shared code.

## D. Other issues
FIXED: missing Client catalog keys (`P1`, `P2`, `Player2Default`), hard-coded "Language/New Game/End Game", `NewGame` hard-coded ids, CS1998 warnings, `InitializeDb` naming, `JwtSettings.SigningKey` caching.
OPEN: `ShouldRender` has side effects; `wal_checkpoint` is a no-op because WAL is never enabled and the Client uses `GetCurrentDirectory()`; `POST /api/players` response lacks association/region names; `TeamMember.LeftAt` is never set so the kiosk filter question is latent; cup byes count as won without played, `CupService` uses server-local `DateTime.Today` and returns a Task for synchronous work; duplicate localisation keys (`El`, `G`, `M`, `Temizle`); `PlayerService` `Nationality`/`City` now come only from free-text input, which the form does not provide, so they stay empty (previously the same effective result).
CANNOT VERIFY: `CityService`/`RegionService` `Include` then `Select` (null `Country`?), prerender running `OnInitializedAsync` twice.
Not a defect: Turkish strings in `Format.cs` (localisation keys by design, left unchanged on purpose).

## New problems found in the changed code
1. **Lockout by email lets anyone lock any account.** `LoginThrottle` counts failures per email only; five bad attempts lock a known address for 15 minutes. Suggested: key by email plus client address, keep the per-address rate limit.
2. **Per-address rate limit may be spoofable.** `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` trusts any `X-Forwarded-For`, and port 8443 is published directly. Suggested: restrict trusted proxies to Cloudflare ranges or close the direct port.
3. **Sole owner can deactivate themselves** while a manager is active. FIXED: deactivation of an owner now requires another active owner.
4. **Shot-clock tick mutates state and saves off the dispatcher** (`Home.razor` `ShotClockTickAsync`); this is the M10 race, now logged instead of killing the loop.
5. **`g.Sum` overflow in match stats**: FIXED (clamped).
6. **Table locking** uses hand-written table names and `FOR UPDATE`; verified against PostgreSQL (see M6).
