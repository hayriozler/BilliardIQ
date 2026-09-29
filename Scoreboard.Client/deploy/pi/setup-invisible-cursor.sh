#!/bin/bash
set -e

echo "Creating invisible cursor theme..."

mkdir -p "$HOME/.local/share/icons/Invisible/cursors"
mkdir -p "$HOME/.config/labwc"

# Create a 1x1 transparent PNG using Python (no extra Python packages needed)
python3 <<'PY'
import struct
import zlib

def chunk(name, data):
    return (
        struct.pack(">I", len(data)) +
        name +
        data +
        struct.pack(">I", zlib.crc32(name + data) & 0xffffffff)
    )

png = (
    b"\x89PNG\r\n\x1a\n"
    + chunk(b"IHDR", struct.pack(">IIBBBBB", 1, 1, 8, 6, 0, 0, 0))
    + chunk(b"IDAT", zlib.compress(b"\x00\x00\x00\x00\x00"))
    + chunk(b"IEND", b"")
)

with open("/tmp/transparent.png", "wb") as f:
    f.write(png)
PY

# Xcursor definition
cat > /tmp/invisible.cursor <<EOF
1 0 0 /tmp/transparent.png
EOF

# Generate invisible cursor
xcursorgen \
    /tmp/invisible.cursor \
    "$HOME/.local/share/icons/Invisible/cursors/left_ptr"

# Cursor theme metadata
cat > "$HOME/.local/share/icons/Invisible/index.theme" <<EOF
[Icon Theme]
Name=Invisible
EOF

# Tell labwc to use the invisible cursor
cat > "$HOME/.config/labwc/environment" <<EOF
XCURSOR_THEME=Invisible
XCURSOR_SIZE=24
EOF

echo
echo "Invisible cursor installed successfully."
echo
echo "Reboot the Raspberry Pi to activate it:"
echo "    sudo reboot"


chmod +x setup-invisible-cursor.sh

sudo reboot