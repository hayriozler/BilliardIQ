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
   This will fail at the "restart service" step the first time, since the service doesn't
   exist yet — that's expected, continue to step 3.

3. **Install the systemd service**:
   ```bash
   scp deploy/pi/zeymera-scoreboard.service admin@scoreboard:~/
   ssh -t admin@scoreboard 'sudo cp ~/zeymera-scoreboard.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable --now zeymera-scoreboard.service'
   ```
   Verify it's up: `ssh admin@scoreboard 'curl -sSf http://localhost:5288/ > /dev/null && echo OK'`

   Then let `publish-and-deploy.ps1` restart this service over a non-interactive SSH call
   without a password prompt on every future redeploy:
   ```bash
   scp deploy/pi/install-deploy-sudoers.sh admin@scoreboard:~/
   ssh -t admin@scoreboard 'chmod +x install-deploy-sudoers.sh && ./install-deploy-sudoers.sh'
   ```
   Skip this and every redeploy's restart step will fail with `sudo: a password is
   required` (see "Redeploying" below) — you'd have to restart the service by hand instead.

4. **Install the kiosk's apt dependencies** (`unclutter`, `wmctrl`, `xset`, Chromium — see
   `install-kiosk-deps.sh` for why each is needed):
   ```bash
   scp deploy/pi/install-kiosk-deps.sh admin@scoreboard:~/
   ssh -t admin@scoreboard 'chmod +x install-kiosk-deps.sh && ./install-kiosk-deps.sh'
   ```

5. **Install the kiosk launcher and wire up autostart**:
   ```bash
   scp deploy/pi/launch-kiosk.sh deploy/pi/setup-autostart.sh admin@scoreboard:/home/admin/zeymera-scoreboard/
   ssh -t admin@scoreboard 'chmod +x /home/admin/zeymera-scoreboard/launch-kiosk.sh && cd /home/admin/zeymera-scoreboard && bash setup-autostart.sh'
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
overwrite the Pi's live database or uploaded player photos), copies the result over, and
restarts the service. `launch-kiosk.sh`/`setup-autostart.sh`/the service file only need to
be re-copied if you actually change them — routine app redeploys don't touch those.

The restart step needs `install-deploy-sudoers.sh` installed (step 3 above) — without it,
`sudo systemctl restart zeymera-scoreboard.service` has no TTY to prompt for a password on
and fails with `sudo: a password is required`, and the script throws `ssh restart failed`.
The publish/copy still succeeded at that point; you'd just need to restart the service by
hand (`ssh -t admin@scoreboard 'sudo systemctl restart zeymera-scoreboard.service'`, entering
the password when prompted) until that sudoers rule is installed.

## Logs

The app logs to `logs/` under its working directory via Serilog (console + a rolling daily
file, `logs/scoreboard-YYYYMMDD.log`) — every WS connect/disconnect, received/sent message
(with the sender's IP and a sequence number), and parsed command. On the Pi this lands at
`/home/admin/zeymera-scoreboard/logs/` automatically, since that's the service's
`WorkingDirectory` — no extra setup needed. Tail it live with:
```bash
ssh admin@scoreboard 'tail -f /home/admin/zeymera-scoreboard/logs/scoreboard-*.log'
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
| `zeymera-scoreboard.service` | Pi, once | systemd unit — runs the app, `Restart=always` |
| `install-kiosk-deps.sh` | Pi, once | apt-installs `unclutter`, `wmctrl`, `xset`, Chromium — everything `launch-kiosk.sh` needs |
| `install-power-sudoers.sh` | Pi, once | NOPASSWD sudoers rule so the app can run `systemctl reboot`/`poweroff` itself |
| `install-deploy-sudoers.sh` | Pi, once | NOPASSWD sudoers rule so `publish-and-deploy.ps1` can restart the service over SSH |
| `launch-kiosk.sh` | Pi, every boot (via autostart) | Waits for the app, then launches Chromium in kiosk mode |
| `setup-autostart.sh` | Pi, once | Wires `launch-kiosk.sh` into `/etc/xdg/labwc/autostart` |
| `publish-and-deploy.ps1` | Windows, every redeploy | Publish → strip local data → scp → restart service |
