using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.PageModels.GamePageModels;

public partial class NewGamePageModel : BasePageModel, IQueryAttributable
{
    private readonly GameRepository _gameRepo;
    private int? _gameId = null;

    public NewGamePageModel(GameRepository GameRepo)
    {
        _gameRepo = GameRepo;
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
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Please select a ball")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial ImageData? Ball { get; set; }

    [ObservableProperty]
    private ObservableCollection<ImageData> _balls =
    [
        new() { Name = "White", Image = ImageSource.FromFile("whiteball.svg"), IsBallSelected = false },
        new() { Name = "Yellow", Image = ImageSource.FromFile("yellowball.svg"), IsBallSelected = false }
    ];

    [RelayCommand]
    private async Task Appearing()
    {
        if (_gameId is null) return;

        var game = await _gameRepo.GetGameById(_gameId.Value);
        if (game is null) return;

        OpponentName = game.OpponentName;
        Location = game.Location;
        Date = game.Date;
        PlayerScore = game.PlayerScore;
        OpponentScore = game.OpponentScore;
        Ball = Balls.FirstOrDefault(b => b.Name == game.Ball);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task Save()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        await _gameRepo.UpsertGameAsync(_gameId, new Game
        {
            Location = Location,
            OpponentName = OpponentName,
            Date = Date,
            Ball = Ball?.Name,
            PlayerScore = PlayerScore,
            OpponentScore = OpponentScore
        });

        await Shell.Current.GoToAsync("..");
    }

    private bool CanSave() => !HasErrors;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("gameId", out object? value))
            _gameId = (int)value;
        ButtonText = _gameId is null ? "Save" : "Update";
        OnPropertyChanged(nameof(ButtonText));
    }

    [ObservableProperty]
    public partial string? ButtonText { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Opponent name is required")]
    [MinLength(2, ErrorMessage = "Minimum 2 characters")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? OpponentName { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Location is required")]
    [MinLength(2, ErrorMessage = "Minimum 2 characters")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string? Location { get; set; }

    [ObservableProperty]
    public partial int PlayerScore { get; set; }

    [ObservableProperty]
    public partial int OpponentScore { get; set; }

    [ObservableProperty]
    public partial DateTime Date { get; set; }

    [ObservableProperty]
    public partial DateTime MinimumDate { get; set; } = DateTime.Now.AddDays(-7);

    [ObservableProperty]
    public partial DateTime MaximumDate { get; set; } = DateTime.Now.AddDays(7);

    [RelayCommand]
    private void ImageSelected(ImageData image)
    {
        SemanticScreenReader.Announce($"{image.Name} selected");
    }

    public string? OpponentNameError => GetErrors(nameof(OpponentName)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasOpponentNameError => GetErrors(nameof(OpponentName)).Cast<object>().Any();
    public string? LocationError => GetErrors(nameof(Location)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasLocationError => GetErrors(nameof(Location)).Cast<object>().Any();
    public string? BallError => GetErrors(nameof(Ball)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasBallError => GetErrors(nameof(Ball)).Cast<object>().Any();
}
