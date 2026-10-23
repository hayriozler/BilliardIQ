$clientProject = "D:\SourceCodes\Z&M\BilliardIQ\Scoreboard.Client\Scoreboard.Client.csproj"
$publishDir = "D:\SourceCodes\Z&M\BilliardIQ\Scoreboard.Client\publish-pi"
$zipPath = "D:\SourceCodes\Z&M\BilliardIQ\Scoreboard.Client\billiardiq-client-linux-arm64.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
if(Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}
dotnet publish $clientProject -c Release -r linux-arm64 --self-contained false -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

Add-Type -AssemblyName System.IO.Compression

$archive = [System.IO.Compression.ZipFile]::Open(
    $zipPath,
    [System.IO.Compression.ZipArchiveMode]::Create
)

try {
    Get-ChildItem -LiteralPath $publishDir -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($publishDir.Length).TrimStart('\', '/')
        $entryName = $relativePath.Replace('\', '/')

        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive,
            $_.FullName,
            $entryName
        ) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Deployment package created successfully."
Write-Host "Github release upload starting.."
$tag = gh release list --limit 1 --json tagName --jq '.[0].tagName'
Write-Host "Latest release: $tag"
gh release upload $tag $zipPath --latest --generate-notes