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

# systemd starts the app in parallel with the desktop session, not before it, so it may not
# be up yet when this script runs - wait for it rather than racing it.
WAIT_SECONDS=120
SETTLE_SECONDS=5
waited=0
answers=0
while true; do
    if curl -sSf "$APP_URL" > /dev/null 2>&1; then
        answers=$((answers + 1))
    else
        answers=0
    fi
    if [ "$answers" -ge 2 ]; then
        sleep "$SETTLE_SECONDS"
        break
    fi
    if [ "$waited" -ge "$WAIT_SECONDS" ]; then
        echo "App did not answer on $APP_URL within ${WAIT_SECONDS}s, starting Chromium anyway."
        break
    fi
    sleep 1
    waited=$((waited + 1))
done

# Disable screen blanking. Harmless no-ops under pure Wayland/labwc; kept for X11 fallback.
xset s off
xset -dpms
xset s noblank

# wmctrl-based watchdog to keep Chromium focused/on top. This is an X11 tool and may be a
# no-op under native Wayland - left in place, untested whether it's actually doing anything
# post-labwc-migration, but it's harmless either way.
(
    while true; do
        wmctrl -a "chromium" 2>/dev/null || true
        sleep 5
    done
) &

CHROMIUM_BIN="$(command -v chromium || command -v chromium-browser)"

# --password-store=basic suppresses a GNOME-Keyring "choose password for new keyring"
# prompt Chromium triggers even in incognito.
exec "$CHROMIUM_BIN" \
    --kiosk \
    --noerrdialogs \
    --disable-infobars \
    --incognito \
    --disable-session-crashed-bubble \
    --disable-translate \
    --password-store=basic \
    --start-fullscreen \
    --hide-scrollbars \
    "$APP_URL"
