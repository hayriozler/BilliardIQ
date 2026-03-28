using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.PageModels.PlayerPageModels;

public partial class PlayerProfilePageModel(PlayerRepository PlayerRepo) : BasePageModel
{
    [ObservableProperty]
    public partial Player? ActivePlayer { get; set; }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task Upsert()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        if (ActivePlayer is not null)
        {
            ActivePlayer.Name = FirstName!;
            ActivePlayer.LastName = Surname!;
            ActivePlayer.Level = SelectedLevel;
            await PlayerRepo.UpsertAsync(ActivePlayer);
        }
        else
        {
            await PlayerRepo.UpsertAsync(new Player
            {
                CreatedAt = DateTime.UtcNow,
                LastName = Surname!,
                Name = FirstName!,
                Level = SelectedLevel
            });
        }
        await AppShell.DisplayToastAsync("Player saved");
    }

    private bool CanSave() => !HasErrors;

    [ObservableProperty]
    public partial string ButtonText { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Name required")]
    [MinLength(3, ErrorMessage = "Minimum 3 characters")]
    [NotifyCanExecuteChangedFor(nameof(UpsertCommand))]
    public partial string? FirstName { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Surname required")]
    [MinLength(3, ErrorMessage = "Minimum 3 characters")]
    [NotifyCanExecuteChangedFor(nameof(UpsertCommand))]
    public partial string? Surname { get; set; }

    [ObservableProperty]
    [Required(ErrorMessage = "Level is required")]
    public partial Level SelectedLevel { get; set; }

    public ObservableCollection<Level> Levels { get; } = new(Enum.GetValues<Level>());

    [RelayCommand]
    private async Task Appearing()
    {
        ErrorsChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FirstNameError));
            OnPropertyChanged(nameof(HasFirstNameError));
            OnPropertyChanged(nameof(LastNameError));
            OnPropertyChanged(nameof(HasLastNameError));
        };

        ActivePlayer = await PlayerRepo.GetPlayerAsync();
        if (ActivePlayer is null)
        {
            ButtonText = "Save";
            HeaderText = "Create a new Player";
        }
        else
        {
            FirstName = ActivePlayer.Name;
            Surname = ActivePlayer.LastName;
            SelectedLevel = ActivePlayer.Level;
            ButtonText = "Update";
            HeaderText = "Update Player";
        }
    }

    [ObservableProperty]
    public partial string HeaderText { get; set; }

    public string? FirstNameError => GetErrors(nameof(FirstName)).Cast<object>().FirstOrDefault()?.ToString();
    public string? LastNameError => GetErrors(nameof(Surname)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasFirstNameError => GetErrors(nameof(FirstName)).Cast<object>().Any();
    public bool HasLastNameError => GetErrors(nameof(Surname)).Cast<object>().Any();
}
