# adb.exe path: C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
$project = ".\BilliardIQ.Mobile.csproj"
$framework = "net10.0-android"

dotnet publish $project -f $framework -c Release -p:AndroidPackageFormats=apk

if ($LASTEXITCODE -ne 0) {
    throw "Release build failed."
}

$apk = Get-ChildItem ".\bin\Release\$framework\publish\*.apk" |
    Select-Object -First 1

if (-not $apk) {
    throw "No APK found in the publish folder."
}

# adb install -r $apk.FullName
& 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe' -s RFCX21AX9RY install -r $apk