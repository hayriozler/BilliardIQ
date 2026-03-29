using BilliardIQ.Mobile.PageModels.Analyzers;

namespace BilliardIQ.Mobile.Pages.Analyzers;

file sealed class GridDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.StrokeColor = Color.FromRgba(255, 255, 255, 64);
        canvas.StrokeSize = 1;

        canvas.DrawLine(rect.Width / 3, 0, rect.Width / 3, rect.Height);
        canvas.DrawLine(rect.Width * 2 / 3, 0, rect.Width * 2 / 3, rect.Height);
        canvas.DrawLine(0, rect.Height / 3, rect.Width, rect.Height / 3);
        canvas.DrawLine(0, rect.Height * 2 / 3, rect.Width, rect.Height * 2 / 3);
    }
}

public partial class PhotoAnalyzerViewPage : BasePage
{
    private CancellationTokenSource? _cameraCts;
    private double _zoomAtPinchStart = 1.0;
    private readonly string _photosFolder;
    private readonly PhotoAnalyzerPageModel _pageModel;

    public PhotoAnalyzerViewPage(PhotoAnalyzerPageModel analyzerPageModel,
        IFileSystem fileSystem) : base(analyzerPageModel)
    {
        _pageModel = analyzerPageModel;
        Padding = 0;
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
        InitializeComponent();
        gridOverlay.Drawable = new GridDrawable();
        _photosFolder = Path.Combine(fileSystem.AppDataDirectory, "Photos");
        Directory.CreateDirectory(_photosFolder);
        Camera.MediaCaptured += OnMediaCaptured;
        _pageModel.CaptureRequested += OnCaptureRequested;
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
            thumbnail.Source = ImageSource.FromStream(() => File.OpenRead(savedPath));
#else
            thumbnail.Source = ImageSource.FromFile(savedPath);
#endif
            thumbnailBorder.IsVisible = true;
            debugText.Text = $"Saved: {fileName}";
        });
    }

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
            values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, "Pictures/BilliardIQ");
        }
        else
        {
            var picturesPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                Android.OS.Environment.DirectoryPictures)!.AbsolutePath;
            var destDir = Path.Combine(picturesPath, "BilliardIQ");
            Directory.CreateDirectory(destDir);
            values.Put(Android.Provider.MediaStore.IMediaColumns.Data, Path.Combine(destDir, fileName));
        }

        var uri = resolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, values);
        if (uri is null) return;

        using var outStream = resolver.OpenOutputStream(uri);
        if (outStream is null) return;

        using var inStream = File.OpenRead(filePath);
        inStream.CopyTo(outStream);
    }
#endif

    private async void OnCaptureRequested(object? sender, EventArgs e)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await Camera.CaptureImage(cts.Token);
        }
        catch (Exception ex)
        {
            Dispatcher.Dispatch(() => debugText.Text = $"Capture failed: {ex.Message}");
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
            _zoomAtPinchStart = Camera.ZoomFactor;

        if (e.Status == GestureStatus.Running)
        {
            var newZoom = _zoomAtPinchStart * e.Scale;
            Camera.ZoomFactor = (float)Math.Max(1.0, Math.Min(8.0, newZoom));
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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
        ((Android.App.Activity)Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!)
            .RequestedOrientation = Android.Content.PM.ScreenOrientation.Unspecified;
#endif
        captureButton.IsEnabled = false;
        captureButton.Opacity = 0.4;
        thumbnail.Source = null;
        thumbnailBorder.IsVisible = false;
        Camera.HandlerChanged -= OnCameraHandlerChanged;
        _cameraCts?.Cancel();
        _cameraCts?.Dispose();
        _cameraCts = null;
        Camera.StopCameraPreview();
    }
}
