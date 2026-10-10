using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Manage;

public partial class PlayerCreatePageModel(ApiClient api, AvatarImageService avatars, AvatarPickerSession picker, CatalogService catalogs)
    : CreateFormPageModel(avatars, picker)
{
    private static readonly int[] _levelValues = [1, 2, 4, 8];

    private byte[]? _pendingPhoto;
    private CatalogDto? _catalog;

    [ObservableProperty]
    public partial string Nickname { get; set; } = "";

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial int LevelIndex { get; set; } = 1;

    [ObservableProperty]
    public partial string ShortcutNumber { get; set; } = "";

    [ObservableProperty]
    public partial string LicenseNo { get; set; } = "";

    [ObservableProperty]
    public partial string LicenseValidUntil { get; set; } = "";

    [ObservableProperty]
    public partial IReadOnlyList<string> AssociationNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial IReadOnlyList<string> RegionNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial IReadOnlyList<string> CountryNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial IReadOnlyList<string> CityNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial int AssociationIndex { get; set; }

    [ObservableProperty]
    public partial int RegionIndex { get; set; }

    [ObservableProperty]
    public partial int CountryIndex { get; set; }

    [ObservableProperty]
    public partial int CityIndex { get; set; }

    public IReadOnlyList<string> LevelNames =>
        [L["Account_LevelBeginner"], L["Account_LevelIntermediate"], L["Account_LevelAdvanced"], L["Account_LevelProfessional"]];

    [RelayCommand]
    private async Task Appearing()
    {
        if (ConsumePickerReturn())
        {
            return;
        }

        ErrorMessage = "";
        try
        {
            _catalog = await catalogs.GetAsync();
            AssociationNames = ["-", .. _catalog.Associations.Select(a => a.Name)];
            RegionNames = ["-", .. _catalog.Regions.Select(r => r.Name)];
            CountryNames = ["-", .. _catalog.Countries.Select(c => c.Name)];
            RebuildCities();
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ErrorMessage = ApiErrorText.For(ex, L);
        }
    }

    partial void OnCountryIndexChanged(int value)
    {
        RebuildCities();
        CityIndex = 0;
    }

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = "";
        if (Name.Trim().Length == 0)
        {
            ErrorMessage = L["Create_PlayerNameRequired"];
            return;
        }

        int? shortcut = null;
        if (ShortcutNumber.Trim().Length > 0)
        {
            if (!int.TryParse(ShortcutNumber.Trim(), out var number) || number is < 1 or > 9999)
            {
                ErrorMessage = L["Create_ShortcutRange"];
                return;
            }

            shortcut = number;
        }

        string? license = null;
        if (LicenseValidUntil.Trim().Length > 0)
        {
            if (!DateOnly.TryParse(LicenseValidUntil.Trim(), out var date))
            {
                ErrorMessage = L["Account_DateInvalid"];
                return;
            }

            license = date.ToString("yyyy-MM-dd");
        }

        IsBusy = true;
        try
        {
            var request = new CreatePlayerRequest(
                Nickname,
                Name,
                AvatarId,
                _pendingPhoto is { Length: > 0 } ? Convert.ToBase64String(_pendingPhoto) : null,
                Email,
                _levelValues[Math.Clamp(LevelIndex, 0, _levelValues.Length - 1)],
                shortcut,
                LicenseNo,
                license,
                SelectedId(_catalog?.Associations, AssociationIndex),
                SelectedId(_catalog?.Regions, RegionIndex),
                SelectedId(_catalog?.Countries, CountryIndex),
                SelectedId(CurrentCities(), CityIndex));
            await api.PostAsync("api/mobile/manage/players", request);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ErrorMessage = ApiErrorText.For(ex, L);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TakePhoto()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                return;
            }

            await SetPhotoAsync(await MediaPicker.Default.CapturePhotoAsync());
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or PermissionException or InvalidOperationException)
        {
            ErrorMessage = L["Account_PhotoFailed"];
        }
    }

    [RelayCommand]
    private async Task ChoosePhoto()
    {
        try
        {
            await SetPhotoAsync(await MediaPicker.Default.PickPhotoAsync());
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or PermissionException or InvalidOperationException)
        {
            ErrorMessage = L["Account_PhotoFailed"];
        }
    }

    [RelayCommand]
    private async Task RemovePhoto()
    {
        _pendingPhoto = null;
        await RefreshPictureAsync();
    }

    protected override void OnAvatarChosen() => _pendingPhoto = null;

    protected override async Task RefreshPictureAsync()
    {
        if (_pendingPhoto is { Length: > 0 } bytes)
        {
            Picture = ImageSource.FromStream(() => new MemoryStream(bytes));
            HasPicture = true;
            return;
        }

        await base.RefreshPictureAsync();
    }

    private async Task SetPhotoAsync(FileResult? file)
    {
        if (file is null)
        {
            return;
        }

        using var stream = await file.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var original = memory.ToArray();
        var resized = await Task.Run(() => ImagePreprocessor.CreateThumbnail(original, maxWidth: 800, maxHeight: 800));
        _pendingPhoto = resized.Length > 0 ? resized : original;
        await RefreshPictureAsync();
    }

    private void RebuildCities() => CityNames = ["-", .. CurrentCities().Select(c => c.Name)];

    private List<CatalogItemDto> CurrentCities() =>
        _catalog is not null && SelectedId(_catalog.Countries, CountryIndex) is { } countryId
            ? [.. _catalog.Cities.Where(c => c.CountryId == countryId)]
            : [];

    private static int? SelectedId(List<CatalogItemDto>? items, int index) =>
        items is not null && index > 0 && index - 1 < items.Count ? items[index - 1].Id : null;
}
