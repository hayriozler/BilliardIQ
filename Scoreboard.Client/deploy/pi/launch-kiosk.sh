#!/usr/bin/env bash
set -uo pipefail

# Launches Chromium in kiosk mode pointed at the local scoreboard app. Meant to be started
# by the desktop session's autostart (see setup-autostart.sh), not run interactively.
#
# Desktop stack this was built/tested against: lightdm (autologin, autologin-session=
# rpd-labwc) -> /usr/bin/labwc-pi -> labwc -m -- i.e. Wayland via labwc, NOT the older
# X11/LXDE stack most Pi-kiosk tutorials assume. If autostart "doesn't fire" on a different
# Pi OS image, trace the actual session chain (systemctl get-default -> display-manager.service
# -> the session's .desktop/Exec= -> any wrapper script) before assuming this script is wrong.

APP_URL="http://localhost:5288/"

LOG_DIR="$HOME/kiosk-logs"
LOG_FILE="$LOG_DIR/kiosk.log"
LOG_MAX_BYTES=1048576

mkdir -p "$LOG_DIR"
if [ -f "$LOG_FILE" ] && [ "$(stat -c %s "$LOG_FILE")" -gt "$LOG_MAX_BYTES" ]; then
    mv -f "$LOG_FILE" "$LOG_FILE.1"
fi

log() {
    echo "$(date '+%Y-%m-%d %H:%M:%S') [$$] $*" >> "$LOG_FILE"
}

log "==== launch-kiosk.sh started (uptime: $(cut -d' ' -f1 /proc/uptime)s) ===="
log "User=$(id -un) DISPLAY=${DISPLAY:-<unset>} WAYLAND_DISPLAY=${WAYLAND_DISPLAY:-<unset>} XDG_SESSION_TYPE=${XDG_SESSION_TYPE:-<unset>} XDG_RUNTIME_DIR=${XDG_RUNTIME_DIR:-<unset>}"

# systemd starts the app in parallel with the desktop session, not before it, so it may not
# be up yet when this script runs - wait for it rather than racing it.
WAIT_SECONDS=120
SETTLE_SECONDS=5
waited=0
answers=0
log "Waiting for app on $APP_URL (max ${WAIT_SECONDS}s)"
while true; do
    if curl -sSf "$APP_URL" > /dev/null 2>&1; then
        answers=$((answers + 1))
    else
        answers=0
    fi
    if [ "$answers" -ge 2 ]; then
        log "App answered after ${waited}s, settling ${SETTLE_SECONDS}s"
        sleep "$SETTLE_SECONDS"
        break
    fi
    if [ "$waited" -ge "$WAIT_SECONDS" ]; then
        log "App did not answer on $APP_URL within ${WAIT_SECONDS}s, starting Chromium anyway."
        break
    fi
    if [ $((waited % 10)) -eq 0 ] && [ "$waited" -gt 0 ]; then
        log "Still waiting for app (${waited}s)"
    fi
    sleep 1
    waited=$((waited + 1))
done

# Disable screen blanking. Harmless no-ops under pure Wayland/labwc; kept for X11 fallback.
xset s off >> "$LOG_FILE" 2>&1 || log "xset s off failed (expected under pure Wayland)"
xset -dpms >> "$LOG_FILE" 2>&1 || log "xset -dpms failed (expected under pure Wayland)"
xset s noblank >> "$LOG_FILE" 2>&1 || log "xset s noblank failed (expected under pure Wayland)"

# wmctrl-based watchdog to keep Chromium focused/on top. This is an X11 tool and may be a
# no-op under native Wayland - left in place, untested whether it's actually doing anything
# post-labwc-migration, but it's harmless either way.
(
    while true; do
        wmctrl -a "chromium" 2>/dev/null || true
        sleep 5
    done
) &
WATCHDOG_PID=$!

CHROMIUM_BIN="$(command -v chromium || command -v chromium-browser || true)"
if [ -z "$CHROMIUM_BIN" ]; then
    log "ERROR: neither chromium nor chromium-browser found in PATH ($PATH)"
    kill "$WATCHDOG_PID" 2>/dev/null || true
    exit 1
fi

log "Starting $CHROMIUM_BIN"

# --password-store=basic suppresses a GNOME-Keyring "choose password for new keyring"
# prompt Chromium triggers even in incognito.
"$CHROMIUM_BIN" \
    --kiosk \
    --noerrdialogs \
    --disable-infobars \
    --incognito \
    --disable-session-crashed-bubble \
    --disable-translate \
    --password-store=basic \
    --start-fullscreen \
    --hide-scrollbars \
    "$APP_URL" >> "$LOG_FILE" 2>&1
exit_code=$?

log "Chromium exited with code $exit_code"
kill "$WATCHDOG_PID" 2>/dev/null || true
exit "$exit_code"
