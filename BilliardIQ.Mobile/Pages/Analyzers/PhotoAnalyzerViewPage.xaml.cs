using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.PageModels.Analyzers;

namespace BilliardIQ.Mobile.Pages.Analyzers;
public partial class PhotoAnalyzerViewPage : BasePage
{
    private CancellationTokenSource? _cameraCts;

    private readonly string _photosFolder;

    public PhotoAnalyzerViewPage(PhotoAnalyzerPageModel analyzerPageModel,
        IFileSystem fileSystem) : base(analyzerPageModel)
    {
        InitializeComponent();
        _photosFolder = Path.Combine(fileSystem.AppDataDirectory, "Photos");
        Directory.CreateDirectory(_photosFolder);
        Camera.MediaCaptured += OnMediaCaptured;
    }

    private void OnMediaCaptured(object? sender, CommunityToolkit.Maui.Core.MediaCapturedEventArgs e)
    {
        var fileName = $"billiardiq_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        var savedPath = Path.Combine(_photosFolder, fileName);

        using var localFileStream = File.Create(savedPath);
        e.Media.CopyTo(localFileStream);

        Dispatcher.Dispatch(() =>
        {
#if ANDROID
            image.Source = ImageSource.FromStream(() => File.OpenRead(savedPath));
#else
            image.Source = ImageSource.FromFile(savedPath);
#endif
            debugText.Text = $"Saved: {fileName}";
        });
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Camera.HandlerChanged -= OnCameraHandlerChanged;
        _cameraCts?.Cancel();
        _cameraCts?.Dispose();
        _cameraCts = null;
        Camera.StopCameraPreview();
    }

    //private async Task Capture()
    //{
    //    try
    //    {
    //        var captureImageCTS = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    //        var stream = await camera.CaptureImage(captureImageCTS.Token);
    //        if (stream is not null)
    //        {
    //            await stream.DisposeAsync();
    //            await Shell.Current.DisplayAlertAsync("Başarılı", "Fotoğraf simülatörde yakalandı!", "Tamam");
    //        }
    //    }
    //    catch (Exception ex)
    //    {

    //    }
    //}
}