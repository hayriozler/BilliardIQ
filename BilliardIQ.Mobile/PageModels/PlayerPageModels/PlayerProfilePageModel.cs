using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Pages.Players;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace BilliardIQ.Mobile.PageModels.PlayerPageModels;

public partial class PlayerProfilePageModel(PlayerRepository PlayerRepo, LocationRepository LocationRepo, IServiceProvider Services) : BasePageModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateMode))]
    public partial Player? ActivePlayer { get; set; }

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task Upsert()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        var player = ActivePlayer ?? new Player { CreatedAt = DateTime.UtcNow };
        player.Name = FirstName!;
        player.LastName = Surname!;
        player.Level = SelectedLevel;
        player.Country = SelectedCountry?.Name ?? string.Empty;
        player.City = SelectedCity?.Name ?? string.Empty;
        player.Club = string.IsNullOrWhiteSpace(Club) ? null : Club;
        player.Association = string.IsNullOrWhiteSpace(Association) ? null : Association;
        player.Email = Email!;
        player.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone;

        await PlayerRepo.UpsertAsync(player);
        _formLoaded = false; // force fresh reload next time page appears
        await AppShell.DisplayToastAsync(L["Profile_Saved"]);
    }

    private bool CanSave() => !HasErrors;

    [ObservableProperty]
    public partial string ButtonText { get; set; } = "Save";

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

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Country is required")]
    [NotifyCanExecuteChangedFor(nameof(UpsertCommand))]
    public partial CountryItem? SelectedCountry { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "City is required")]
    [NotifyCanExecuteChangedFor(nameof(UpsertCommand))]
    [NotifyPropertyChangedFor(nameof(CityDisplayText))]
    public partial CityItem? SelectedCity { get; set; }

    public string CityDisplayText => SelectedCity?.Name ?? L["Profile_SelectCity"];

    [ObservableProperty]
    public partial string? Club { get; set; }

    [ObservableProperty]
    public partial string? Association { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [NotifyCanExecuteChangedFor(nameof(UpsertCommand))]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial string? Phone { get; set; }

    // Language selection
    public ObservableCollection<string> Languages { get; } = ["English", "Türkçe"];

    [ObservableProperty]
    public partial string SelectedLanguageName { get; set; } =
        LocalizationManager.Instance.CurrentLanguage == "tr" ? "Türkçe" : "English";

    partial void OnSelectedLanguageNameChanged(string value)
    {
        var code = value == "Türkçe" ? "tr" : "en";
        LocalizationManager.Instance.SetLanguage(code);
        // Refresh localized labels
        OnPropertyChanged(nameof(L));
        ButtonText = ActivePlayer is null ? L["Action_Save"] : L["Action_Update"];
        HeaderText = ActivePlayer is null ? L["Profile_CreateHeader"] : L["Profile_UpdateHeader"];
    }

    public ObservableCollection<Level> Levels { get; } = new(Enum.GetValues<Level>());
    public ObservableCollection<CountryItem> Countries { get; } = [];
    public ObservableCollection<CityItem> Cities { get; } = [];
    private readonly List<CityItem> _allCitiesCache = [];
    private bool _errorsRegistered = false;
    private bool _formLoaded = false;

    partial void OnSelectedCountryChanged(CountryItem? value)
    {
        SelectedCity = null;
        Cities.Clear();
        if (value is null) return;
        foreach (var city in _allCitiesCache.Where(c => c.CountryId == value.Id))
            Cities.Add(city);
    }

    [RelayCommand]
    private async Task SelectCity()
    {
        if (SelectedCountry is null) return;
        var page = Services.GetRequiredService<CitySearchPage>();
        await Shell.Current.Navigation.PushModalAsync(new NavigationPage(page), animated: true);
    }

    [RelayCommand]
    private async Task Appearing()
    {
        // Register validation error handlers once (singleton ViewModel)
        if (!_errorsRegistered)
        {
            ErrorsChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(FirstNameError));
                OnPropertyChanged(nameof(HasFirstNameError));
                OnPropertyChanged(nameof(LastNameError));
                OnPropertyChanged(nameof(HasLastNameError));
                OnPropertyChanged(nameof(CountryError));
                OnPropertyChanged(nameof(HasCountryError));
                OnPropertyChanged(nameof(CityError));
                OnPropertyChanged(nameof(HasCityError));
                OnPropertyChanged(nameof(EmailError));
                OnPropertyChanged(nameof(HasEmailError));
            };
            _errorsRegistered = true;
        }

        // Load location data once
        if (Countries.Count == 0)
        {
            var countries = await LocationRepo.GetCountriesAsync();
            foreach (var c in countries) Countries.Add(c);

            foreach (var country in countries)
            {
                var cities = await LocationRepo.GetCitiesByCountryIdAsync(country.Id);
                _allCitiesCache.AddRange(cities);
            }
        }

        // Load player form data only on first appearance or after a save
        if (_formLoaded) return;

        ActivePlayer = await PlayerRepo.GetPlayerAsync();
        if (ActivePlayer is null)
        {
            ButtonText = L["Action_Save"];
            HeaderText = L["Profile_CreateHeader"];
        }
        else
        {
            FirstName = ActivePlayer.Name;
            Surname = ActivePlayer.LastName;
            SelectedLevel = ActivePlayer.Level;
            Club = ActivePlayer.Club;
            Association = ActivePlayer.Association;
            Email = ActivePlayer.Email;
            Phone = ActivePlayer.Phone;
            // Setting SelectedCountry fires OnSelectedCountryChanged which populates Cities
            SelectedCountry = Countries.FirstOrDefault(c => c.Name == ActivePlayer.Country);
            SelectedCity = Cities.FirstOrDefault(c => c.Name == ActivePlayer.City);
            ButtonText = L["Action_Update"];
            HeaderText = L["Profile_UpdateHeader"];
        }

        _formLoaded = true;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateMode))]
    public partial string HeaderText { get; set; } = LocalizationManager.Instance["Profile_CreateHeader"];

    public bool IsUpdateMode => ActivePlayer is not null;

    public string? FirstNameError => GetErrors(nameof(FirstName)).Cast<object>().FirstOrDefault()?.ToString();
    public string? LastNameError => GetErrors(nameof(Surname)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasFirstNameError => GetErrors(nameof(FirstName)).Cast<object>().Any();
    public bool HasLastNameError => GetErrors(nameof(Surname)).Cast<object>().Any();
    public string? CountryError => GetErrors(nameof(SelectedCountry)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasCountryError => GetErrors(nameof(SelectedCountry)).Cast<object>().Any();
    public string? CityError => GetErrors(nameof(SelectedCity)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasCityError => GetErrors(nameof(SelectedCity)).Cast<object>().Any();
    public string? EmailError => GetErrors(nameof(Email)).Cast<object>().FirstOrDefault()?.ToString();
    public bool HasEmailError => GetErrors(nameof(Email)).Cast<object>().Any();
}
