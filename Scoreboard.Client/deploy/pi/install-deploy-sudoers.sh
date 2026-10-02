#!/usr/bin/env bash
set -euo pipefail

# One-time: lets publish-and-deploy.ps1 stop/start the app's systemd service and install its
# unit file over non-interactive SSH calls, without sudo prompting for a password it has no
# TTY to ask for.
#
# Run this ON the Pi as the app's service user (e.g. `admin`):
#   bash install-deploy-sudoers.sh

SERVICE_USER="$(whoami)"
SERVICE="billiardiq-scoreboard.service"
STAGED="$HOME/billiardiq-scoreboard.service.new"
UNIT="/etc/systemd/system/$SERVICE"
RULE_FILE="/etc/sudoers.d/billiardiq-scoreboard-deploy"
RULE="${SERVICE_USER} ALL=(root) NOPASSWD: /usr/bin/systemctl stop ${SERVICE}, /usr/bin/systemctl start ${SERVICE}, /usr/bin/systemctl restart ${SERVICE}, /usr/bin/systemctl enable ${SERVICE}, /usr/bin/systemctl daemon-reload, /usr/bin/install -m 0644 -o root -g root ${STAGED} ${UNIT}"

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
