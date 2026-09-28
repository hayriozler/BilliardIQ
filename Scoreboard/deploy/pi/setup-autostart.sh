#!/usr/bin/env bash
set -euo pipefail

# One-time: wires launch-kiosk.sh into the desktop session's autostart, so the kiosk browser
# comes up automatically on boot.
#
# What actually works on this Pi's stack (lightdm -> labwc, Wayland - see launch-kiosk.sh's
# header comment): appending directly to the proven-working SYSTEM autostart file, which is
# already responsible for launching the panel/wf-panel-pi, file manager/pcmanfm-pi, kanshi,
# and lxsession-xdg-autostart. Per-user autostart files (~/.config/autostart/*.desktop,
# ~/.config/labwc/autostart) were both tried first and silently did not fire - don't revert
# to those without re-verifying on the actual target image.
#
# Run this ON the Pi as the app's service user (e.g. `admin`), after zeymera-scoreboard is
# deployed to APP_DIR below:
#   bash setup-autostart.sh

APP_DIR="/home/admin/zeymera-scoreboard"
AUTOSTART_FILE="/etc/xdg/labwc/autostart"

# Output redirected to a log file - boot-time autostart failures are otherwise completely
# invisible (no attached terminal to see stderr on).
LINE="$APP_DIR/launch-kiosk.sh > \$HOME/kiosk-autostart.log 2>&1 &"

if grep -qF "launch-kiosk.sh" "$AUTOSTART_FILE" 2>/dev/null; then
    echo "Already present in $AUTOSTART_FILE, skipping."
else
    echo "$LINE" | sudo tee -a "$AUTOSTART_FILE" > /dev/null
    echo "Added kiosk launch line to $AUTOSTART_FILE - reboot to test."
fi
