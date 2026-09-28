<#
.SYNOPSIS
    Publishes the Client app for the Pi (linux-arm64, framework-dependent) and deploys it.

.DESCRIPTION
    Run this from Windows. It publishes to a local folder, strips the dev SQLite db and
    player photos out of the publish output (so a redeploy can never overwrite the Pi's
    live data), copies the result to the Pi over scp, and restarts the systemd service.

    Requires: an SSH key already set up for passwordless `ssh`/`scp` to the Pi, and
    deploy/pi/zeymera-scoreboard.service + install-dotnet.sh + setup-autostart.sh already
    installed on the Pi (one-time - see deploy/pi/README.md).

.PARAMETER PiHost
    Hostname or IP of the Pi. Defaults to "scoreboard".

.PARAMETER PiUser
    SSH user on the Pi. Defaults to "admin".

.PARAMETER RemoteAppDir
    Where the app lives on the Pi. Must match WorkingDirectory in zeymera-scoreboard.service.
#>
param(
    [string]$PiHost = "scoreboard",
    [string]$PiUser = "admin",
    [string]$RemoteAppDir = "/home/admin/zeymera-scoreboard"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$clientProject = Join-Path $repoRoot "Zeymera.BillardIQ.Client\Zeymera.BillardIQ.Client.csproj"

# Must be an absolute path - a relative -o path triggers a real Web SDK bug where the
# static-web-assets publish step duplicates output into a nested publish/ subfolder inside it.
$publishDir = Join-Path $repoRoot "publish-pi"

Write-Host "Publishing to $publishDir ..."
dotnet publish $clientProject -c Release -r linux-arm64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# dotnet publish sweeps up whatever's actually in wwwroot/ at publish time, including the
# dev SQLite db and any locally-uploaded player photos - never let those overwrite the
# Pi's live data.
Remove-Item -Recurse -Force (Join-Path $publishDir "wwwroot\Db") -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force (Join-Path $publishDir "wwwroot\Players") -ErrorAction SilentlyContinue

Write-Host "Copying to ${PiUser}@${PiHost}:${RemoteAppDir} ..."
& scp -r "$publishDir\*" "${PiUser}@${PiHost}:${RemoteAppDir}/"
if ($LASTEXITCODE -ne 0) { throw "scp failed" }

$serviceInstalled = & ssh "${PiUser}@${PiHost}" "systemctl list-unit-files zeymera-scoreboard.service 2>/dev/null | grep -q zeymera-scoreboard.service && echo yes || echo no"
if ($serviceInstalled -eq "yes") {
    Write-Host "Restarting service ..."
    & ssh "${PiUser}@${PiHost}" "sudo systemctl restart zeymera-scoreboard.service"
    if ($LASTEXITCODE -ne 0) { throw "ssh restart failed" }
} else {
    Write-Host "Service not installed yet - skipping restart (see deploy/pi/README.md step 3)."
}

Write-Host "Done."
