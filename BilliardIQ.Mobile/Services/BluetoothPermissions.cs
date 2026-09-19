namespace BilliardIQ.Mobile.Services;

// Android 12+ (API 31) moved BLUETOOTH_SCAN/BLUETOOTH_CONNECT to runtime-requested
// permissions even though they're also declared in AndroidManifest.xml — without this,
// scanning throws Java.Lang.SecurityException from ScanBinder.registerScanner().
public class BluetoothPermissions : Permissions.BasePlatformPermission
{
#if ANDROID
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(31)
            ? new[]
              {
                  (Android.Manifest.Permission.BluetoothScan, true),
                  (Android.Manifest.Permission.BluetoothConnect, true),
              }
            : new[]
              {
                  (Android.Manifest.Permission.Bluetooth, true),
                  (Android.Manifest.Permission.BluetoothAdmin, true),
                  (Android.Manifest.Permission.AccessFineLocation, true),
              };
#endif
}
