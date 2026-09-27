#!/usr/bin/env bash
set -euo pipefail

# One-time: installs the apt packages launch-kiosk.sh depends on but that aren't guaranteed
# to be present on a fresh Raspberry Pi OS (Bookworm) image.
#
#   wmctrl              - keeps Chromium focused/on top under X11 fallback
#   x11-xserver-utils   - provides `xset` (screen-blanking disable)
#   chromium-browser    - the kiosk browser itself (metapackage; pulls in `chromium` on
#                         Bookworm's actual package)
#
# curl is assumed already present (Raspberry Pi OS ships it by default).
#
# Run this ON the Pi as the app's service user (e.g. `admin`), before setup-autostart.sh:
#   bash install-kiosk-deps.sh

sudo apt-get update
sudo apt-get install -y wmctrl x11-apps x11-xserver-utils chromium-browser

echo "Done. Verify with: wmctrl -m; xset q; command -v chromium || command -v chromium-browser"
