#!/bin/bash
set -e

mkdir -p "$HOME/.config/labwc"

cat > "$HOME/.config/labwc/environment" <<'EOF'
XCURSOR_THEME=PiXtrix
XCURSOR_SIZE=24
EOF

echo "Cursor ON (PiXtrix)."
echo "Reboot or restart the labwc session to apply."

chmod +x cursor-on.sh
sudo reboot