# Zeymera Scoreboard

A kiosk-style digital scoreboard for billiards, built on Blazor Server.

## Projects

| Project | What it is |
|---|---|
| `Scoreboard.Client` | The scoreboard display itself. Blazor Server app (net10.0), state persisted via EF Core/SQLite. This is what you run on the machine driving the screen. |
| `Scoreboard.WebApp` | ASP.NET Core Blazor Web App backed by PostgreSQL (EF Core). Hosts the admin UI for clubs/teams/players/tables/pricing and the minimal `/api/*` the scoreboards talk to: they push finished match results to it and pull clubs, teams and players from it — see "Server sync". |

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

1. `IsShuttingDown` is set, which makes `ApplyCommandsAsync` (every keyboard command and on-screen button), the shot clock's tick, and `RemoteSyncService`/`RemotePullService`'s background ticks all become no-ops — nothing new gets written to the database from this point on.
2. The SQLite WAL is checkpointed (`PRAGMA wal_checkpoint(TRUNCATE)`) and pooled connections are cleared, so the database file is in a clean, fully-flushed state before the machine actually goes down.
3. `sudo systemctl reboot` or `sudo systemctl poweroff` is invoked. On anything other than Linux (e.g. running `dotnet run` on a Windows dev machine) this step is skipped — steps 1–2 still happen, so the behavior can be tested end-to-end without actually rebooting a Windows machine.

Step 3 needs the app's service user to be able to run those two `systemctl` commands without a password prompt — see `Scoreboard.Client/deploy/pi/install-power-sudoers.sh` and "Raspberry Pi kiosk deployment" below. Without that one-time setup, `sudo` itself will fail (no TTY to prompt on) and the machine won't actually reboot/shut down — but steps 1–2 already happened by then, and the app process itself keeps running normally either way, since `Process.Start` here is fire-and-forget and never awaited.

## Controls

The board is driven from the keyboard (see the table above) and the on-screen buttons of the controls overlay. Both end up in `Home.razor`'s `ApplyCommandsAsync`, the *only* place that saves to the database: it applies every command in a list first, then persists once. The on-screen buttons dispatch through the same path (`DispatchAsync(command, payload)`) rather than mutating state directly, so a button click and a key press behave identically. `Models/ScoreboardCommand.cs` lists the commands.

There is no network control channel any more: the WebSocket `/ws` endpoint, the command hub and the control-app protocol were removed. Everything that reaches the server goes through the HTTP sync described under "Server sync".

## Clubs, teams and players (read-only mirror)

The `club`, `team` and `player` tables (SQLite, same database as the scoreboard state) are a **read-only mirror** of what the server has for this organization. They are never edited on the board; `RemotePullService` overwrites them from the server every `PollSeconds`:

| Table | Columns (besides the local `Id` primary key) |
|---|---|
| `club` | `RemoteId`, `Name`, `ShortName`, `City`, `PrimaryColor` |
| `team` | `RemoteId`, `ClubId` (local `club.Id`), `Name`, `UpdatedAt` |
| `player` | `RemoteId`, `Nickname`, `Name`, `ShortcutNumber`, `PhotoPath`, `AvatarId`, `TeamId` (local `team.Id`), `Level`, `Country`, `City`, `LicenseNo`, `LicenseValidUntil`, `AssociationName`, `UpdatedAt` |

`RemoteId` is the server's id (unique when set); the local `Id` stays the key the board and `match_result` use. A player belongs to one team locally; if the server lists a player in several teams, the first wins. Rows the server no longer has are deleted. Two exceptions: the placeholder players `Id 1` and `Id 2` ("Player 1"/"Player 2", no `RemoteId`) always exist because the board needs someone in each slot when no roster player is selected, and a roster player that disappears from the server while on the board is replaced by the placeholder in that slot. Player photos are downloaded into `wwwroot/PlayerSet/`.

- **Assigning a roster player to the board** — press `P` (or use the "Choose Player" button per slot in the controls overlay) to pick from the mirrored roster. Typing a player's `ShortcutNumber` into a player name field selects that player.
- Existing kiosk databases are upgraded in place at startup (`DbInitializerExtension`): missing tables and columns are added, nothing is dropped, and settings, game state and match rows are kept.

## Match history

Ending a match is two separate steps, both in the controls overlay:

- **"End Game"** snapshots the current game into the `match_result` table — date/time played, both players' display names (a copy, not just a `PlayerId` reference, so history stays readable even if that roster player is later renamed or deleted), scores, averages, high runs, innings played, the match target, and the winner. It does **not** reset the board — the final score stays on screen (e.g. for a photo/announcement).
- **"New Game"** resets the board for the next match. It does **not** save anything — press "End Game" first if the current match's stats should be kept. `MatchTarget` is preserved across "New Game" (it's treated as a match-format setting, not per-game state).

View history at **`/matches`**, linked from the controls overlay, with the winner's name highlighted.

Both are also `ScoreboardCommand`s (`EndGame`, `NewGame`), triggered from the keyboard or the on-screen buttons. Neither has a confirmation dialog — both fire immediately.

The match target itself (`Current.MatchTarget`, default 20 on a brand-new database) can be edited directly in the controls overlay, or set remotely with `SetMatchTarget` (see above).

## Server sync

`Scoreboard.WebApp` (PostgreSQL) is the source of truth. Each table's scoreboard talks to it over plain HTTP, with **no authentication** — every request carries just two headers:

| Header | Value |
|---|---|
| `X-Client-Id` | The organization's client id (shown on the WebApp's "Connection" page; same for every table of the salon) |
| `X-Table-No` | This table's number in the organization (`RemoteSync:TableNo`). Required to send match results; optional when pulling |

Anyone who knows the client id can post match results for that salon, so keep it to your own scoreboards; it can be regenerated on the Connection page (every scoreboard then needs the new value).

```json
"RemoteSync": {
  "Enabled": false,
  "ClientId": "",
  "TableNo": 1,
  "BaseUrl": "http://server:8080/api/",
  "PollSeconds": 15
}
```

`Enabled` is the master switch; `BaseUrl` is the API root including `/api/`.

- **Push — match results only** (`Services/RemoteSyncService.cs`). Every `PollSeconds`, each `match_result` row not yet sent is `POST`ed to `stats` together with its 5-minute score distribution (`match_score_stat`). The players are sent as the **server's** ids (`RemoteId`), `null` for the placeholders. Once the server accepted a match it is deleted locally. Nothing about players, teams or clubs is ever sent. With sync disabled or misconfigured (empty `ClientId`/`TableNo`/`BaseUrl`) the service sends nothing and logs a warning, and it keeps at most the 200 newest unsent matches so the database cannot grow forever.
- **Pull — clubs, players, teams** (`Services/RemotePullService.cs`). Every `PollSeconds` it `GET`s `clubs`, `players` and `teams` and mirrors them (see above). If any of the three requests fails, that tick changes nothing.

Both services are no-ops once `SystemPowerService.IsShuttingDown` is set.

## Raspberry Pi kiosk deployment

`Scoreboard.Client/deploy/pi/` has everything for running the Client app as a kiosk on a Raspberry Pi — the systemd service, the .NET runtime install script, the Chromium kiosk launcher, the autostart wiring, and a one-command publish+deploy script for Windows. See `Scoreboard.Client/deploy/pi/README.md` for the full one-time setup and the routine redeploy command.

## Logs

The Client app logs via Serilog to both the console and a rolling daily file under `logs/` (relative to wherever it's running — `Scoreboard.Client/deploy/pi/README.md` covers the Pi path). EF Core's and ASP.NET Core's own request/query logs are dialed down to `Warning` so the file stays focused on actual board traffic instead of SQL noise.

## Known gaps

- No formal EF Core migrations — schema changes are patched in at startup in `Program.cs` (`CREATE TABLE IF NOT EXISTS` / `EnsureColumn`) since `Database.EnsureCreated()` is a no-op once the db file exists. Fine for now, but if the schema keeps growing, switching to real migrations would remove the need for this pattern.
- `SystemPowerService.IsShuttingDown` (see "System power" above) never gets reset once set — if the `sudo systemctl reboot`/`poweroff` call fails (e.g. the sudoers rule from `Scoreboard.Client/deploy/pi/install-power-sudoers.sh` was never installed), the board is left permanently ignoring every command, with no recovery except restarting the app process. There's no timeout/rollback if the actual OS-level reboot doesn't happen.
