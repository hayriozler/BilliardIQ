<#
.SYNOPSIS
    Publishes the Client app for the Pi (linux-arm64, framework-dependent) and deploys it.

.DESCRIPTION
    Run this from Windows. It publishes to a local folder, strips the dev SQLite db and
    player photos out of the publish output (so a redeploy can never overwrite the Pi's
    live data), then on the Pi: stops the service, deletes the previous binaries (dll, pdb,
    native libs, json manifests, executables - never wwwroot), copies the new publish over scp,
    installs the systemd unit if it changed, and starts the service again.

    Requires: an SSH key already set up for passwordless `ssh`/`scp` to the Pi, and
    deploy/pi/install-deploy-sudoers.sh already run on the Pi (one-time - see deploy/pi/README.md).

.PARAMETER PiHost
    Hostname or IP of the Pi. Defaults to "scoreboard".

.PARAMETER PiUser
    SSH user on the Pi. Defaults to "admin".

.PARAMETER RemoteAppDir
    Where the app lives on the Pi. Must match WorkingDirectory in billiardiq-scoreboard.service
    and the staging path in install-deploy-sudoers.sh ($HOME/billiardiq-scoreboard).

.PARAMETER ServiceName
    systemd unit name without the .service suffix.
#>
param(
    [string]$PiHost = "scoreboard",
    [string]$PiUser = "admin",
    [string]$RemoteAppDir = "/home/admin/billiardiq-scoreboard",
    [string]$ServiceName = "billiardiq-scoreboard"
)

$ErrorActionPreference = "Stop"

$target = "${PiUser}@${PiHost}"
$unitFile = "$ServiceName.service"
$unitPath = "/etc/systemd/system/$unitFile"
$stagedUnit = "$RemoteAppDir.service.new"

$clientRoot = Resolve-Path "$PSScriptRoot\..\.."
$clientProject = Join-Path $clientRoot "Scoreboard.Client.csproj"

# Must be an absolute path - a relative -o path triggers a real Web SDK bug where the
# static-web-assets publish step duplicates output into a nested publish/ subfolder inside it.
$publishDir = Join-Path $clientRoot "publish-pi"

function Invoke-Remote([string]$Command) {
    & ssh $target $Command
    if ($LASTEXITCODE -ne 0) { throw "Remote command failed: $Command" }
}

Write-Host "Publishing to $publishDir ..."
dotnet publish $clientProject -c Release -r linux-arm64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# dotnet publish sweeps up whatever's actually in wwwroot/ at publish time, including the
# dev SQLite db and any locally-uploaded player photos - never let those overwrite the
# Pi's live data.
Remove-Item -Recurse -Force (Join-Path $publishDir "wwwroot\Db") -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force (Join-Path $publishDir "wwwroot\Players") -ErrorAction SilentlyContinue

# systemd rejects unit files with CRLF line endings, so normalise before copying.
$unitText = (Get-Content -Raw (Join-Path $PSScriptRoot $unitFile)) -replace "`r`n", "`n"
$localUnit = Join-Path ([System.IO.Path]::GetTempPath()) $unitFile
[System.IO.File]::WriteAllText($localUnit, $unitText)

Write-Host "Checking sudo permissions on the Pi ..."
& ssh $target "sudo -n /usr/bin/systemctl daemon-reload"
if ($LASTEXITCODE -ne 0) {
    throw "sudo is not allowed without a password on the Pi. Run deploy/pi/install-deploy-sudoers.sh there once (see deploy/pi/README.md)."
}

Invoke-Remote "mkdir -p '$RemoteAppDir'"

Write-Host "Stopping $ServiceName ..."
Invoke-Remote "if systemctl is-active --quiet '$unitFile'; then sudo -n /usr/bin/systemctl stop '$unitFile'; fi"

Write-Host "Removing the previous binaries (wwwroot is kept) ..."
Invoke-Remote "find '$RemoteAppDir' -maxdepth 1 -type f \( -name '*.dll' -o -name '*.pdb' -o -name '*.so' -o -name '*.deps.json' -o -name '*.runtimeconfig.json' -o -name '*.staticwebassets*.json' -o -name 'Scoreboard.Client' -o -name 'Zeymera.Scoreboard.Client' \) -delete"

Write-Host "Copying to ${target}:${RemoteAppDir} ..."
& scp -r "$publishDir\*" "${target}:${RemoteAppDir}/"
if ($LASTEXITCODE -ne 0) { throw "scp failed - the service is stopped and the binaries are removed; fix the connection and run the script again." }

& scp $localUnit "${target}:${stagedUnit}"
if ($LASTEXITCODE -ne 0) { throw "scp of the service file failed" }

Write-Host "Syncing the systemd unit ..."
Invoke-Remote "if ! cmp -s '$stagedUnit' '$unitPath'; then sudo -n /usr/bin/install -m 0644 -o root -g root '$stagedUnit' '$unitPath' && sudo -n /usr/bin/systemctl daemon-reload && sudo -n /usr/bin/systemctl enable '$unitFile'; fi; rm -f '$stagedUnit'"

Write-Host "Starting $ServiceName ..."
Invoke-Remote "sudo -n /usr/bin/systemctl start '$unitFile'"

Remove-Item $localUnit -ErrorAction SilentlyContinue

Write-Host "Done."
