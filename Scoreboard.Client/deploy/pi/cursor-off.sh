#!/bin/bash
set -e

mkdir -p "$HOME/.config/labwc"

cat > "$HOME/.config/labwc/environment" <<'EOF'
XCURSOR_THEME=Invisible
XCURSOR_SIZE=24
EOF

echo "Cursor OFF."
echo "Reboot or restart the labwc session to apply."

chmod +x cursor-off.sh
sudo reboot