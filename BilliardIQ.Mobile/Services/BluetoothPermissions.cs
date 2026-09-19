namespace BilliardIQ.Mobile.Services;

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
