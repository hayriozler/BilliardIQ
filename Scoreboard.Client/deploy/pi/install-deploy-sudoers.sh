#!/usr/bin/env bash
set -euo pipefail

# One-time: lets publish-and-deploy.ps1 restart the app's systemd service over a
# non-interactive SSH call, without sudo prompting for a password it has no TTY to ask for.
#
# Run this ON the Pi as the app's service user (e.g. `admin`):
#   bash install-deploy-sudoers.sh

SERVICE_USER="$(whoami)"
RULE_FILE="/etc/sudoers.d/zeymera-scoreboard-deploy"
RULE="${SERVICE_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl restart zeymera-scoreboard.service"

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
echo "Verify with: sudo -n -l | grep 'systemctl restart'"
