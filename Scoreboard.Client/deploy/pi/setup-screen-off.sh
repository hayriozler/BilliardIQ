#!/usr/bin/env bash
set -euo pipefail

AUTOSTART_FILE="/etc/xdg/labwc/autostart"
TIMEOUT_SECONDS="${1:-1800}"

for package in swayidle wlopm; do
    if ! command -v "$package" > /dev/null 2>&1; then
        sudo apt-get install -y "$package"
    fi
done

LINE=$(printf "swayidle -w timeout %s 'wlopm --off \"*\"' resume 'wlopm --on \"*\"' > \$HOME/screen-off.log 2>&1 &" "$TIMEOUT_SECONDS")

if grep -qF "swayidle" "$AUTOSTART_FILE" 2>/dev/null; then
    echo "Already present in $AUTOSTART_FILE, skipping."
else
    echo "$LINE" | sudo tee -a "$AUTOSTART_FILE" > /dev/null
    echo "Added the screen off timer ($TIMEOUT_SECONDS s) to $AUTOSTART_FILE - reboot to test."
fi
