# Raspberry Pi kiosk deployment

Target: a Pi 4/5 running 64-bit Raspberry Pi OS (Bookworm), running the Client app as a
systemd service with Chromium in kiosk mode on top, autostarted on boot.

Desktop stack this was built/tested against: `lightdm` (autologin, `autologin-session=
rpd-labwc`) → `/usr/bin/labwc-pi` → `labwc -m` — i.e. **Wayland via labwc**, not the older
X11/LXDE stack most Pi-kiosk tutorials assume. If something here doesn't fire on a
different Pi OS image, trace the actual session chain (`systemctl get-default` →
`display-manager.service` → the session's `.desktop`/`Exec=` → any wrapper script) before
assuming a script is wrong — see the comments in `launch-kiosk.sh` and `setup-autostart.sh`.

## One-time setup

Run these once, in order, on a freshly-imaged Pi (hostname `scoreboard` below, adjust to
match your own; login user is a sudoer, e.g. `admin` — modern Raspberry Pi Imager doesn't
create a default `pi` account).

1. **Install the .NET runtime** (ASP.NET Core runtime only — the app is published
   framework-dependent, no SDK needed on the Pi):
   ```bash
   scp deploy/pi/install-dotnet.sh admin@scoreboard:~/
   ssh admin@scoreboard 'chmod +x install-dotnet.sh && ./install-dotnet.sh'
   ```

2. **First publish + copy** (see "Redeploying" below — same script, just run it once now):
   ```powershell
   .\deploy\pi\publish-and-deploy.ps1
   ```
   This will stop at the sudo check the first time, since the sudoers rule is not installed
   yet — that's expected, continue to step 3.

3. **Allow the deploy script to manage the service** (stop, start, enable, install the unit
   file) without a password prompt:
   ```bash
   scp deploy/pi/install-deploy-sudoers.sh admin@scoreboard:~/
   ssh -t admin@scoreboard 'chmod +x install-deploy-sudoers.sh && ./install-deploy-sudoers.sh'
   ```
   Then run `.deploypipublish-and-deploy.ps1` again: it installs and enables
   `billiardiq-scoreboard.service` on its own. Verify it's up:
   `ssh admin@scoreboard 'curl -sSf http://localhost:5288/ > /dev/null && echo OK'`

4. **Install the kiosk's apt dependencies** (`unclutter`, `wmctrl`, `xset`, Chromium — see
   `install-kiosk-deps.sh` for why each is needed). The same script sets the Pi's time zone
   (default `Europe/Amsterdam`; pass another one as the first argument, e.g.
   `./install-kiosk-deps.sh Europe/Istanbul`) and turns NTP on, because the kiosk clock and the
   match times use the Pi's local time:
   ```bash
   scp deploy/pi/install-kiosk-deps.sh admin@scoreboard:~/
   ssh -t admin@scoreboard 'chmod +x install-kiosk-deps.sh && ./install-kiosk-deps.sh'
   ```

5. **Install the kiosk launcher and wire up autostart**:
   ```bash
   scp deploy/pi/launch-kiosk.sh deploy/pi/setup-autostart.sh admin@scoreboard:/home/admin/billiardiq-scoreboard/
   ssh -t admin@scoreboard 'chmod +x /home/admin/billiardiq-scoreboard/launch-kiosk.sh && cd /home/admin/billiardiq-scoreboard && bash setup-autostart.sh'
   ```

6. **Allow the app to reboot/shut down the Pi** (see "System power" in the main README) —
   lets it run `sudo systemctl reboot`/`poweroff` without a password prompt when the
   NumLock+Insert / NumLock+Delete keyboard hold fires:
   ```bash
   scp deploy/pi/install-power-sudoers.sh admin@scoreboard:~/
   ssh -t admin@scoreboard 'chmod +x install-power-sudoers.sh && ./install-power-sudoers.sh'
   ```
   Optional — skip it if you don't want the board able to reboot/shut down the Pi itself.
   Without it, the hold gesture still stops all board activity and checkpoints the
   database, it just can't carry out the actual reboot/shutdown (see "Known gaps" in the
   main README for what that leaves the board in).

7. **Reboot** and confirm Chromium comes up in kiosk mode on its own:
   ```bash
   ssh -t admin@scoreboard 'sudo reboot'
   ```
   If it doesn't, check `~/kiosk-autostart.log` on the Pi first — boot-time autostart
   failures are otherwise invisible (no attached terminal).

## Redeploying (after code changes)

```powershell
.\deploy\pi\publish-and-deploy.ps1
```

This publishes, strips `wwwroot/Db`/`wwwroot/Players` from the output (so it can never
overwrite the Pi's live database or uploaded player photos), then on the Pi stops the
service, deletes the previous binaries (dll, pdb, native libs, manifests, executables — never
`wwwroot`), copies the new publish over, installs `billiardiq-scoreboard.service` if it
changed, and starts the service. `launch-kiosk.sh`/`setup-autostart.sh` only need to be
re-copied if you actually change them.

The script needs `install-deploy-sudoers.sh` installed (step 3 above). Without it the
first `sudo -n` call fails with `sudo: a password is required` and the script stops before
touching anything on the Pi.

## Migrating from the old zeymera-scoreboard names

An install made before the rename lives in `/home/admin/zeymera-scoreboard` with a
`zeymera-scoreboard.service`. Move it once, keeping the live data:
```bash
scp deploy/pi/migrate-from-zeymera.sh deploy/pi/install-deploy-sudoers.sh deploy/pi/install-power-sudoers.sh admin@scoreboard:~/
ssh -t admin@scoreboard 'bash migrate-from-zeymera.sh && bash install-deploy-sudoers.sh && bash install-power-sudoers.sh'
```
Then run `.deploypipublish-and-deploy.ps1`. Remove the old `Zeymera.Scoreboard.Client.*`
files if any are left in the folder.

## Client settings on the Pi

The service runs in the Production environment, so `appsettings.Production.json` (published with
the app) points the client at `https://www.billiardiq.com/api/`. The organisation's client id is
machine-specific and is never published: put it in `appsettings.Local.json` next to the app, once.
The deploy script does not touch that file.
```bash
ssh admin@scoreboard 'cat > /home/admin/billiardiq-scoreboard/appsettings.Local.json' <<'JSON'
{ "RemoteSync": { "ClientId": "<client id from the panel>", "TableNo": 1 } }
JSON
ssh -t admin@scoreboard 'sudo systemctl restart billiardiq-scoreboard.service'
```

## Logs

The app logs to `logs/` under its working directory via Serilog (console + a rolling daily
file, `logs/scoreboard-YYYYMMDD.log`) — every WS connect/disconnect, received/sent message
(with the sender's IP and a sequence number), and parsed command. On the Pi this lands at
`/home/admin/billiardiq-scoreboard/logs/` automatically, since that's the service's
`WorkingDirectory` — no extra setup needed. Tail it live with:
```bash
ssh admin@scoreboard 'tail -f /home/admin/billiardiq-scoreboard/logs/scoreboard-*.log'
```

## Exiting kiosk mode for testing

- `Alt+F4`, or
- `pkill -f chromium` (locally or via SSH), or
- switch VT with `Ctrl+Alt+F2` and kill it from there.

Chromium's single-instance-per-profile behavior means a leftover process from a prior test
can make a fresh launch silently no-op (prints a couple of harmless GCM/registration error
lines, then exits instantly). Clear it with:
```bash
pkill -9 -f chromium
rm -f ~/.config/chromium/Singleton*
```

## Files here

| File | Runs where | Purpose |
|---|---|---|
| `install-dotnet.sh` | Pi, once | Installs the ASP.NET Core runtime to `~/.dotnet` |
| `billiardiq-scoreboard.service` | Pi, installed by the deploy script | systemd unit — runs the app, `Restart=always` |
| `install-kiosk-deps.sh` | Pi, once | apt-installs `unclutter`, `wmctrl`, `xset`, Chromium — everything `launch-kiosk.sh` needs |
| `install-power-sudoers.sh` | Pi, once | NOPASSWD sudoers rule so the app can run `systemctl reboot`/`poweroff` itself |
| `install-deploy-sudoers.sh` | Pi, once | NOPASSWD sudoers rule so `publish-and-deploy.ps1` can stop/start the service and install its unit file over SSH |
| `migrate-from-zeymera.sh` | Pi, once | Renames an old zeymera-scoreboard install (folder, service, autostart path, sudoers rules) to billiardiq-scoreboard |
| `launch-kiosk.sh` | Pi, every boot (via autostart) | Waits for the app, then launches Chromium in kiosk mode |
| `setup-autostart.sh` | Pi, once | Wires `launch-kiosk.sh` into `/etc/xdg/labwc/autostart` |
| `publish-and-deploy.ps1` | Windows, every redeploy | Publish → strip local data → stop service → remove old binaries → scp → sync unit → start service |
