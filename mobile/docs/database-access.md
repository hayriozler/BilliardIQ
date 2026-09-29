# Accessing the SQLite database on a device

## Location

The app opens its SQLite database at `FileSystem.AppDataDirectory` + the filename defined in
`BilliardIQ.Mobile/Services/Constants.cs`:

```csharp
internal const string DatabaseFileName = "BillardIQ.db3";   // note: missing the second "i"
internal static string DatabasePath => $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName)}";
```

On Android, `FileSystem.AppDataDirectory` resolves to the app's private internal-storage files
directory (`Context.FilesDir`). With `ApplicationId` = `com.billiardiq.mobile`
(`BilliardIQ.Mobile.csproj`), the full on-device path is:

```
/data/data/com.billiardiq.mobile/files/BillardIQ.db3
```

This is private app storage: not on `/sdcard`, not visible in a normal file manager, and not
reachable via plain `adb shell` (which runs as the `shell` user, not the app's UID). Pulling it
requires `adb` and a **debuggable** build (Debug builds are debuggable by default; Release builds
are not, so this won't work on a Release APK).

## adb location (this machine)

`adb` isn't on `PATH` here. Full path:

```
C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
```

Either call it by full path or add that folder to `PATH`.

## Pulling the database off a device

```bash
ADB="/c/Program Files (x86)/Android/android-sdk/platform-tools/adb.exe"

# 1. Confirm a device is connected and authorized
"$ADB" devices -l

# 2. Confirm the app is installed
"$ADB" shell pm list packages | grep billiard

# 3. Pull the file
"$ADB" exec-out run-as com.billiardiq.mobile cat files/BillardIQ.db3 > BillardIQ.db3
```

### Why `run-as` and `exec-out` specifically

- **`run-as com.billiardiq.mobile`**: switches the shell into the app's own UID so it can read
  files under its private `/data/data/<package>/...` directory (mode `700`, owned by the app).
  Plain `adb shell` runs as `shell` and gets permission denied. This only works because Debug
  builds are marked `android:debuggable="true"`; Release builds refuse `run-as`.
- **`adb exec-out` instead of `adb shell ... > file`**: `adb shell`'s protocol does newline
  translation (`\n` → `\r\n`) meant for text terminal output, which silently corrupts binary
  data — a pulled `.db3` file ends up a few hundred bytes larger than the original and won't
  open. `adb exec-out` streams stdout back byte-for-byte, so the pulled file matches the
  on-device file exactly (verified by comparing `ls -la` sizes on both ends).

Once pulled, open the `.db3` file with any SQLite browser (e.g. DB Browser for SQLite).

## Pushing a database back onto the device (if ever needed)

```bash
"$ADB" push BillardIQ.db3 /data/local/tmp/BillardIQ.db3
"$ADB" shell run-as com.billiardiq.mobile cp /data/local/tmp/BillardIQ.db3 files/BillardIQ.db3
"$ADB" shell rm /data/local/tmp/BillardIQ.db3
```

`run-as` can't read from `/sdcard` or arbitrary paths directly for a push, so the file has to
land in a world-readable temp location (`/data/local/tmp`) first, then be copied in as the app's
own user.
