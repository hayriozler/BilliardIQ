#!/usr/bin/env bash
set -euo pipefail

# One-time .NET runtime install on the Raspberry Pi (64-bit Raspberry Pi OS, Bookworm).
# The app is published framework-dependent (see publish-and-deploy.ps1), so only the
# ASP.NET Core runtime is needed here - not the full SDK.
#
# Run this ON the Pi as the app's service user (e.g. `admin`):
#   bash install-dotnet.sh

DOTNET_CHANNEL="10.0"

curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --runtime aspnetcore

if ! grep -q 'DOTNET_ROOT' "$HOME/.bashrc"; then
    {
        echo ''
        echo '# .NET runtime (installed by deploy/pi/install-dotnet.sh)'
        echo 'export DOTNET_ROOT=$HOME/.dotnet'
        echo 'export PATH=$PATH:$HOME/.dotnet'
    } >> "$HOME/.bashrc"
    echo "Added DOTNET_ROOT/PATH to ~/.bashrc"
fi

echo "Done. Run 'source ~/.bashrc' (or open a new shell), then 'dotnet --info' to verify."
