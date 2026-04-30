using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.PageModels.GamePageModels;

public partial class NewGamePageModel : BasePageModel, IQueryAttributable
{
    private readonly GameRepository _gameRepo;
    private readonly ScoreboardOcrService _ocrService;
    private int? _gameId = null;

    private const string _recentLocationsKey = "recent_locations";
    private const int _maxRecentLocations = 3;

    public NewGamePageModel(GameRepository GameRepo, ScoreboardOcrService OcrService)
    {
        _gameRepo = GameRepo;
        _ocrService = OcrService;
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
        LoadRecentLocations();

        if (_gameId is null) return;

        var game = await _gameRepo.GetGameById(_gameId.Value);
        if (game is null) return;

        OpponentName = game.OpponentName;
        Location = game.Location;
        Date = game.Date;
        PlayerScore = game.PlayerScore;
        OpponentScore = game.OpponentScore;
        HighestRun = game.HighestRun;
        InningsText = game.Innings.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        Notes = game.Notes;
        Ball = Balls.FirstOrDefault(b => b.Name == game.Ball);

        if (!string.IsNullOrEmpty(game.ScoreboardPhotoPath) && File.Exists(game.ScoreboardPhotoPath))
        {
            ScoreboardPhotoPath = game.ScoreboardPhotoPath;
            ScoreboardPhotoSource = ImageSource.FromFile(game.ScoreboardPhotoPath);
        }
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
            Location = locationToSave,
            OpponentName = OpponentName,
            Date = Date,
            Ball = Ball?.Name,
            PlayerScore = PlayerScore,
            OpponentScore = OpponentScore,
            HighestRun = HighestRun,
            Innings = ParseInnings(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
            ScoreboardPhotoPath = ScoreboardPhotoPath,
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

            var fileName = $"scoreboard_{DateTime.UtcNow:yyyyMMddHHmmss}_{photo.FileName}";
            var localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            using var sourceStream = await photo.OpenReadAsync();
            using var fileStream = File.OpenWrite(localPath);
            await sourceStream.CopyToAsync(fileStream);

            ScoreboardPhotoPath = localPath;
            ScoreboardPhotoSource = ImageSource.FromFile(localPath);

            await RunOcrAsync(localPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Scoreboard photo error: {ex.Message}");
        }
    }

    private async Task RunOcrAsync(string photoPath)
    {
        IsOcrRunning = true;
        OcrStatusText = null;
        try
        {
            var detected = await _ocrService.ExtractValuesAsync(photoPath);

            if (detected is null)
            {
                OcrStatusText = L["Ocr_Failed"];
                return;
            }

            // Auto-fill all detected fields — user can edit afterwards
            PlayerScore  = detected.Player;
            OpponentScore = detected.Opponent;

            if (detected.Innings is not null)
                InningsText = detected.Innings.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (detected.HighestRun is not null)
                HighestRun = detected.HighestRun.Value;

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
            $"{L["NewGame_MyScore"]}: {d.Player}",
            $"{L["NewGame_OpponentScore"]}: {d.Opponent}"
        };
        if (d.Innings is not null)
            parts.Add($"{L["NewGame_Innings"]}: {d.Innings}");
        if (d.HighestRun is not null)
            parts.Add($"{L["NewGame_HighestRun"]}: {d.HighestRun}");
        return $"✓ {string.Join("  |  ", parts)}";
    }

    [RelayCommand]
    private void RemoveScoreboardPhoto()
    {
        ScoreboardPhotoPath = null;
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
    public partial string? ScoreboardPhotoPath { get; set; }

    [ObservableProperty]
    public partial bool IsOcrRunning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOcrStatus))]
    public partial string? OcrStatusText { get; set; }

    public bool HasOcrStatus => !string.IsNullOrEmpty(OcrStatusText);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScoreboardPhoto))]
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
    public bool HasScoreboardPhoto => ScoreboardPhotoSource is not null;
}
