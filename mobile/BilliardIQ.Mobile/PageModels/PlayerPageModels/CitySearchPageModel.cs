using BilliardIQ.Mobile.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BilliardIQ.Mobile.PageModels.PlayerPageModels;

public partial class CitySearchPageModel(PlayerProfilePageModel ProfileModel) : BasePageModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    public partial ObservableCollection<CityItem> FilteredCities { get; set; } = [];

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    public bool HasResults => FilteredCities.Count > 0;

    [RelayCommand]
    private void Appearing()
    {
        SearchText = null;
        ApplyFilter(null);
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter(value);

    private void ApplyFilter(string? search)
    {
        FilteredCities.Clear();
        var source = ProfileModel.Cities;
        var filtered = string.IsNullOrWhiteSpace(search)
            ? source.AsEnumerable()
            : source.Where(c => c.Name.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));
        foreach (var city in filtered)
            FilteredCities.Add(city);
        OnPropertyChanged(nameof(HasResults));
    }

    [RelayCommand]
    private async Task SelectCity(CityItem city)
    {
        ProfileModel.SelectedCity = city;
        await Shell.Current.Navigation.PopModalAsync(animated: true);
    }

    [RelayCommand]
    private static async Task Cancel() => await Shell.Current.Navigation.PopModalAsync(animated: true);
}
