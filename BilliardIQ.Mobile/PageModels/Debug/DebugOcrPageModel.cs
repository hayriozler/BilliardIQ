using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.OCR;

namespace BilliardIQ.Mobile.PageModels.Debug;

public partial class DebugOcrPageModel(
    IOcrService rawOcr,
    TableVisionService tableVision,
    IErrorHandler errorHandler) : ObservableObject
{
    // ── Ortak durum ────────────────────────────────────────────────────────────
    [ObservableProperty] private ImageSource? _selectedPhoto;
    [ObservableProperty] private string       _selectedPhotoPath = string.Empty;
    [ObservableProperty] private bool         _isLoading;
    [ObservableProperty] private bool         _hasPhoto;

    // ── OCR bölümü ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _rawOcrText   = string.Empty;
    [ObservableProperty] private string _parsedResult = string.Empty;
    [ObservableProperty] private bool   _hasOcrResult;

    // ── Masa analizi bölümü ────────────────────────────────────────────────────
    [ObservableProperty] private ImageSource? _annotatedPhoto;
    [ObservableProperty] private string       _analysisStatus = string.Empty;
    [ObservableProperty] private bool         _hasAnalysis;

    // ── Galeri seçimi ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions { SelectionLimit = 1 });
            if (!photos.Any()) return;

            var photo        = photos.First();
            SelectedPhotoPath = photo.FullPath;
            SelectedPhoto    = ImageSource.FromFile(photo.FullPath);
            HasPhoto         = true;

            // Önceki sonuçları temizle
            RawOcrText     = string.Empty;
            ParsedResult   = string.Empty;
            HasOcrResult   = false;
            AnnotatedPhoto = null;
            AnalysisStatus = string.Empty;
            HasAnalysis    = false;
        }
        catch (Exception ex)
        {
            errorHandler.HandleError(ex);
        }
    }

    // ── OCR çalıştır ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RunOcrAsync()
    {
        if (string.IsNullOrEmpty(SelectedPhotoPath)) return;

        IsLoading    = true;
        HasOcrResult = false;

        try
        {
            await rawOcr.InitAsync();
            var raw    = await File.ReadAllBytesAsync(SelectedPhotoPath);
            var bytes  = await Task.Run(() => ImagePreprocessor.NormalizeToJpeg(raw));
            var result = await rawOcr.RecognizeTextAsync(bytes, tryHard: true);

            RawOcrText = result.Success
                ? (string.IsNullOrWhiteSpace(result.AllText) ? "(metin bulunamadı)" : result.AllText)
                : "(OCR başarısız)";

            if (!result.Success) { HasOcrResult = true; return; }

            var parsed = await ScoreboardOcrService.ExtractValuesAsync(result);
            ParsedResult = parsed is null
                ? "Parse başarısız — yapılandırılmış değer bulunamadı."
                : $"Oyuncu 1: {parsed.Player1Score}\n"   +
                  $"Oyuncu 2: {parsed.Player2Score}\n"   +
                  $"El:       {parsed.Innings?.ToString() ?? "-"}\n"    +
                  $"Ortalama: {parsed.Average?.ToString("F3") ?? "-"}\n"+
                  $"En iyi:   {parsed.HighestRun?.ToString() ?? "-"}\n" +
                  $"Top:      {parsed.PlayerBall?.ToString() ?? "-"}";

            HasOcrResult = true;
        }
        catch (Exception ex)
        {
            RawOcrText   = $"İstisna: {ex.Message}";
            ParsedResult = string.Empty;
            HasOcrResult = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Masa analizi çalıştır ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task AnalyzeTableAsync()
    {
        if (string.IsNullOrEmpty(SelectedPhotoPath)) return;

        IsLoading   = true;
        HasAnalysis = false;

        try
        {
            var result = await tableVision.AnalyzeAsync(SelectedPhotoPath);

            AnalysisStatus = result.StatusMessage;
            HasAnalysis    = true;

            if (result.AnnotatedImage.Length > 0)
            {
                // Annotated görseli ImageSource'a çevir
                var bytes = result.AnnotatedImage;
                AnnotatedPhoto = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
        }
        catch (Exception ex)
        {
            AnalysisStatus = $"Hata: {ex.Message}";
            HasAnalysis    = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
