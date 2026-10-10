#!/usr/bin/env bash
set -euo pipefail

AUTOSTART_FILE="${AUTOSTART_FILE:-/etc/xdg/labwc/autostart}"
IMAGE="${1:-$HOME/screensaver.jpg}"
IMAGE_AFTER_SECONDS="${2:-600}"
SCREEN_OFF_AFTER_SECONDS="${3:-3600}"

if [[ ! "$IMAGE" =~ ^[A-Za-z0-9._/~-]+$ ]]; then
    echo "The image path may only contain letters, digits and . _ / ~ -" >&2
    exit 1
fi

for package in swayidle wlopm; do
    if ! command -v "$package" > /dev/null 2>&1; then
        sudo apt-get install -y "$package"
    fi
done

if [ -f "$IMAGE" ]; then
    IMAGE="$(realpath "$IMAGE")"
    if ! command -v imv-wayland > /dev/null 2>&1; then
        sudo apt-get install -y imv
    fi
    IMV="$(command -v imv-wayland)"

    LINE=$(printf "swayidle -w timeout %s '%s -f -s full -b 000000 %s &' resume 'pkill -x imv-wayland' timeout %s 'pkill -x imv-wayland; wlopm --off \"*\"' resume 'wlopm --on \"*\"' > \$HOME/screen-off.log 2>&1 &" \
        "$IMAGE_AFTER_SECONDS" "$IMV" "$IMAGE" "$SCREEN_OFF_AFTER_SECONDS")
    echo "Image $IMAGE after $IMAGE_AFTER_SECONDS s, monitor off after $SCREEN_OFF_AFTER_SECONDS s."
else
    LINE=$(printf "swayidle -w timeout %s 'wlopm --off \"*\"' resume 'wlopm --on \"*\"' > \$HOME/screen-off.log 2>&1 &" "$SCREEN_OFF_AFTER_SECONDS")
    echo "No image at $IMAGE, monitor off after $SCREEN_OFF_AFTER_SECONDS s."
fi

if grep -qF "swayidle" "$AUTOSTART_FILE" 2>/dev/null; then
    sudo sed -i '/swayidle/d' "$AUTOSTART_FILE"
fi

echo "$LINE" | sudo tee -a "$AUTOSTART_FILE" > /dev/null
echo "Written to $AUTOSTART_FILE - reboot to test."
