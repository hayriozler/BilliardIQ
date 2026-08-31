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
    [ObservableProperty] 
    public partial ImageSource? SelectedPhoto { get; set; }
    [ObservableProperty] public partial string  SelectedPhotoPath { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsLoading { get; set; } 
    [ObservableProperty] public partial bool HasPhoto { get; set; }

    // ── OCR bölümü ─────────────────────────────────────────────────────────────
    [ObservableProperty] public partial string RawOcrText { get; set; } = string.Empty;
    [ObservableProperty] public partial string ParsedResult { get; set; } = string.Empty;
    [ObservableProperty] public partial bool   HasOcrResult { get; set; }

    // ── Masa analizi bölümü ────────────────────────────────────────────────────
    [ObservableProperty] public partial ImageSource? AnnotatedPhoto { get; set; }
    [ObservableProperty] public partial string AnalysisStatus { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasAnalysis { get; set; }

    // ── Galeri seçimi ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions { SelectionLimit = 1 });
            if (photos.Count() <= 0) return;

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
