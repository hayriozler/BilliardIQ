using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class AccountPageModel(AuthService auth, SessionStore session, ApiClient api, AvatarImageService avatars) : BasePageModel
{
    private static readonly int[] _levelValues = [1, 2, 4, 8];

    private byte[]? _pendingPhoto;
    private string? _photoUrl;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "";

    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Role { get; set; } = "";

    [ObservableProperty]
    public partial string OrganizationName { get; set; } = "";

    [ObservableProperty]
    public partial bool CanSwitchOrganization { get; set; }

    [ObservableProperty]
    public partial string NewEmail { get; set; } = "";

    [ObservableProperty]
    public partial string EmailPassword { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    public partial bool MessageIsError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsPlayer { get; set; }

    [ObservableProperty]
    public partial string FullName { get; set; } = "";

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
    public partial string Association { get; set; } = "";

    [ObservableProperty]
    public partial string Location { get; set; } = "";

    [ObservableProperty]
    public partial string Clubs { get; set; } = "";

    [ObservableProperty]
    public partial string Teams { get; set; } = "";

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

    public int? CurrentAvatarId => AvatarId;

    public void SelectAvatar(int avatarId)
    {
        AvatarId = avatarId;
        _pendingPhoto = null;
        _photoUrl = null;
        _ = RefreshPictureAsync();
    }

    [RelayCommand]
    private async Task Appearing()
    {
        var current = session.Current;
        if (current is null)
        {
            return;
        }

        DisplayName = current.User.DisplayName;
        Email = current.User.Email ?? "";
        Role = current.Role;
        OrganizationName = current.Organization?.Name ?? "";
        CanSwitchOrganization = current.Organizations.Count > 1;
        IsPlayer = current.Role == ApiRoles.Player;
        NewEmail = "";
        EmailPassword = "";
        Message = "";
        Phone = current.User.Phone ?? "";
        LanguageIndex = LocaleToIndex(current.User.Locale);

        if (IsPlayer)
        {
            await LoadPlayerAsync();
        }
    }

    [RelayCommand]
    private Task ChangeOrganization() => Shell.Current.GoToAsync(AuthService.OrganizationRoute);

    [RelayCommand]
    private Task ChangePassword() => Shell.Current.GoToAsync(AuthService.ChangePasswordRoute);

    [RelayCommand]
    private Task PickAvatar() => Shell.Current.GoToAsync("avatarpicker");

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

    private bool _removePhoto;

    [RelayCommand]
    private async Task SaveProfile()
    {
        Message = "";
        if (!TryParseDate(BirthDate, out var birth) || !TryParseDate(LicenseValidUntil, out var license))
        {
            ShowError(L["Account_DateInvalid"]);
            return;
        }

        IsBusy = true;
        try
        {
            if (IsPlayer)
            {
                var request = new OwnProfileRequest(
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
                    IndexToLocale(LanguageIndex));
                var saved = await api.PutAsync<FullProfileDto>("api/mobile/player/profile", request);
                ApplyProfile(saved);
                _pendingPhoto = null;
                _removePhoto = false;
                await RefreshPictureAsync();
            }
            else
            {
                if (DisplayName.Trim().Length == 0)
                {
                    ShowError(L["Auth_Required"]);
                    return;
                }

                await api.PutAsync("api/mobile/me/profile", new UpdateProfileRequest(DisplayName.Trim(), IndexToLocale(LanguageIndex), NullIfEmpty(Phone) ?? ""));
            }

            await auth.RefreshSessionAsync();
            LocalizationManager.Instance.SetLanguage(LanguageIndex == 0 ? "tr" : "en");
            MessageIsError = false;
            Message = L["Account_Saved"];
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

    [RelayCommand]
    private async Task ChangeEmail()
    {
        Message = "";
        MessageIsError = true;
        if (NewEmail.Trim().Length == 0 || EmailPassword.Length == 0)
        {
            Message = L["Auth_Required"];
            return;
        }

        IsBusy = true;
        try
        {
            await auth.ChangeEmailAsync(NewEmail.Trim(), EmailPassword);
            await Appearing();
            MessageIsError = false;
            Message = L["Account_EmailChanged"];
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            Message = ApiErrorText.For(ex, L);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignOut()
    {
        IsBusy = true;
        try
        {
            await auth.SignOutAsync();
            await Shell.Current.GoToAsync(AuthService.LoginRoute);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPlayerAsync()
    {
        IsBusy = true;
        try
        {
            ApplyProfile(await api.GetAsync<FullProfileDto>("api/mobile/player/profile"));
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

    private void ApplyProfile(FullProfileDto p)
    {
        FullName = $"{p.FirstName} {p.LastName}".Trim();
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
        LanguageIndex = LocaleToIndex(p.Locale);
        ShortcutNumber = p.ShortcutNumber?.ToString() ?? "";
        Association = p.AssociationName ?? "";
        Location = string.Join(", ", new[] { p.CountryName, p.RegionName, p.CityName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        Clubs = string.Join(", ", p.Clubs);
        Teams = string.Join(", ", p.Teams);
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

    private void ShowError(string text)
    {
        MessageIsError = true;
        Message = text;
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

    private static int LocaleToIndex(string? locale) => locale is { } l && l.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

    private static string IndexToLocale(int index) => index == 1 ? "en-US" : "tr-TR";
}
