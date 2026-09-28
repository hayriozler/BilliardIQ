#!/usr/bin/env bash
set -euo pipefail

# One-time: lets the app's service user run `systemctl reboot`/`systemctl poweroff`
# without a password prompt, so SystemPowerService.cs can actually carry out the
# reboot/shutdown triggered by the NumLock+Insert / NumLock+Delete keyboard hold.
#
# Run this ON the Pi as the app's service user (e.g. `admin`):
#   bash install-power-sudoers.sh

SERVICE_USER="$(whoami)"
RULE_FILE="/etc/sudoers.d/zeymera-scoreboard-power"
RULE="${SERVICE_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl reboot, /usr/bin/systemctl poweroff"

TMP_FILE="$(mktemp)"
echo "$RULE" > "$TMP_FILE"

if ! sudo visudo -c -f "$TMP_FILE" > /dev/null; then
    echo "Generated sudoers rule failed validation, aborting:" >&2
    cat "$TMP_FILE" >&2
    rm -f "$TMP_FILE"
    exit 1
fi

sudo install -m 0440 -o root -g root "$TMP_FILE" "$RULE_FILE"
rm -f "$TMP_FILE"

echo "Installed $RULE_FILE for user '$SERVICE_USER':"
echo "  $RULE"
echo "Verify with: sudo -n -l | grep systemctl"
