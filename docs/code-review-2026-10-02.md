# Code review: Scoreboard.WebApp and Scoreboard.Client (2026-10-02)

Raw findings from a read-only review agent. Nothing here has been verified by running code; each item comes from reading and grepping. Paths are relative to the repository root.

The per-organisation global query filter is sound: no `ScopedRunner` or `OrganizationRunner` bypass was found. The only `IgnoreQueryFilters` uses are intentional anonymous lookups (MobileAuthService.cs:246, 269, 271, 340). The raw SQL and `ExecuteUpdate` calls are keyed by ids that come from filtered queries.

Correction to the agent's report: `Scoreboard.Client/appsettings.Production.json` is tracked in git without a ClientId. The ClientId the agent saw is only in the uncommitted working copy.

## C. Bugs and risks

### HIGH
- **Scoreboard.WebApp/Services/PlayerService.cs:203-208 (called from :186-211).** `photoExtension` goes unchecked into `Path.Combine` and `File.WriteAllBytesAsync`, so `../` traversal and arbitrary extensions (`.html`, `.svg`, `.aspx`) are possible. The file lands under the static `wwwroot/Players`, so there is stored XSS and arbitrary file write. Reachable by any staff user via `POST /api/players` and by any player via `PUT /api/mobile/player/profile`. Fix: whitelist jpg/png/webp, sniff magic bytes, cap size, generate the file name server-side.
- **Scoreboard.WebApp/Program.cs:79-105.** The cookie scheme has no `OnValidatePrincipal`. A deactivated staff member (AuthService.cs:164) or suspended user, or a changed password or role, keeps the session for up to 30 days sliding. Fix: revalidate StaffMember.IsActive and User.Status against a security stamp.
- **Scoreboard.Client/Services/RemotePullService.cs:65 and :71, RemoteSyncService.cs:52 and :58.** The inner catch uses `when (ex is not OperationCanceledException)`. An HttpClient timeout throws `TaskCanceledException`, which is an `OperationCanceledException`, so it escapes the do/while to the outer silent catch. Pull and sync then stop for the rest of the process after one network timeout. Fix: filter on `!stoppingToken.IsCancellationRequested` and set `HttpClient.Timeout`.
- **Scoreboard.Client/Services/RemotePullService.cs:125-140 with :332 and :423.** A SQLite write transaction stays open while photos download over HTTP. Scoreboard writes (Home.razor:844, :1148, `PersistAsync`) block or hit "database is locked". Fix: download photos outside the transaction.
- **Scoreboard.Client/Components/Pages/Home.razor:552-565 with Services/LocalizationService.cs:16, :37, :59.** `ShouldRender` runs 4-6 synchronous DB queries on every render (ResolvePlayers, LoadOrganizationName, LoadRoster). `L.T` calls `GetLang`, which hits the DB each time; Home.razor has about 51 `L.T(` call sites and a miss queries twice. The shot clock re-renders at 10 Hz (Home.razor:1126-1156) and decrements a fixed 0.1 per tick (:1143), so it runs slow under load. Fix: cache the language, reload players only on change, compute the clock from a Stopwatch or timestamps.
- **Scoreboard.Client/appsettings.Production.json.** The ClientId is the only credential for `/api/stats` and `/api/scoreboard`. Keep it out of git (it currently is, in the committed version); regenerate it if it was ever pushed.

### MEDIUM
- **Scoreboard.Client/Program.cs:49-52 and :73.** The SQLite db sits in `wwwroot/Db` and `UseStaticFiles` serves it, so `/Db/scoreboard.db3` is downloadable. Fix: store the db outside wwwroot.
- **Scoreboard.WebApp/Middlewares/ClientIdMiddleware.cs:27-45 and Endpoints/MatchStatsEndpoints.cs:12 and :77.** No `RequireAuthorization` on `/api/stats`, which relies solely on the static `X-Client-Id`. With only that id a caller can delete any stat of the org. `PlayerDto` (ScoreboardDataService.cs:169-176) exposes email and licence to that caller. No throttling; `Api:AllowHttp` (Program.cs:208) permits plaintext. Fix: per-device secret via an auth scheme, drop Email from the scoreboard DTO, add rate limiting.
- **Scoreboard.WebApp/Endpoints/MatchStatsEndpoints.cs:35-40 and Services/StatsService.cs:190.** `BucketIndex >= 0` is the only check and the bucket count is unbounded. `new int[mine.Max(b => b.BucketIndex) + 1]` lets a client send `BucketIndex = int.MaxValue` and exhaust server memory. Names are not length- or null-checked (DB max is 100), which gives a 500. Fix: cap the index and count, validate lengths.
- **Scoreboard.WebApp/Data/DataContext.cs:413-421.** The EntityChange upsert runs after `base.SaveChangesAsync`, outside its transaction. If it fails, kiosks miss the change until the 24h resync (`_fullSyncInterval`, ScoreboardDataService.cs:~28). Fix: wrap both in one transaction.
- **Scoreboard.WebApp/Services/SystemPlayerService.cs:12-29 and Program.cs:253-254.** System players are global rows (filter `|| IsSystem`). Any org switching language renames them for every org, bumps `UpdatedAt`, and queues changes for all orgs (:23-25). Names are already localised at DTO time via `NameFor`. Fix: drop the persisted rename.
- **Scoreboard.WebApp/Services/TableService.cs:67-97, :126-161, :163-190.** Open, close and settle are check-then-act with multiple `SaveChanges` and no concurrency token. Concurrent calls can open a table twice, leave an orphan session, or create a duplicate Payment. Fix: transaction plus a RowVersion (xmin) token.
- **No rate limiting or lockout on credential endpoints:** AuthEndpoints.cs:16, `/api/mobile/auth/login`, `/refresh`, `/register-player`. Invite codes are only 8 chars from 32 symbols (MobileAuthService.cs:254-297). Fix: AddRateLimiter and a lockout.
- **Roles are enforced only by hiding UI.** Only Staff.razor:2 has a role attribute. `/api/players`, `/api/clubs`, `/api/teams` (PlayersEndpoints.cs:12, ClubsEndpoints.cs:11, TeamsEndpoints.cs:11) accept any staff role, including Waiter and Referee, for create and delete. `AuthService.SetStaffActiveAsync` (AuthService.cs:164-176) lets a Manager disable an Owner, because it only guards "last active user". Fix: manager policy on write endpoints and role checks in services.
- **Scoreboard.Client/Components/Pages/Home.razor:1126-1156.** The shot-clock loop only catches `OperationCanceledException`. A `DbContext.SaveChanges()` failure (:1148) or a `StateHasChanged` throw kills it silently, because it is started fire-and-forget at :491. Fix: catch-all with logging inside the loop.
- **Scoreboard.Client/Components/Pages/Home.razor:6, :492, :516 and :552.** One circuit-scoped `DataContext` is used by awaited async calls and by sync calls from the timer and `ShouldRender`; risk of "second operation started on this context". Fix: `IDbContextFactory` per operation.
- **Scoreboard.Client/Components/Pages/Home.razor:843-857 and Services/RemoteSyncService.cs:86 and :125-135.** `MatchResult` is saved at :844, but its buckets are saved later by `PersistAsync`; the sync loop can push the match first and lose the score distribution. The push has no idempotency key: a lost response re-POSTs and creates a duplicate `MatchStat`. A permanent 4xx is retried every cycle until the 200-row drop. Fix: one SaveChanges and a client-generated GUID deduped server-side.
- **Scoreboard.Client/Components/Pages/Home.razor:868-890.** `NewGame` never clears `ScoreEventSet`, so events from an unfinished game leak into the next game's buckets. `_gameStartedAt` (:810) is not persisted, so a restart mid-match skews buckets (clamped at :853).
- **Scoreboard.Client/Components/Pages/Home.razor:472-486 with Services/BoardSessionGuard.cs:~24 (`ForceAcquire`).** Takeover never tells the old circuit; it keeps its timers, key handling and saves on the same db. Fix: raise an event to the old circuit.
- **Scoreboard.Client/Services/SystemPowerService.cs:73 and :77-85.** `IsShuttingDown` is set and never reset. On non-Linux or in a container the method returns after "skipping", so the flag stays true; sync and pull then no-op forever (RemoteSyncService.cs:69, RemotePullService.cs:82) and Home ignores keys (Home.razor:896). `_heldKeys` (:11) and `_holdCts` (:12) are mutated unlocked from several circuits in a singleton.
- **Unbounded queries.** Services/StatsService.cs:142-166 loads every `MatchStat` for the org, plus buckets on the detail page, on each page view. PlayerService.cs:8-17 `ListForOrganizationAsync` is unbounded and `Include(User)` loads PasswordHash; it feeds every full snapshot (ScoreboardDataService.cs:108-112) and the Players page. Fix: project and page, drop the User include.
- **Scoreboard.WebApp/Components/Pages/Dashboard.razor:385-387 and :527.** A 1s `Timer` calls `_ = InvokeAsync(OnTickAsync)`. Every 10th tick runs `LoadAsync` (7 scoped DB round-trips) and a slow load can overlap the next tick. `Dispose` only disposes the timer, so an in-flight callback can hit a disposed renderer. Fix: `PeriodicTimer` with a CTS.
- **Unhandled handler exceptions crash the circuit.** Players.razor:454-466 `DeleteAsync` has no catch, and PlayerService.cs:223 throws `InvalidOperationException` for system players. Players.razor:417-424 `OnPhotoSelectedAsync` throws on files over 5 MB. Most pages catch only `ArgumentException` or `InvalidOperationException`, so `DbUpdateException` from unique-index races (ShortcutNumber PlayerService.cs:108-112, email AuthService.cs:107) escapes. Fix: ErrorBoundary and a central catch.
- **JWT stays valid after revoke.** Access tokens live 30 min and are not checked against the refresh token or user status (Program.cs:106-122), so they survive password change or deactivation.
- **Mobile refresh race.** Two concurrent refreshes with the same token trigger reuse-detection `RevokeAllAsync` and log the user out everywhere (MobileAuthService.cs:55-59).

### LOW
- Mobile profile inputs are not length-checked, and a duplicate Phone gives a 500 (MobileAuthService.cs:158-173).
- RefreshToken and PlayerInvite rows are never purged.
- AuthEndpoints.cs:24 puts the email in the redirect query string (also :103).
- Antiforgery is disabled on /login, /logout, /register and /culture (AuthEndpoints.cs:37, :81, :109, :115; Program.cs:265). The Lax cookie mitigates it.
- The `bq_pick` cookie (AuthEndpoints.cs:29) has no `Secure` flag.
- `ClubService.EnsureDefaultClubAsync` (ClubService.cs:48) can race and create two default clubs.
- Home.razor:395 and :655 start fire-and-forget `ShowNoticeAsync`, which does `Task.Delay(3500)` then `StateHasChanged` without a token; can throw after disposal.
- Home.razor:1104 and :1121 can double-import the kiosk.js module through concurrent `_kiosk ??=`.
- Scoreboard.Client/Components/Pages/WarmUp.razor:112 creates `_autoReturnCts` linked sources that are never disposed; WarmUp.razor:94 decrements 0.1 per tick, so it drifts.
- Home.razor:1019 lets `CurrentPoints--` go negative.
- RemotePullService.cs:305-306 (`ToDictionary` throws on a duplicate system slot) and :422 (an absolute `PhotoPath` from the server overrides the base URI) trust server data.
- Program.cs:169-197 runs the seed accounts in every environment if they are configured; the dev file holds passwords (appsettings.Development.json).

## A. Unused code (verified by grep across both projects, razor, js and json)
- Scoreboard.WebApp/Services/AuthService.cs:16 `LoginAsync` is never called.
- Services/Format.cs:73 `Format.PaymentMethod` is never used.
- Services/AvatarGenerator.cs:40 and Scoreboard.Client/Services/AvatarGenerator.cs:40: `SeedFromName` unused in both; WebApp `GetColor` (:38) unused in WebApp.
- Scoreboard.Client/wwwroot/js/kiosk.js:8 `isFullscreen` unused.
- Scoreboard.Client/Scoreboard.Client.csproj:25 the `Blazor.LocalStorage` package is never referenced.
- Scoreboard.Client/Services/LocalizationService.cs:9 the `LanguageChanged` event has no subscribers; Home.razor:685 `Adjust` and :1167 `L.LanguageChanged -= Adjust` are dead.
- Domain/Common.cs:11 `ITenantScoped` is a marker only, never used as a constraint; twelve entities implement it.
- Domain/Organization.cs:13 `User.IsPlatformAdmin` is only written (Program.cs:184, :195), never read; platform-admin status is actually `OrganizationId == null`.
- Domain/Mobile.cs:11 `RefreshToken.LastUsedAt` is set once and never read or updated.
- Domain/Scoring.cs:21 `RuleSet.ToSnapshot` is unused.
- Responses/PlayerDto.cs:5 `ClubId` is always passed null; the Client's `RemotePlayer` ignores it.
- Unused `HttpContext context` parameters: ClubsEndpoints.cs:17, :30; TeamsEndpoints.cs:17, :30, :33, :46; PlayersEndpoints.cs:19, :36.
- Unused usings: OrganizationEndpoints.cs:1-3; the `Middlewares` using in ClubsEndpoints.cs:1 and TeamsEndpoints.cs:1.
- Unused locals `var orgId`: Dashboard.razor:408 and :456; Cities.razor:129 and :141.
- Schema-only DbSets (removal needs a migration), Data/DataContext.cs lines 22, 37, 40, 43, 45, 50, 52, 54-57, 59-62, 68-72, 74-77: Device, ClubMembership, CustomerMembership, Reservation, SessionPlayer, CashRegisterShift, RuleSet, Match*, Tournament*, StageStanding, League, Season*, TeamFixture, PlayerStats, PlayerOrganizationStats, RatingHistory, AuditLog.
- Unused localisation keys (none referenced in any .cs or .razor; the same 40 keys in en and nl): Localization/en.json and nl.json lines 52, 65, 66, 82, 105, 121, 145, 205, 214, 223, 238, 252, 255, 256, 264, 285, 287, 288, 315, 356 (old device-pairing strings); en.geo.json and nl.geo.json lines 7, 8, 9, 11, 13, 14, 18, 19, 21, 24, 25, 27, 28, 31; en.regions.json and nl.regions.json lines 5, 6, 7, 9, 10, 12.

## B. Duplicate code
- **AvatarGenerator, 80 lines, identical** in WebApp and Client (only the namespace differs). The Client csproj already links animals.json from WebApp, so link or share this file too.
- **Entity-to-DTO mapping, about 25 lines:** PlayersEndpoints.cs:51-61 repeats ScoreboardDataService.cs:169-176; TeamsEndpoints.cs:55-59 repeats ScoreboardDataService.cs:163-167; ClubsEndpoints.cs:45 is a pass-through wrapper.
- **"Language of current org" query, 5 copies:** ScoreboardDataService.cs:158, StatsService.cs:67 and :119, PlayersEndpoints.cs:27, SystemPlayerService. The org fetch `FirstAsync(o => o.Id == db.CurrentOrganizationId)` repeats at ClubService.cs:59, PricingService.cs:89, ScoreboardDataService.cs:12, TableService.cs:20 and :134. Put both in `OrganizationService`.
- `DefaultShortName`: ClubService.cs:81 and TeamService.cs:145.
- Currency symbol switch: Format.cs:13 and :19.
- Geo CRUD: CountryService, CityService, RegionService and AssociationService repeat the same `ListAsync`, `AddAsync` and `DeleteAsync` shapes.
- Razor `RunAsync` try/catch/finally boilerplate (about 15 lines) in 11 pages: Cities, Staff, Products, Pricing, Tournaments, TournamentDetail, Tables, Dashboard, Clubs, Teams, Associations.
- Endpoint try/catch to `BadRequest`: about 10 lines x 9 in MobileEndpoints.cs:40-165 and about 6 lines each in Clubs, Teams and Players endpoints. Use a shared exception filter.
- `IsLocal` logic: Program.cs:263 and AuthEndpoints.cs:143.
- Player name-split and validation: PlayerService.cs:97-100 and :131-136 repeat :168-180.
- Client sync services: HttpClient and header setup in RemotePullService.cs:47-55 and RemoteSyncService.cs:38-41; the Upsert Club/Team/Player pattern at RemotePullService.cs:252-296; get-or-add Setting at :177, :199 and :214.
- Home.razor and WarmUp.razor: the `requestFullscreen` import, the PeriodicTimer loop and the key maps.

## D. Other issues
- **Scoreboard.Client/Components/Pages/Home.razor:206 (and :183, :197, :223, :224, :281).** `L.T("Player2Default")`, `"P1"` and `"P2"` are missing from all three Client catalogs; the UI shows the raw key text.
- Home.razor:242, :250, :251: "Language", "New Game" and "End Game" are hard-coded English.
- Home.razor:883-884: `NewGame` hard-codes player ids 1/2, while `ClearRosterPlayer` uses `DefaultPlayerId` (:524).
- Home.razor:621 and :738: `async Task` methods with no `await` (CS1998).
- Home.razor:552-565: `ShouldRender` has side effects and always returns true.
- Scoreboard.Client/Services/DbInitializerExtension.cs:8: `InitializeDbAsync` is fully synchronous and misnamed.
- SystemPowerService.cs:88-90 runs `PRAGMA wal_checkpoint`, but nothing sets WAL mode; it is a no-op and the rollback journal makes lock contention worse. Scoreboard.Client/Program.cs:49-56 also uses `Directory.GetCurrentDirectory()` instead of the content root.
- PlayerService.cs:143-147 vs :151-152: the structured country/city sets `Nationality` and `City`, then the free-text values overwrite them.
- PlayersEndpoints.cs:19-28: the POST returns a tracked player without Association or Region includes, so `AssociationName` and `RegionName` come back null, unlike the list endpoint.
- Members with `LeftAt` set are returned to kiosks: TeamService.cs:8-17 and :19-27 include them, while StatsService.cs:137 and CupService.cs:31 filter `LeftAt == null`.
- Services/Tournaments/CupLogic.cs:150-155: byes count as `Won` and add points but not `Played`.
- Services/Tournaments/CupService.cs:437: `DateTime.Today` uses server-local time, not the org time zone. :431 `CompleteIfDoneAsync` returns a `Task` for synchronous work.
- Localization: the keys "G", "M", "El" and "Temizle" are defined in more than one catalog file per language; the later file silently wins.
- Security/JwtSettings.cs:22: `SigningKey` builds a new `SymmetricSecurityKey` on each access.
- Unverified: CityService.cs:11 and RegionService.cs use `Include(...).ThenInclude(Country)` and then `Select(x => x.City)`; EF Core may drop the include, leaving `c.Country` null.
- Unverified: Scoreboard.Client/Components/App.razor `<Routes @rendermode="InteractiveServer" />` uses the default prerender; if prerender is on, `Home.OnInitializedAsync` runs twice per load.
