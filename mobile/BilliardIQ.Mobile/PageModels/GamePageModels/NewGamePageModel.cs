using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.OCR;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.PageModels.GamePageModels;

public partial class NewGamePageModel : BasePageModel, IQueryAttributable
{
    private readonly GameRepository _gameRepo;
    private readonly IOcrService _ocrService;
    private int? _gameId = null;

    private const string _recentLocationsKey = "recent_locations";
    private const int _maxRecentLocations = 3;

    public NewGamePageModel(GameRepository GameRepo, IOcrService ocrService)
    {
        _gameRepo = GameRepo;
        _ocrService = ocrService;
        Date = DateTime.Today;
        MinimumDate = DateTime.Today.AddDays(-7);
        MaximumDate = DateTime.Today.AddDays(7);

        ErrorsChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(OpponentNameError));
            OnPropertyChanged(nameof(HasOpponentNameError));
            OnPropertyChanged(nameof(LocationError));
            OnPropertyChanged(nameof(HasLocationError));
            OnPropertyChanged(nameof(BallError));
            OnPropertyChanged(nameof(HasBallError));
        };

        LocalizationManager.Instance.PropertyChanged += (_, _) =>
            OnPropertyChanged(nameof(PageTitle));
    }

    public string PageTitle => _gameId is null ? L["NewGame_Title"] : L["NewGame_UpdateTitle"];

    // ── Recent locations ─────────────────────────────────────────────────────
    public ObservableCollection<string> RecentLocations { get; } = [];
    public bool HasRecentLocations => RecentLocations.Count > 0;

    private void LoadRecentLocations()
    {
        RecentLocations.Clear();
        var stored = Preferences.Default.Get(_recentLocationsKey, string.Empty);
        foreach (var loc in stored.Split('|', StringSplitOptions.RemoveEmptyEntries).Take(_maxRecentLocations))
            RecentLocations.Add(loc);
        OnPropertyChanged(nameof(HasRecentLocations));
    }

    private static void SaveRecentLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location)) return;
        location = location.Trim();

        var existing = Preferences.Default.Get(_recentLocationsKey, string.Empty)
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !l.Equals(location, StringComparison.OrdinalIgnoreCase))
            .Prepend(location)
            .Take(_maxRecentLocations);

        Preferences.Default.Set(_recentLocationsKey, string.Join('|', existing));
    }

    [RelayCommand]
    private void SelectLocation(string location) => Location = location;

    // ── Form lifecycle ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Please select a ball")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial ImageData? Ball { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ImageData> Balls { get; set; } =
    [
        new() { Name = "White", Image = ImageSource.FromFile("whiteball.svg"), IsBallSelected = false },
        new() { Name = "Yellow", Image = ImageSource.FromFile("yellowball.svg"), IsBallSelected = false }
    ];

    [RelayCommand]
    private async Task Appearing()
    {
        if (_gameId is null) return;
        LoadRecentLocations();
        var game = await _gameRepo.GetGameByIdAsync(_gameId.Value);
        if (game is null) return;

        OpponentName = game.Opponent;
        Location = game.Location;
        Date = game.Date;
        PlayerScore = game.PlayerScore;
        OpponentScore = game.OpponentScore;
        HighestRun = game.HighestRun;
        InningsText = game.Innings.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        Notes = game.Notes;
        Ball = Balls.FirstOrDefault(b => b.Name == game.Ball);

        if (game.ScoreboardThumbnail is { Length: > 0 })
            SetThumbnail(game.ScoreboardThumbnail);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task Save()
    {
        // Normalize empty location to null before validation
        if (string.IsNullOrWhiteSpace(Location)) Location = null;

        ValidateAllProperties();
        if (HasErrors) return;

        var locationToSave = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim();

        await _gameRepo.UpsertGameAsync(_gameId, new Game
        {
            Location            = locationToSave,
            Opponent            = OpponentName,
            Date                = Date,
            Ball                = Ball?.Name,
            PlayerScore         = PlayerScore,
            OpponentScore       = OpponentScore,
            HighestRun          = HighestRun,
            Innings             = ParseInnings(),
            Notes               = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
            ScoreboardThumbnail = ScoreboardThumbnail,
        });

        if (locationToSave is not null)
            SaveRecentLocation(locationToSave);

        await Shell.Current.GoToAsync("..");
    }

    private bool CanSave() => !HasErrors;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _gameId = query.TryGetValue("gameId", out object? value) ? (int)value : null;
        OnPropertyChanged(nameof(IsNewGame));
        OnPropertyChanged(nameof(IsExistingGame));
        OnPropertyChanged(nameof(PageTitle));
    }

    public bool IsNewGame => _gameId is null;
    public bool IsExistingGame => _gameId is not null;

    // ── Scoreboard photo ──────────────────────────────────────────────────────
    [RelayCommand]
    private async Task TakeScoreboardPhoto()
    {
        try
        {
#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted) return;
#endif
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return;
            await ApplyScoreboardPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Scoreboard camera error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PickScoreboardPhoto()
    {
        try
        {
#if ANDROID
            // Android 13+ uses READ_MEDIA_IMAGES; older uses READ_EXTERNAL_STORAGE.
            // Permissions.Photos abstracts both.
            var status = await Permissions.RequestAsync<Permissions.Photos>();
            if (status != PermissionStatus.Granted) return;
#endif
            var photos = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions { Title = L["NewGame_ScoreboardPhoto"] });
            if (photos is null || photos.Count==0) return;
            await ApplyScoreboardPhotoAsync(photos.First());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Scoreboard pick error: {ex.Message}");
        }
    }

    private async Task ApplyScoreboardPhotoAsync(FileResult photo)
    {
        using var stream = await photo.OpenReadAsync();
        using var ms     = new MemoryStream();
        await stream.CopyToAsync(ms);
        var originalBytes = ms.ToArray();

        // Thumbnail stored in DB — no file copy needed
        var thumb = await Task.Run(() => ImagePreprocessor.CreateThumbnail(originalBytes));
        SetThumbnail(thumb.Length > 0 ? thumb : originalBytes);

        await RunOcrAsync(originalBytes);
    }

    private void SetThumbnail(byte[] bytes)
    {
        ScoreboardThumbnail   = bytes;
        ScoreboardPhotoSource = ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    private async Task RunOcrAsync(byte[] originalBytes)
    {
        IsOcrRunning = true;
        OcrStatusText = null;
        try
        {
            await _ocrService.InitAsync();
            // Normalize HEIC/WebP → JPEG so ML Kit can always decode the image
            var bytes  = await Task.Run(() => ImagePreprocessor.NormalizeToJpeg(originalBytes));
            var result = await _ocrService.RecognizeTextAsync(bytes, tryHard: true);
            if (!result.Success)
            {
                OcrStatusText = L["Ocr_Failed"];
                return;
            }

            var detected = await ScoreboardOcrService.ExtractValuesAsync(result);
            if (detected is null)
            {
                OcrStatusText = L["Ocr_Failed"];
                return;
            }

            // Auto-fill all detected fields — user can edit afterwards
            PlayerScore   = detected.Player1Score;
            OpponentScore = detected.Player2Score;

            // Prefer the billiard average (e.g. 0.422); fall back to innings count
            if (detected.Average is not null)
                InningsText = detected.Average.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            else if (detected.Innings is not null)
                InningsText = detected.Innings.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (detected.HighestRun is not null)
                HighestRun = detected.HighestRun.Value;

            // Auto-select ball when confident
            if (detected.PlayerBall is not null)
            {
                var ballName = detected.PlayerBall.ToString(); // "White" or "Yellow"
                var match = Balls.FirstOrDefault(b => b.Name == ballName);
                if (match is not null) Ball = match;
            }

            OcrStatusText = BuildOcrSummary(detected);
        }
        finally
        {
            IsOcrRunning = false;
        }
    }

    private string BuildOcrSummary(OcrDetectedValues d)
    {
        var parts = new List<string>
        {
            $"{L["NewGame_MyScore"]}: {d.Player1Score}",
            $"{L["NewGame_OpponentScore"]}: {d.Player2Score}"
        };
        if (d.Average is not null)
            parts.Add($"{L["NewGame_Innings"]}: {d.Average:F3}");
        else if (d.Innings is not null)
            parts.Add($"{L["NewGame_Innings"]}: {d.Innings}");
        if (d.HighestRun is not null)
            parts.Add($"{L["NewGame_HighestRun"]}: {d.HighestRun}");
        if (d.PlayerBall is not null)
            parts.Add($"Top: {d.PlayerBall}");
        return $"✓ {string.Join("  |  ", parts)}";
    }

    [RelayCommand]
    private void RemoveScoreboardPhoto()
    {
        ScoreboardThumbnail   = null;
        ScoreboardPhotoSource = null;
    }

    // ── Observable properties ─────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Opponent name is required")]
    [MinLength(2, ErrorMessage = "Minimum 2 characters")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? OpponentName { get; set; }

    // Optional — if provided must be at least 3 chars
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MinLength(3, ErrorMessage = "Minimum 3 characters")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? Location { get; set; }

    [ObservableProperty]
    public partial int PlayerScore { get; set; }

    [ObservableProperty]
    public partial int OpponentScore { get; set; }

    [ObservableProperty]
    public partial int HighestRun { get; set; }

    [ObservableProperty]
    public partial string InningsText { get; set; } = "0";

    [ObservableProperty]
    public partial string? Notes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScoreboardPhoto))]
    public partial byte[]? ScoreboardThumbnail { get; set; }

    [ObservableProperty]
    public partial bool IsOcrRunning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOcrStatus))]
    public partial string? OcrStatusText { get; set; }

    public bool HasOcrStatus => !string.IsNullOrEmpty(OcrStatusText);

    [ObservableProperty]
    public partial ImageSource? ScoreboardPhotoSource { get; set; }

    [ObservableProperty]
    public partial DateTime Date { get; set; }

    [ObservableProperty]
    public partial DateTime MinimumDate { get; set; } = DateTime.Now.AddDays(-7);

    [ObservableProperty]
    public partial DateTime MaximumDate { get; set; } = DateTime.Now.AddDays(7);

    [RelayCommand]
    private static void ImageSelected(ImageData image) =>
        SemanticScreenReader.Announce($"{image.Name} selected");

    private double ParseInnings()
    {
        var normalized = (InningsText ?? "0").Replace(',', '.');
        return double.TryParse(normalized,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result) ? Math.Max(0, result) : 0;
    }

    public string? OpponentNameError => GetErrors(nameof(OpponentName)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasOpponentNameError => GetErrors(nameof(OpponentName)).Cast<object>().Any();
    public string? LocationError => GetErrors(nameof(Location)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasLocationError => GetErrors(nameof(Location)).Cast<object>().Any();
    public string? BallError => GetErrors(nameof(Ball)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasBallError => GetErrors(nameof(Ball)).Cast<object>().Any();
    public bool HasScoreboardPhoto => ScoreboardThumbnail is { Length: > 0 };
}
