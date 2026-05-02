@echo off
set "ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
set "APK=D:\SourceCodes\BilliardIQ\BilliardIQ.Mobile\bin\Debug\net10.0-android\com.billiardiq.mobile-Signed.apk"
set "PKG=com.billiardiq.mobile"
set "DEVICE=RFCX21AX9RY"

echo Uninstalling...
"%ADB%" -s %DEVICE% uninstall %PKG%

echo Installing...
"%ADB%" -s %DEVICE% install "%APK%"
