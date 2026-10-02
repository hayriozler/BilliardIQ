#!/usr/bin/env bash
set -euo pipefail

# One-time: moves an existing Pi install from the old "zeymera-scoreboard" names to
# "billiardiq-scoreboard" without losing the live data (wwwroot/Db, wwwroot/Players, logs).
#
# Run this ON the Pi as the app's service user (e.g. `admin`), then run
# install-deploy-sudoers.sh and install-power-sudoers.sh, then publish-and-deploy.ps1
# from Windows (it installs and starts the renamed service).
#   bash migrate-from-zeymera.sh

OLD_NAME="zeymera-scoreboard"
NEW_NAME="billiardiq-scoreboard"
OLD_DIR="$HOME/$OLD_NAME"
NEW_DIR="$HOME/$NEW_NAME"
AUTOSTART_FILE="/etc/xdg/labwc/autostart"

if systemctl list-unit-files "$OLD_NAME.service" 2>/dev/null | grep -q "$OLD_NAME.service"; then
    sudo systemctl disable --now "$OLD_NAME.service" || true
    sudo rm -f "/etc/systemd/system/$OLD_NAME.service"
    sudo systemctl daemon-reload
    echo "Removed the old $OLD_NAME.service."
fi

if [ -d "$OLD_DIR" ] && [ ! -d "$NEW_DIR" ]; then
    mv "$OLD_DIR" "$NEW_DIR"
    echo "Moved $OLD_DIR to $NEW_DIR."
elif [ -d "$OLD_DIR" ]; then
    echo "Both $OLD_DIR and $NEW_DIR exist - merge them by hand, nothing moved." >&2
fi

if [ -f "$AUTOSTART_FILE" ] && grep -qF "/$OLD_NAME/" "$AUTOSTART_FILE"; then
    sudo sed -i "s#/$OLD_NAME/#/$NEW_NAME/#g" "$AUTOSTART_FILE"
    echo "Updated the kiosk autostart path in $AUTOSTART_FILE."
fi

sudo rm -f "/etc/sudoers.d/$OLD_NAME-deploy" "/etc/sudoers.d/$OLD_NAME-power"
echo "Removed the old sudoers rules - run install-deploy-sudoers.sh and install-power-sudoers.sh next."
