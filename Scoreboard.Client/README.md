# Zeymera Scoreboard

A kiosk-style digital scoreboard for billiards, built on Blazor Server.

## Projects

| Project | What it is |
|---|---|
| `Scoreboard.Client` | The scoreboard display itself. Blazor Server app (net10.0), state persisted via EF Core/SQLite. This is what you run on the machine driving the screen. |
| `Scoreboard.WebApp` | ASP.NET Core Blazor Web App backed by PostgreSQL (EF Core). Hosts an admin UI for managing clubs/teams/players, plus a minimal API that receives what a kiosk's `RemoteSyncService` pushes it — clients, players, match stats — see "Remote data push" below. |

## Running the scoreboard

```
cd Scoreboard.Client
dotnet run
```

Opens at `http://localhost:5288` by default (see `Properties/launchSettings.json`). On first run it creates `wwwroot/Db/scoreboard.db` (SQLite) and seeds a default game state.

Click anywhere on the board to enter fullscreen kiosk mode.

## Keyboard shortcuts

Every shortcut also has a numpad-friendly alternate, so a bare numeric keypad (no letter keys) can drive the whole board — including its NumLock-off equivalent, since the keypad's NumLock state isn't something the app controls.

| Key | Numpad alternate | NumLock-off equivalent | Action |
|---|---|---|---|
| `C` | `9` | Page Up | Show/hide the controls overlay |
| `T` | `3` | Page Down | Start/stop the shot clock |
| `R` | `6` | Right Arrow | Reset the shot clock |
| `1` / `2` | *(already numeric)* | | Set active player |
| `P` | `0` | Insert | Open the player picker for whichever player is currently active |
| `W` | `5` | | Open the warm-up duration picker |
| `+` / `-` | *(already numeric)* | | Adjust the current-points counter |
| `E` | `4` | Left Arrow | End Game |
| `N` | `8` | Up Arrow | New Game |

The warm-up timer page (`/warmup/{minutes}`) has its own shortcuts: `T`/`3`/Page Down to pause/resume, `R`/`6`/Right Arrow to reset, `Enter` to restart, `C`/`Esc`/`9`/Page Up to return to the board.

There's also a hold gesture, not a tap: `NumLock` + `Insert`/`Delete`, held together for 4 seconds, reboots or shuts down the machine — see "System power" below.

## System power (reboot/shutdown)

Holding `NumLock` + `Insert` for 4 seconds reboots the machine the app is running on; holding `NumLock` + `Delete` for 4 seconds shuts it down. This is meant for a numpad-only remote (no letter keys), where `Insert`/`Delete` are what a numpad's `0`/`.` keys send while `NumLock` is off — the same reasoning behind the "NumLock-off equivalent" column above. `NumLock` itself is only ever app-relevant here — no other shortcut checks its state, `SystemPowerService`'s `IsHeld("NumLock")` is only ever paired with `Insert`/`Delete`.

Pressing the physical `NumLock` key toggles its on/off state at the OS level, outside the app's control — so starting this gesture (even one released well before 4 seconds) flips what every other numpad shortcut sends for the rest of the session (the "NumLock-off equivalent" column above only applies in one of the two states). If the numpad-driven remote starts responding to the wrong keys after someone reaches for `Insert`/`Delete`, that's why — toggle `NumLock` back (or just retrain the remote/board on the new state) rather than treating it as a bug.

`Services/SystemPowerService.cs` tracks held keys itself (`Home.razor`'s `HandleKeyDown`/`HandleKeyUp` just report every keydown/keyup to it) and starts a 4-second timer the moment both keys of a combo are down together, cancelling it if either is released early. Once the hold completes:

1. `IsShuttingDown` is set, which makes `ApplyCommandsAsync` (so every keyboard *and* WebSocket command), the shot clock's tick, and `RemoteSyncService`/`RemotePullService`/`RemoteWsPushService`'s background ticks all become no-ops — nothing new gets written to the database from this point on.
2. The SQLite WAL is checkpointed (`PRAGMA wal_checkpoint(TRUNCATE)`) and pooled connections are cleared, so the database file is in a clean, fully-flushed state before the machine actually goes down.
3. `sudo systemctl reboot` or `sudo systemctl poweroff` is invoked. On anything other than Linux (e.g. running `dotnet run` on a Windows dev machine) this step is skipped — steps 1–2 still happen, so the behavior can be tested end-to-end without actually rebooting a Windows machine.

Step 3 needs the app's service user to be able to run those two `systemctl` commands without a password prompt — see `Scoreboard.Client/deploy/pi/install-power-sudoers.sh` and "Raspberry Pi kiosk deployment" below. Without that one-time setup, `sudo` itself will fail (no TTY to prompt on) and the machine won't actually reboot/shut down — but steps 1–2 already happened by then, and the app process itself keeps running normally either way, since `Process.Start` here is fire-and-forget and never awaited.

## Remote control input

The scoreboard accepts the same commands from two sources. Every one of them ends up calling `Home.razor`'s `ApplyCommandsAsync` with a list of one or more `ScoreboardCommandMessage`s — that's the *only* place that ever saves to the database or broadcasts, applying every command in the list first and persisting/broadcasting exactly once at the end, regardless of how many commands arrived together. The on-screen buttons dispatch through this same path too (via a small `DispatchAsync(command, payload)` helper) rather than mutating state directly, so a button click and a remote command behave identically.

1. **Keyboard**, as above — each keypress becomes a one-command list.
2. **WebSocket** — a control app connects to `ws://<host>:<port>/ws` and sends a JSON envelope as a UTF-8 text frame. `Models/ScoreboardCommandParser.cs` accepts three shapes:

   - **Single command**: `{"command":"...", "payload": ...}`.
   - **Multiple commands in one message** — put them in a `commands` array instead, applied in array order: `{"commands":[{"command":"..."},{"command":"...","payload":...}]}`. This is the recommended shape for a control app, since it always looks the same whether you're sending one command or several, and it guarantees the board only saves/broadcasts once for the whole message instead of once per command.
   - **Bare command name** with no payload, as plain (non-JSON) text — e.g. just `IncrementPoints`.

   A message that doesn't match any of these (wrong field names, an unrecognized command name, etc.) is silently dropped rather than guessed at — earlier versions of this parser deserialized straight into the command record, so a mismatched shape like `{"type":"...","value":...}` (no `"command"` field) would silently resolve to `ToggleControls`, the first `ScoreboardCommand` enum member, instead of doing nothing. Within a `commands` array, one invalid entry is skipped, not a reason to reject the rest.

   **Command reference** (`command` is a `ScoreboardCommand` enum name, case-insensitive):

   | Payload | Commands |
   |---|---|
   | *(none)* | `ToggleControls`, `ToggleShotClock`, `ResetShotClock`, `SelectPlayer1`, `SelectPlayer2`, `IncrementPoints`, `DecrementPoints`, `CommitPoints`, `ClearRosterPlayer1`, `ClearRosterPlayer2`, `EndGame`, `NewGame` |
   | number | `AdjustPoints` — the absolute tally value, not a delta (see below); `SetMatchTarget` — the target score; `SelectRosterPlayer1`, `SelectRosterPlayer2` — a roster player's `Id` to link to that slot; `WarmUp` — the chosen duration in minutes, navigates the board to the full-screen warm-up timer at `/warmup/<minutes>` |
   | string | `RenamePlayer1`, `RenamePlayer2` — the free-typed name |
   | object | `UpsertPlayer` (alias: `AddPlayer`) — see below; `AddTeam` — see below |

   ```json
   {"command":"IncrementPoints"}
   {"command":"AdjustPoints","payload":3}
   {"command":"RenamePlayer1","payload":"Hayri"}
   {"command":"SetMatchTarget","payload":40}
   {"command":"SelectRosterPlayer1","payload":7}
   {"commands":[{"command":"SelectPlayer1"},{"command":"AdjustPoints","payload":3},{"command":"CommitPoints"}]}
   ```

   There's a single score input, not one per player: the "Current Points" counter (`IncrementPoints`/`DecrementPoints`/`AdjustPoints`) always applies to whichever player is currently active (`ActivePlayer`, set via `SelectPlayer1`/`SelectPlayer2`). Despite the name, `AdjustPoints` takes the tally's **absolute value**, not a delta — send `payload: 3` to mean "the tally is 3", not "add 3" (the command name predates this and was kept as-is to avoid a breaking change for existing remotes). This is deliberate: a remote that tracks its own running tally and pushes it on every change is far more resilient to a flaky connection than one that sends deltas — a lost or duplicated absolute update self-corrects on the next message, while a lost delta permanently desyncs the two sides. It only tallies a pending count — nothing is written to a real score until `CommitPoints` (Enter key, or the on-screen "Add" button) applies that tally to the active player's score, updates their high run, resets the counter to 0, hands the turn to the other player, and — only when Player 1 was the active player — advances `Inning`. `Inning` never decrements; correct a bad tally before committing it with `DecrementPoints`/`AdjustPoints`, not by rolling the inning back after the fact.

   `UpsertPlayer` (also accepted as `AddPlayer`, same command) adds or updates a row in the local `player` roster (see "Player roster" below) — the photo is optional and only needs sending when it changes. `avatar` is an arbitrary string hashed into one of the generated avatars (the same string always maps to the same avatar); omit it to get a random avatar on creation, or leave an existing player's avatar unchanged. `teamId` links the player to a row added via `AddTeam`. `shortcutNumber` is the number a keyboard/remote can type into the free-text name field to look this player up (distinct from `id`):
   ```json
   {"command":"UpsertPlayer","payload":{"id":7,"nickname":"Hayri","name":"Hayri Ozler","photoBase64":"<base64 jpeg bytes>","photoExtension":"jpg","avatar":"eagle","teamId":1,"shortcutNumber":12}}
   {"command":"AddPlayer","payload":{"id":3,"nickName":"Ali","name":"Ali Yılmaz","avatar":"eagle","teamId":1,"shortcutNumber":13}}
   ```

   `AddTeam` adds or updates a row in the local `team` table by `id`:
   ```json
   {"command":"AddTeam","payload":{"id":1,"name":"Kartallar"}}
   ```

   **C# example** (MAUI or any .NET client, using `System.Net.WebSockets.ClientWebSocket`):
   ```csharp
   private ClientWebSocket _socket = new();

   private async Task ConnectAsync(string host) =>
       await _socket.ConnectAsync(new Uri($"ws://{host}:5288/ws"), CancellationToken.None);

   private async Task SendCommandAsync(string command, object? payload = null)
   {
       var envelope = payload is null ? new { command } : new { command, payload };
       var json = JsonSerializer.Serialize(envelope);
       await _socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
   }

   // usage:
   await SendCommandAsync("SelectPlayer1");
   await SendCommandAsync("RenamePlayer1", "Hayri");
   await SendCommandAsync("SetMatchTarget", 40);
   ```

   **Receiving state** — the server does **not** push anything the instant a client connects; connecting only opens the socket. After **every** change to the board (from any source — this client, another connected client, the keyboard, the on-screen controls) it pushes a snapshot to every connected client, and a client can also explicitly ask for one at any time (e.g. right after connecting, once it's actually ready to handle the reply) with the no-payload `GetState` command:
   ```json
   {"commands":[{"command":"GetState"}]}
   ```
   `{"type":"state","commands":[]}` — an empty `commands` array alongside `"type":"state"` — is also accepted as a `GetState` request, since that's the shape some clients send. Either way, what comes back looks like this — broadcast to **every** connected client, not just the one that asked:
   ```json
   {"type":"state","state":{"player1DisplayName":"Hayri","player2DisplayName":"Player 2","player1Score":13,"player2Score":11,"player1Avg":1.857,"player2Avg":1.571,"player1HighRun":5,"player2HighRun":3,"activePlayer":2,"currentPoints":0,"inning":7,"matchTarget":25,"shotClockActive":false,"shotClockSeconds":40,"shotClockRemaining":40}}
   ```
   Display names are already resolved (roster `Nickname`, falling back to `Name`, falling back to the free-typed name, falling back to `"Player 1"`/`"Player 2"`) — no need to separately resolve `Player1Id`/`Player2Id` against the roster. A well-behaved client should treat this push, not its own sent commands, as the source of truth for what to render — check `type == "state"` on every received message and refresh the UI from `state`. This request/response design (over auto-pushing on connect) exists specifically so a client isn't racing its own message-handler setup against the server's connect-time push — it asks only once it's actually ready.

   Quick test from a browser console **on the scoreboard page itself** (a live circuit needs to be open for anything to receive the command — a bare `curl`/script connection with no rendered page won't do anything):
   ```js
   const ws = new WebSocket("ws://localhost:5288/ws");
   ws.onopen = () => console.log("connected");
   ws.onmessage = (e) => console.log("received:", e.data); // state snapshot, on connect and after every change
   ws.send(JSON.stringify({ command: "RenamePlayer1", payload: "Hayri" }));
   ws.send("IncrementPoints");
   ws.send(JSON.stringify({ commands: [{ command: "SelectPlayer1" }, { command: "AdjustPoints", payload: 3 }, { command: "CommitPoints" }] }));
   ```
   The sync-dot on the board reflects whether a control connection is currently active.

## Player roster

The `player` table (SQLite, same database as the scoreboard state) is a local roster independent of `Scoreboard.WebApp` — `Id` is auto-increment, plus `Nickname`, `Name`, `ShortcutNumber` (optional, used for the free-text lookup below — not the same as `Id`), and `PhotoPath` (a relative path under `wwwroot/Players/`; the photo bytes live on disk, not in the DB). The remote `UpsertPlayer` command may still pass an explicit `id` (e.g. to match a MAUI app's own id scheme) — EF/SQLite only auto-assign when the value is left at its default, so both paths work side by side.

- **`/players`** — a page (linked from the board's controls overlay) to add/edit/delete roster players by hand, with a photo upload and a shortcut number.
- **Assigning a roster player to the board** — press `P` (or use the "Choose Player" button per slot in the controls overlay) to open a picker dialog listing the roster; picking one links that slot to the player's `Id` (`ScoreboardState.Player1Id`/`Player2Id`). Once linked, the board displays that player's `Nickname` (falling back to `Name` if the nickname is empty) and their photo next to the name, instead of the free-typed `Player1Name`/`Player2Name`. "Use manual name instead" in the dialog unlinks it and reverts to the free-text name. Typing a number into the free-text name field is also treated as a lookup — if a roster player has that number as their `ShortcutNumber`, that player is linked instead of using the digits as a literal name.
- **`UpsertPlayer`** over WebSocket (see above) adds/updates roster entries remotely — e.g. from the MAUI companion app.

## Match history

Ending a match is two separate steps, both in the controls overlay:

- **"End Game"** snapshots the current game into the `match_result` table — date/time played, both players' display names (a copy, not just a `PlayerId` reference, so history stays readable even if that roster player is later renamed or deleted), scores, averages, high runs, innings played, the match target, and the winner. It does **not** reset the board — the final score stays on screen (e.g. for a photo/announcement).
- **"New Game"** resets the board for the next match. It does **not** save anything — press "End Game" first if the current match's stats should be kept. `MatchTarget` is preserved across "New Game" (it's treated as a match-format setting, not per-game state).

View history at **`/matches`**, linked from the controls overlay, with the winner's name highlighted.

Both are also `ScoreboardCommand`s (`EndGame`, `NewGame`), so either can be triggered remotely over WebSocket as well as the on-screen buttons. Neither has a confirmation dialog — both fire immediately, locally or remotely.

The match target itself (`Current.MatchTarget`, default 20 on a brand-new database) can be edited directly in the controls overlay, or set remotely with `SetMatchTarget` (see above).

## Remote data push

`Scoreboard.WebApp` is backed by PostgreSQL (EF Core, `Npgsql`) — six resources: `Club` (a venue, e.g. a billiards hall — `Endpoints/ClubsEndpoints.cs`), `ScoreboardClient` (one per monitor/kiosk at that club, optionally linked to a `Club` via `ClubId` + which physical `TableNumber` it's showing — `Endpoints/ScoreboardClientsEndpoints.cs`), `Player` and `Team` (owned directly by a `Club`, managed either through the admin UI at `/clubs` or the API below), `MatchStat` (a finished match's final stats, still keyed by `ClientId` + the kiosk's own local ids), and `TeamPlayer` (the join table linking a `Team` to its member `Player`s). `Services/ClubService.cs`, `Services/PlayerService.cs` and `Services/TeamService.cs` hold the actual CRUD logic and are shared between the Blazor pages (`Components/Pages/`) and the minimal API endpoints below, so the two never drift apart. No league matches, no live-scoreboard mirror — those were removed as unnecessary for the current scope. `Scoreboard.WebApp.http` at the project root has ready-to-run requests for all of this (VS Code's REST Client extension or Visual Studio's built-in `.http` support) — it walks through creating a club, registering clients for it, then pushing players/stats/teams using the resulting client id.

`POST /api/clubs` generates its own `Code` (via `Data/PairingCodeGenerator.cs`, the same generator `POST /api/clients` uses) and returns it in the response — a short random code identifying the club itself, distinct from any individual kiosk's own `ScoreboardClient.Id`. That same `Code` is also the club's login credential for the admin UI (see below).

`Player`/`Team` are scoped to whichever `Club` the calling `ScoreboardClient` is linked to (via `X-Client-Id` → `ScoreboardClient.ClubId`) and upserted by their own `Id` — pass `0` to create, or an existing id to update (`POST /api/players`/`POST /api/teams`). `PUT /api/teams/{id}/players` replaces a team's full roster in one call, given the member players' ids: `{"playerIds":[1,2]}`. `DELETE /api/teams/{id}/players/{playerId}` removes a single member without touching the rest of the roster. All of these require the same `X-Client-Id` header as `/api/stats`, and the client must already be linked to a club (`ClubId` set via `POST /api/clients` or the admin UI).

`Services/RemoteSyncService.cs` (Client project) is a `BackgroundService` that periodically POSTs local data to the Api, configured under `RemoteSync` in the Client's `appsettings.json`:

```json
"RemoteSync": {
  "Enabled": false,
  "ClientId": "",
  "BaseUrl": "",
  "PollSeconds": 15
}
```

`Enabled` is the master switch — leave it `false` (the default) to skip remote push entirely, e.g. on a kiosk with no network reachable from it. `BaseUrl` is the WebApp's base address, e.g. `"http://192.168.1.50:5000/"` — the client POSTs `players`/`stats` relative to it. `ClientId` is a code obtained by first calling `POST /api/clients` on the WebApp once (see `Endpoints/ScoreboardClientsEndpoints.cs`) to register this kiosk; every push is validated by `Middlewares/ClientIdMiddleware.cs`, which requires that code in an `X-Client-Id` header on every `/api/*` request (except `/api/clients`/`/api/clubs` themselves, for bootstrapping).

Every `PollSeconds` (default 15), the service checks for unsent rows and pushes them. The whole background service — config parsing, the HTTP calls, everything — is wrapped so it can never crash the app: a bad `BaseUrl` just disables the feature for that run, and a failed push (server down, network blip) leaves the row as-is for the next tick to retry, with no separate retry/backoff bookkeeping needed:

- **`POST players`** — any `player` row where `Synced = false` (new column, default `false`/`0`). Body: `{ id, nickname, name, avatarId, photoBase64, photoExtension }` — the photo is read off disk and base64-encoded only when the player has one (`PhotoPath` set); the Api saves it under its own `wwwroot/Players/<clientId>/` and serves it back out via `UseStaticFiles()`. Marks `Synced = true` on success.
- **`POST stats`** — every `match_result` row (i.e. every finished match from "End Game" not yet sent — see "Match history" above). Body: `{ player1Id, player1Name, player1Score, player1Avg, player1HighRun, player2Id, player2Name, player2Score, player2Avg, player2HighRun, inning, matchTarget, winner, playedAt }`. **Deletes the row on success** rather than marking a flag — the Api becomes the durable store for match history, and local SQLite is only a buffer for matches it hasn't received yet. This means `/matches` will stop showing a match shortly after it's pushed, once `RemoteSync` is enabled.

On the WebApp side, `POST /api/players` upserts on the player's own `Id` (`0` creates, non-zero updates) rather than insert-only, so a retried push after a lost response updates the existing row instead of duplicating it — though the kiosk's remote sync flow currently expects the pre-refactor upsert-by-a-kiosk-chosen-id semantics, so `RemoteSyncService` needs revisiting before this is actually enabled end to end. `POST /api/stats` is a plain insert — a lost-response retry can create a duplicate stat row, which is an acceptable tradeoff for a scoreboard history log.

`Scoreboard.WebApp` has no EF Core migrations applied anywhere yet (no local Postgres was reachable to test against) — run `dotnet ef database update` from `Scoreboard.WebApp/` against wherever Postgres actually lives before relying on any of this.

## Club/team/player admin UI

`Scoreboard.WebApp` also serves a small Blazor Server admin UI (same project, same process as the API — not a separate app), gated behind a cookie login so one club can never see another's roster:

- **`/clubs`** — public. Register a new club; shows the generated `Code` once (also usable as `ClientId` filtering for `/api/clients?clubId=`).
- **`/login`** — public. Enter a club's `Code` to sign in (`Endpoints/ClubAuthEndpoints.cs`, plain `POST /login` cookie sign-in, not a Blazor-bound form, since Blazor Server circuits can't write auth cookies mid-circuit).
- **`/players`** and **`/teams`** — `[Authorize]`. Manage the signed-in club's players/teams. The club id is read from the auth cookie's `ClubId` claim, never from a route parameter or client input — every `Services/*.cs` query is scoped by that claim, so there's no way to reach another club's data through the UI even by guessing an id.

These pages call the same `Services/ClubService.cs`/`PlayerService.cs`/`TeamService.cs` the `/api/*` endpoints use, via direct `ScoreboardDbContext` access (no HTTP round-trip to itself).

## Raspberry Pi kiosk deployment

`Scoreboard.Client/deploy/pi/` has everything for running the Client app as a kiosk on a Raspberry Pi — the systemd service, the .NET runtime install script, the Chromium kiosk launcher, the autostart wiring, and a one-command publish+deploy script for Windows. See `Scoreboard.Client/deploy/pi/README.md` for the full one-time setup and the routine redeploy command.

## Logs

The Client app logs via Serilog to both the console and a rolling daily file under `logs/` (relative to wherever it's running — `Scoreboard.Client/deploy/pi/README.md` covers the Pi path). Every WS connect/disconnect, received/sent message, and parsed command is logged with the sender's IP and a shared, monotonically increasing sequence number (`[#N]`) so you can line up exactly what came in against what went out, in order, across reconnects. `Services/ScoreboardCommandHub.cs` logs the hub side (registration, sends, broadcasts); `Endpoints/ScoreboardWebSocketEndpoint.cs`'s `/ws` handler logs the raw receive side. EF Core's and ASP.NET Core's own request/query logs are dialed down to `Warning` so the file stays focused on actual board traffic instead of SQL noise.

## Known gaps

- `Services/WebSocketService.cs` in the Client project is an older outbound `ClientWebSocket` stub, superseded by the inbound `/ws` endpoint + `ScoreboardCommandHub` design above; currently unused.
- No formal EF Core migrations — schema changes are patched in at startup in `Program.cs` (`CREATE TABLE IF NOT EXISTS` / `EnsureColumn`) since `Database.EnsureCreated()` is a no-op once the db file exists. Fine for now, but if the schema keeps growing, switching to real migrations would remove the need for this pattern.
- The `/ws` state broadcast (see "Remote control input" above) does **not** fire on the shot clock's per-tick countdown (every 100ms) — only on discrete state changes (a command applied, the clock naturally expiring, etc.), to avoid flooding connected clients. `shotClockRemaining` in the snapshot is accurate at the moment it's sent, but a control app won't see it counting down live between those discrete pushes, only jump when it starts/stops/resets/expires.
- `SystemPowerService.IsShuttingDown` (see "System power" above) never gets reset once set — if the `sudo systemctl reboot`/`poweroff` call fails (e.g. the sudoers rule from `Scoreboard.Client/deploy/pi/install-power-sudoers.sh` was never installed), the board is left permanently ignoring every command, keyboard and WebSocket alike, with no recovery except restarting the app process. There's no timeout/rollback if the actual OS-level reboot doesn't happen.
