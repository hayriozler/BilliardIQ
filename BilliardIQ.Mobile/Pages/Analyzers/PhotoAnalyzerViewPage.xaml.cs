using BilliardIQ.Mobile.PageModels.Analyzers;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BilliardIQ.Mobile.Pages.Analyzers;

public sealed class CapturedPhoto : INotifyPropertyChanged
{
    private bool _isSelected;
    public required string FilePath { get; init; }
    public required ImageSource Source { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class PhotoAnalyzerViewPage : BasePage
{
    private CancellationTokenSource? _cameraCts;
    private double _zoomAtPinchStart = 1.0;
    private readonly string _photosFolder;
    private readonly PhotoAnalyzerPageModel _pageModel;
    private readonly ObservableCollection<CapturedPhoto> _photos = [];
    private CapturedPhoto? _selectedPhoto;

    public PhotoAnalyzerViewPage(PhotoAnalyzerPageModel analyzerPageModel,
        IFileSystem fileSystem) : base(analyzerPageModel)
    {
        _pageModel = analyzerPageModel;
        Padding = 0;
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
        InitializeComponent();
        _photosFolder = Path.Combine(fileSystem.AppDataDirectory, "BilliardIQ", "Photos");
        Directory.CreateDirectory(_photosFolder);
        Camera.MediaCaptured += OnMediaCaptured;
        _pageModel.CaptureRequested += OnCaptureRequested;
        photoStrip.ItemsSource = _photos;
    }

    private void OnMediaCaptured(object? sender, CommunityToolkit.Maui.Core.MediaCapturedEventArgs e)
    {
        var fileName = $"billiardiq_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        var savedPath = Path.Combine(_photosFolder, fileName);

        using (var localFileStream = File.Create(savedPath))
            e.Media.CopyTo(localFileStream);

#if ANDROID
        SaveToMediaStore(savedPath, fileName);
#endif

        Dispatcher.Dispatch(() =>
        {
#if ANDROID
            var source = ImageSource.FromStream(() => File.OpenRead(savedPath));
#else
            var source = ImageSource.FromFile(savedPath);
#endif
            var photo = new CapturedPhoto { FilePath = savedPath, Source = source };
            _photos.Add(photo);

            // Show captured photo full screen immediately
            fullPhoto.Source = photo.Source;
            photoViewer.IsVisible = true;
        });
    }

    private void OnPhotoSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_selectedPhoto is not null)
            _selectedPhoto.IsSelected = false;

        _selectedPhoto = e.CurrentSelection.FirstOrDefault() as CapturedPhoto;
        if (_selectedPhoto is not null)
            _selectedPhoto.IsSelected = true;

        photoActions.IsVisible = _selectedPhoto is not null;
    }

    private void OnDeletePhoto(object? sender, EventArgs e)
    {
        if (_selectedPhoto is null) return;

        var photo = _selectedPhoto;
        _selectedPhoto = null;
        photoActions.IsVisible = false;
        photoStrip.SelectedItem = null;
        _photos.Remove(photo);

        if (File.Exists(photo.FilePath))
            File.Delete(photo.FilePath);

#if ANDROID
        DeleteFromMediaStore(Path.GetFileName(photo.FilePath));
#endif
    }

    private void OnPhotoViewerClose(object? sender, EventArgs e)
    {
        photoViewer.IsVisible = false;
        fullPhoto.Source = null;
    }

    // ── Gestures ──────────────────────────────────────────────────────────────

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
            _zoomAtPinchStart = Camera.ZoomFactor;

        if (e.Status == GestureStatus.Running)
            Camera.ZoomFactor = (float)Math.Clamp(_zoomAtPinchStart * e.Scale, 1.0, 8.0);
    }

    private async void OnCameraViewTapped(object? sender, TappedEventArgs e)
    {
        var pos = e.GetPosition(cameraOverlay);
        if (pos is null) return;

        // Position focus indicator at tap point and animate
        focusIndicator.TranslationX = pos.Value.X - cameraOverlay.Width / 2;
        focusIndicator.TranslationY = pos.Value.Y - cameraOverlay.Height / 2;
        focusIndicator.Opacity = 1;
        focusIndicator.IsVisible = true;

#if ANDROID
        TryFocusAt((float)pos.Value.X, (float)pos.Value.Y);
#endif

        await focusIndicator.FadeTo(0, 900);
        focusIndicator.IsVisible = false;
    }

#if ANDROID
    // Dispatch a synthetic tap to the native CameraX PreviewView so that its
    // built-in tap-to-focus handler fires at the requested coordinates.
    private void TryFocusAt(float xDp, float yDp)
    {
        try
        {
            if (Camera.Handler?.PlatformView is not Android.Views.View nativeView) return;

            var density = (float)DeviceDisplay.Current.MainDisplayInfo.Density;
            var xPx = xDp * density;
            var yPx = yDp * density;
            var now = Java.Lang.JavaSystem.CurrentTimeMillis();

            var down = Android.Views.MotionEvent.Obtain(
                now, now, Android.Views.MotionEventActions.Down, xPx, yPx, 0);
            var up = Android.Views.MotionEvent.Obtain(
                now, now + 80, Android.Views.MotionEventActions.Up, xPx, yPx, 0);

            nativeView.DispatchTouchEvent(down);
            nativeView.DispatchTouchEvent(up);

            down?.Recycle();
            up?.Recycle();
        }
        catch { }
    }
#endif

    // ── Camera lifecycle ──────────────────────────────────────────────────────

    private async void OnCaptureRequested(object? sender, EventArgs e)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await Camera.CaptureImage(cts.Token);
        }
        catch { }
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync(animated: false);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
        SetFullScreen(true);
#endif
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted) return;

        if (Camera.Handler is not null)
            StartPreview();
        else
            Camera.HandlerChanged += OnCameraHandlerChanged;
    }

    private void OnCameraHandlerChanged(object? sender, EventArgs e)
    {
        Camera.HandlerChanged -= OnCameraHandlerChanged;
        if (Camera.Handler is not null)
            StartPreview();
    }

    private void StartPreview()
    {
        _cameraCts = new CancellationTokenSource();
        Camera.StartCameraPreview(_cameraCts.Token);
        captureButton.IsEnabled = true;
        captureButton.Opacity = 1;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
#if ANDROID
        SetFullScreen(false);
        ((Android.App.Activity)Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!)
            .RequestedOrientation = Android.Content.PM.ScreenOrientation.Unspecified;
#endif
        captureButton.IsEnabled = false;
        captureButton.Opacity = 0.4;
        _photos.Clear();
        _selectedPhoto = null;
        photoActions.IsVisible = false;
        photoViewer.IsVisible = false;
        Camera.HandlerChanged -= OnCameraHandlerChanged;
        _cameraCts?.Cancel();
        _cameraCts?.Dispose();
        _cameraCts = null;
        Camera.StopCameraPreview();
    }

#if ANDROID
    private static void SetFullScreen(bool enable)
    {
        var window = ((Android.App.Activity)Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!).Window!;
        var contentView = window.DecorView.FindViewById(Android.Resource.Id.Content);
        if (enable)
        {
            // Stop Android from adding status-bar/nav-bar insets to the content area
            contentView?.SetFitsSystemWindows(false);
#pragma warning disable CA1422
            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)(
                (int)Android.Views.SystemUiFlags.LayoutStable |
                (int)Android.Views.SystemUiFlags.LayoutFullscreen |
                (int)Android.Views.SystemUiFlags.LayoutHideNavigation |
                (int)Android.Views.SystemUiFlags.Fullscreen |
                (int)Android.Views.SystemUiFlags.HideNavigation |
                (int)Android.Views.SystemUiFlags.ImmersiveSticky);
#pragma warning restore CA1422
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var controller = window.InsetsController;
                if (controller is not null)
                {
                    controller.SystemBarsBehavior = (int)Android.Views.WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                    controller.Hide(Android.Views.WindowInsets.Type.SystemBars());
                }
            }
        }
        else
        {
            contentView?.SetFitsSystemWindows(true);
#pragma warning disable CA1422
            window.DecorView.SystemUiVisibility = Android.Views.StatusBarVisibility.Visible;
#pragma warning restore CA1422
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
                window.InsetsController?.Show(Android.Views.WindowInsets.Type.SystemBars());
        }
    }
#endif

#if ANDROID
    private static void SaveToMediaStore(string filePath, string fileName)
    {
        var resolver = Android.App.Application.Context.ContentResolver;
        if (resolver is null) return;

        var values = new Android.Content.ContentValues();
        values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "image/jpeg");

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
        {
#pragma warning disable CA1416
            values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, "Pictures/BilliardIQ");
#pragma warning restore CA1416
        }
        else
        {
            var picturesPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                Android.OS.Environment.DirectoryPictures)!.AbsolutePath;
            var destDir = Path.Combine(picturesPath, "BilliardIQ");
            Directory.CreateDirectory(destDir);
            values.Put(Android.Provider.MediaStore.IMediaColumns.Data, Path.Combine(destDir, fileName));
        }

        var uri = resolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri!, values);
        if (uri is null) return;

        using var outStream = resolver.OpenOutputStream(uri);
        if (outStream is null) return;

        using var inStream = File.OpenRead(filePath);
        inStream.CopyTo(outStream);
    }

    private static void DeleteFromMediaStore(string fileName)
    {
        var resolver = Android.App.Application.Context.ContentResolver;
        if (resolver is null) return;

        resolver.Delete(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri!,
            Android.Provider.MediaStore.IMediaColumns.DisplayName + " = ?",
            [fileName]);
    }
#endif
}
