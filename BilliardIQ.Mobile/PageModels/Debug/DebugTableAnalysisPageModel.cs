using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Debug;

public partial class DebugTableAnalysisPageModel(
    TableVisionService tableVision,
    IErrorHandler errorHandler) : ObservableObject
{
    // ── Seçili fotoğraf ────────────────────────────────────────────────────────
    [ObservableProperty] private ImageSource? _originalPhoto;
    [ObservableProperty] private string       _photoPath = string.Empty;
    [ObservableProperty] private bool         _hasPhoto;

    // ── Engine seçimi ──────────────────────────────────────────────────────────
    [ObservableProperty] private DetectionEngine _selectedEngine = DetectionEngine.Hough;

    public bool IsColorEngine  => SelectedEngine == DetectionEngine.Color;
    public bool IsHoughEngine  => SelectedEngine == DetectionEngine.Hough;
    public bool IsOnnxEngine   => SelectedEngine == DetectionEngine.Onnx;

    partial void OnSelectedEngineChanged(DetectionEngine value)
    {
        OnPropertyChanged(nameof(IsColorEngine));
        OnPropertyChanged(nameof(IsHoughEngine));
        OnPropertyChanged(nameof(IsOnnxEngine));
    }

    [RelayCommand] private void SelectColor() => SelectedEngine = DetectionEngine.Color;
    [RelayCommand] private void SelectHough() => SelectedEngine = DetectionEngine.Hough;
    [RelayCommand] private void SelectOnnx()  => SelectedEngine = DetectionEngine.Onnx;

    // ── Analiz sonuçları ───────────────────────────────────────────────────────
    [ObservableProperty] private bool         _isAnalyzing;
    [ObservableProperty] private ImageSource? _annotatedPhoto;
    [ObservableProperty] private string       _statusMessage = string.Empty;
    [ObservableProperty] private string       _detailText    = string.Empty;
    [ObservableProperty] private bool         _hasResult;

    // ── Komutlar ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var picks = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions { SelectionLimit = 1 });
            if (!picks.Any()) return;

            var pick      = picks.First();
            PhotoPath     = pick.FullPath;
            OriginalPhoto = ImageSource.FromFile(pick.FullPath);
            HasPhoto      = true;
            HasResult     = false;
            AnnotatedPhoto  = null;
            StatusMessage   = string.Empty;
            DetailText      = string.Empty;
        }
        catch (Exception ex) { errorHandler.HandleError(ex); }
    }

    [RelayCommand]
    private async Task RunAnalysisAsync()
    {
        if (string.IsNullOrEmpty(PhotoPath)) return;

        IsAnalyzing = true;
        HasResult   = false;

        try
        {
            var result = await tableVision.AnalyzeAsync(PhotoPath, SelectedEngine);

            StatusMessage = result.StatusMessage;

            var lines = new List<string>();

            // Toplar: relative + piksel koordinatları
            foreach (var b in result.Balls)
            {
                string name = b.Color switch
                {
                    BallColor.White  => "Beyaz",
                    BallColor.Yellow => "Sarı",
                    BallColor.Red    => "Kırmızı",
                    _                => "?"
                };
                lines.Add($"{name}: rel=({b.CenterX:F3}, {b.CenterY:F3})  px=({b.PixelX}, {b.PixelY})  r={b.PixelRadius}px");
            }

            // Köşeler: piksel koordinatları
            if (result.Corners.Count == 4)
            {
                lines.Add(string.Empty);
                lines.Add("Köşeler (piksel):");
                string[] labels = ["Sol-Üst", "Sağ-Üst", "Sağ-Alt", "Sol-Alt"];
                for (int i = 0; i < 4; i++)
                    lines.Add($"  {labels[i]}: ({result.Corners[i].X:F0}, {result.Corners[i].Y:F0})");
            }

            DetailText = lines.Count > 0 ? string.Join("\n", lines) : "Hiçbir şey algılanamadı.";

            if (result.AnnotatedImage.Length > 0)
            {
                var bytes = result.AnnotatedImage;
                AnnotatedPhoto = ImageSource.FromStream(() => new MemoryStream(bytes));
            }

            HasResult = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
            DetailText    = ex.ToString();
            HasResult     = true;
        }
        finally { IsAnalyzing = false; }
    }
}
