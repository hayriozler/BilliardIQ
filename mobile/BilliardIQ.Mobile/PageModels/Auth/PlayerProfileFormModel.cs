using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public abstract partial class PlayerProfileFormModel(AvatarImageService avatars, AvatarPickerSession picker, CatalogService catalogs) : BasePageModel
{
    private static readonly int[] _levelValues = [1, 2, 4, 8];

    private byte[]? _pendingPhoto;
    private string? _photoUrl;
    private bool _removePhoto;
    private bool _returningFromPicker;
    private CatalogDto? _catalog;
    private bool _applying;

    [ObservableProperty]
    public partial string FirstName { get; set; } = "";

    [ObservableProperty]
    public partial string LastName { get; set; } = "";

    [ObservableProperty]
    public partial string Nickname { get; set; } = "";

    [ObservableProperty]
    public partial int? AvatarId { get; set; }

    [ObservableProperty]
    public partial ImageSource? Picture { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPicture))]
    public partial bool HasPicture { get; set; }

    [ObservableProperty]
    public partial int LevelIndex { get; set; } = 1;

    [ObservableProperty]
    public partial string LicenseNo { get; set; } = "";

    [ObservableProperty]
    public partial string LicenseValidUntil { get; set; } = "";

    [ObservableProperty]
    public partial string BirthDate { get; set; } = "";

    [ObservableProperty]
    public partial int GenderIndex { get; set; }

    [ObservableProperty]
    public partial int HandednessIndex { get; set; }

    [ObservableProperty]
    public partial string Phone { get; set; } = "";

    [ObservableProperty]
    public partial int LanguageIndex { get; set; }

    [ObservableProperty]
    public partial string ShortcutNumber { get; set; } = "";

    [ObservableProperty]
    public partial IReadOnlyList<string> CountryNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial IReadOnlyList<string> RegionNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial IReadOnlyList<string> CityNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial IReadOnlyList<string> AssociationNames { get; set; } = ["-"];

    [ObservableProperty]
    public partial int CountryIndex { get; set; }

    [ObservableProperty]
    public partial int RegionIndex { get; set; }

    [ObservableProperty]
    public partial int CityIndex { get; set; }

    [ObservableProperty]
    public partial int AssociationIndex { get; set; }

    [ObservableProperty]
    public partial string Clubs { get; set; } = "";

    [ObservableProperty]
    public partial string Teams { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    public partial bool MessageIsError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool HasMessage => Message.Length > 0;

    public bool IsIdle => !IsBusy;

    public bool HasNoPicture => !HasPicture;

    public IReadOnlyList<string> LevelNames =>
        [L["Account_LevelBeginner"], L["Account_LevelIntermediate"], L["Account_LevelAdvanced"], L["Account_LevelProfessional"]];

    public IReadOnlyList<string> GenderNames =>
        ["-", L["Account_GenderMale"], L["Account_GenderFemale"], L["Account_GenderOther"], L["Account_GenderUndisclosed"]];

    public IReadOnlyList<string> HandednessNames =>
        ["-", L["Account_HandRight"], L["Account_HandLeft"]];

    public IReadOnlyList<string> LanguageNames => ["Türkçe", "English"];

    protected abstract Task<FullProfileDto> FetchProfileAsync();

    protected abstract Task<FullProfileDto> StoreProfileAsync(ProfileUpdateRequest request);

    protected abstract bool CanEditName { get; }

    protected async Task LoadProfileAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await FetchProfileAsync();
            _catalog = await catalogs.GetAsync();
            ApplyProfile(profile);
            _pendingPhoto = null;
            _removePhoto = false;
            await RefreshPictureAsync();
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ShowError(ApiErrorText.For(ex, L));
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<bool> SaveFormAsync()
    {
        Message = "";
        if (!TryParseDate(BirthDate, out var birth) || !TryParseDate(LicenseValidUntil, out var license))
        {
            ShowError(L["Account_DateInvalid"]);
            return false;
        }

        IsBusy = true;
        try
        {
            var request = new ProfileUpdateRequest(
                CanEditName ? FirstName.Trim() : null,
                CanEditName ? LastName.Trim() : null,
                Nickname.Trim(),
                AvatarId,
                _pendingPhoto is { Length: > 0 } ? Convert.ToBase64String(_pendingPhoto) : null,
                _removePhoto && _pendingPhoto is null,
                _levelValues[Math.Clamp(LevelIndex, 0, _levelValues.Length - 1)],
                NullIfEmpty(LicenseNo),
                license,
                birth,
                GenderIndex <= 0 ? null : GenderIndex - 1,
                HandednessIndex <= 0 ? null : HandednessIndex - 1,
                NullIfEmpty(Phone) ?? "",
                LanguageIndex == 1 ? "en-US" : "tr-TR",
                SelectedId(_catalog?.Countries, CountryIndex),
                SelectedId(CurrentRegions(), RegionIndex),
                SelectedId(CurrentCities(), CityIndex),
                SelectedId(_catalog?.Associations, AssociationIndex));
            ApplyProfile(await StoreProfileAsync(request));
            _pendingPhoto = null;
            _removePhoto = false;
            await RefreshPictureAsync();
            MessageIsError = false;
            Message = L["Account_Saved"];
            return true;
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ShowError(ApiErrorText.For(ex, L));
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task PickAvatar()
    {
        _returningFromPicker = true;
        picker.Begin(id =>
        {
            AvatarId = id;
            _pendingPhoto = null;
            _photoUrl = null;
            _removePhoto = false;
            _ = RefreshPictureAsync();
        });
        return Shell.Current.GoToAsync("avatarpicker");
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
            ShowError(L["Account_PhotoFailed"]);
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
            ShowError(L["Account_PhotoFailed"]);
        }
    }

    [RelayCommand]
    private async Task RemovePhoto()
    {
        _pendingPhoto = null;
        _photoUrl = null;
        _removePhoto = true;
        await RefreshPictureAsync();
    }

    protected bool ConsumePickerReturn()
    {
        var returning = _returningFromPicker;
        _returningFromPicker = false;
        return returning;
    }

    protected void ShowError(string text)
    {
        MessageIsError = true;
        Message = text;
    }

    private void ApplyProfile(FullProfileDto p)
    {
        FirstName = p.FirstName;
        LastName = p.LastName;
        Nickname = p.Nickname ?? "";
        AvatarId = p.AvatarId;
        _photoUrl = p.PhotoUrl;
        LevelIndex = Math.Max(0, Array.IndexOf(_levelValues, p.Level));
        LicenseNo = p.LicenseNo ?? "";
        LicenseValidUntil = p.LicenseValidUntil ?? "";
        BirthDate = p.BirthDate ?? "";
        GenderIndex = p.Gender is { } g ? g + 1 : 0;
        HandednessIndex = p.Handedness is { } h ? h + 1 : 0;
        Phone = p.Phone ?? "";
        LanguageIndex = p.Locale is { } l && l.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        ShortcutNumber = p.ShortcutNumber?.ToString() ?? "";
        Clubs = string.Join(", ", p.Clubs);
        Teams = string.Join(", ", p.Teams);
        ApplyCatalog(p);
        OnPropertyChanged(nameof(FullName));
    }

    partial void OnCountryIndexChanged(int value)
    {
        if (_applying)
        {
            return;
        }

        RebuildGeo();
        RegionIndex = 0;
        CityIndex = 0;
    }

    private void ApplyCatalog(FullProfileDto p)
    {
        _applying = true;
        try
        {
            CountryNames = ["-", .. (_catalog?.Countries ?? []).Select(c => c.Name)];
            AssociationNames = ["-", .. (_catalog?.Associations ?? []).Select(a => a.Name)];
            CountryIndex = IndexOf(_catalog?.Countries, p.CountryId);
            RebuildGeo();
            RegionIndex = IndexOf(CurrentRegions(), p.RegionId);
            CityIndex = IndexOf(CurrentCities(), p.CityId);
            AssociationIndex = IndexOf(_catalog?.Associations, p.AssociationId);
        }
        finally
        {
            _applying = false;
        }
    }

    private void RebuildGeo()
    {
        RegionNames = ["-", .. CurrentRegions().Select(r => r.Name)];
        CityNames = ["-", .. CurrentCities().Select(c => c.Name)];
    }

    private List<CatalogItemDto> CurrentRegions() =>
        _catalog is not null && SelectedId(_catalog.Countries, CountryIndex) is { } countryId
            ? [.. _catalog.Regions.Where(r => r.CountryId == countryId)]
            : [];

    private List<CatalogItemDto> CurrentCities() =>
        _catalog is not null && SelectedId(_catalog.Countries, CountryIndex) is { } countryId
            ? [.. _catalog.Cities.Where(c => c.CountryId == countryId)]
            : [];

    private static int? SelectedId(List<CatalogItemDto>? items, int index) =>
        items is not null && index > 0 && index - 1 < items.Count ? items[index - 1].Id : null;

    private static int IndexOf(List<CatalogItemDto>? items, int? id)
    {
        if (items is null || id is null)
        {
            return 0;
        }

        var position = items.FindIndex(i => i.Id == id);
        return position < 0 ? 0 : position + 1;
    }

    private async Task RefreshPictureAsync()
    {
        if (_pendingPhoto is { Length: > 0 } bytes)
        {
            Picture = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        else
        {
            Picture = await avatars.GetAsync(AvatarId, _removePhoto ? null : _photoUrl);
        }

        HasPicture = Picture is not null;
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
        _removePhoto = false;
        await RefreshPictureAsync();
    }

    private static bool TryParseDate(string text, out string? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!DateOnly.TryParse(text.Trim(), out var date))
        {
            return false;
        }

        value = date.ToString("yyyy-MM-dd");
        return true;
    }

    private static string? NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
