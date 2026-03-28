using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.PageModels.Analyzers;

namespace BilliardIQ.Mobile.Pages.Analyzers;
public partial class PhotoAnalyzerViewPage : BasePage
{
    private readonly string imagePath;
    private readonly IFileSystem _fileSystem;

    public PhotoAnalyzerViewPage(PhotoAnalyzerPageModel analyzerPageModel,
        IFileSystem fileSystem) : base(analyzerPageModel) {
        
        InitializeComponent();
        _fileSystem= fileSystem;
        imagePath = Path.Combine(fileSystem.CacheDirectory, "camera-view-image.jpg");
        Camera.MediaCaptured +=OnMediaCaptured;
      }

    private void OnMediaCaptured(object? sender, CommunityToolkit.Maui.Core.MediaCapturedEventArgs e)
    {
        using var localFileStream = File.Create(imagePath);

        e.Media.CopyTo(localFileStream);

        Dispatcher.Dispatch(() =>
        {
            // workaround for https://github.com/dotnet/maui/issues/13858
#if ANDROID
            image.Source = ImageSource.FromStream(() => File.OpenRead(imagePath));
#else
			image.Source = ImageSource.FromFile(imagePath);
#endif

            debugText.Text = $"Image saved to {imagePath}";
        });
    }
    private async void OnCloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    protected override void OnAppearing() => base.OnAppearing();

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