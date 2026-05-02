@echo off
setlocal

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.4.4f1\Editor\Unity.exe"
set "UNITY_PROJECT=D:\SourceCodes\BilliardIQ\BilliardIQ.Unity"
set "UNITY_EXPORT=D:\SourceCodes\BilliardIQ\UnityExport"
set "MAUI_LIBS=D:\SourceCodes\BilliardIQ\BilliardIQ.Mobile\Platforms\Android\libs"
set "AAR_SRC=%UNITY_EXPORT%\unityLibrary\build\outputs\aar\unityLibrary-debug.aar"
set "AAR_DST=%MAUI_LIBS%\unityLibrary-debug.aar"
set "JAVA_HOME=C:\PROGRA~2\Android\openjdk\jdk-21.0.8"
set "PATH=%JAVA_HOME%\bin;%PATH%"

echo [1/5] Exporting Unity project...
"%UNITY_EXE%" -batchmode -quit ^
    -projectPath "%UNITY_PROJECT%" ^
    -buildTarget Android ^
    -executeMethod AndroidExporter.Export ^
    -logFile "%UNITY_EXPORT%\unity_export.log"
if errorlevel 1 (
    echo [ERROR] Unity export failed. Check log: %UNITY_EXPORT%\unity_export.log
    pause & exit /b 1
)
echo [OK] Unity export done.

echo [1b/5] Fixing local.properties...
echo sdk.dir=C\:\\Program Files (x86)\\Android\\android-sdk> "%UNITY_EXPORT%\local.properties"

echo [2/5] Building unityLibrary...
cd /d "%UNITY_EXPORT%"
call gradle :unityLibrary:assembleDebug

echo [3/5] Copying AAR...
copy /Y "%AAR_SRC%" "%AAR_DST%"

echo [4/5] Building MAUI...
cd /d "D:\SourceCodes\BilliardIQ"
dotnet build BilliardIQ.Mobile\BilliardIQ.Mobile.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true -p:AndroidUseAssemblyStore=false

echo [5/5] Deploying (clean install)...
"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s RFCX21AX9RY uninstall com.billiardiq.mobile
"C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s RFCX21AX9RY install "D:\SourceCodes\BilliardIQ\BilliardIQ.Mobile\bin\Debug\net10.0-android\com.billiardiq.mobile-Signed.apk"

echo.
echo === Done ===
pause
