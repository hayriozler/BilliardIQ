#!/usr/bin/env bash
set -euo pipefail

# Downloads the first .zip asset from the repository's latest GitHub release,
# extracts it to /tmp, copies everything into a target folder except wwwroot,
# then starts launch-kiosk.sh.
#
# Run this ON the Pi:
#   bash update-from-latest-release.sh

REPO="hayriozler/BilliardIQ"
API_URL="https://api.github.com/repos/${REPO}/releases/latest"

read -r -p "Enter target folder (absolute path): " TARGET_DIR
if [ -z "${TARGET_DIR}" ]; then
	echo "Target folder is required." >&2
	exit 1
fi

mkdir -p "$TARGET_DIR"

TMP_DIR="$(mktemp -d /tmp/billiardiq-release-XXXXXX)"
RELEASE_JSON="${TMP_DIR}/latest-release.json"
ZIP_PATH="${TMP_DIR}/release.zip"
EXTRACT_DIR="${TMP_DIR}/extracted"

cleanup() {
	rm -rf "$TMP_DIR"
}
trap cleanup EXIT

echo "Fetching latest release metadata from ${REPO} ..."
curl -fsSL "$API_URL" -o "$RELEASE_JSON"

ZIP_URL="$(grep -oE '"browser_download_url"[[:space:]]*:[[:space:]]*"[^"]+"' "$RELEASE_JSON" \
	| sed -E 's/^.*"([^"]+)"$/\1/' \
	| grep -iE '\.zip([?#].*)?$' \
	| head -n 1 \
	| tr -d '\r' || true)"

if [ -z "$ZIP_URL" ]; then
	echo "No .zip asset found in the latest release for ${REPO}." >&2
	exit 1
fi

echo "Stopping Chromium if running ..."
pkill -f chromium-browser >/dev/null 2>&1 || true
pkill -f chromium >/dev/null 2>&1 || true

echo "Downloading: $ZIP_URL"
curl -fL "$ZIP_URL" -o "$ZIP_PATH"

mkdir -p "$EXTRACT_DIR"
unzip -q "$ZIP_PATH" -d "$EXTRACT_DIR"

SOURCE_DIR="$EXTRACT_DIR"
FIRST_ENTRY="$(find "$EXTRACT_DIR" -mindepth 1 -maxdepth 1 | head -n 1 || true)"
ENTRY_COUNT="$(find "$EXTRACT_DIR" -mindepth 1 -maxdepth 1 | wc -l | tr -d ' ')"
if [ "$ENTRY_COUNT" = "1" ] && [ -d "$FIRST_ENTRY" ]; then
	SOURCE_DIR="$FIRST_ENTRY"
fi

echo "Copying files to $TARGET_DIR (excluding wwwroot) ..."
rsync -a \
	--exclude='wwwroot/' \
	--exclude='wwwRoot/' \
	"$SOURCE_DIR/" "$TARGET_DIR/"

LAUNCH_SCRIPT="$TARGET_DIR/launch-kiosk.sh"
if [ ! -f "$LAUNCH_SCRIPT" ]; then
	echo "launch-kiosk.sh was not found in $TARGET_DIR." >&2
	exit 1
fi

chmod +x "$LAUNCH_SCRIPT"

echo "Starting launch-kiosk.sh ..."
nohup bash "$LAUNCH_SCRIPT" >/tmp/billiardiq-kiosk-launch.log 2>&1 &

echo "Done. Kiosk launch log: /tmp/billiardiq-kiosk-launch.log"