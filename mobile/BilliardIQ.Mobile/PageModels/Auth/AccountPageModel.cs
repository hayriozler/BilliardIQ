using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class AccountPageModel(AuthService auth, SessionStore session, ApiClient api, AvatarImageService avatars, AvatarPickerSession picker, CatalogService catalogs)
    : PlayerProfileFormModel(avatars, picker, catalogs)
{
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
    [NotifyPropertyChangedFor(nameof(IsNotPlayer))]
    public partial bool IsPlayer { get; set; }

    public bool IsNotPlayer => !IsPlayer;

    protected override bool CanEditName => false;

    protected override Task<FullProfileDto> FetchProfileAsync() =>
        api.GetAsync<FullProfileDto>("api/mobile/player/profile");

    protected override Task<FullProfileDto> StoreProfileAsync(ProfileUpdateRequest request) =>
        api.PutAsync<FullProfileDto>("api/mobile/player/profile", request);

    [RelayCommand]
    private async Task Appearing()
    {
        if (ConsumePickerReturn())
        {
            return;
        }

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
        LanguageIndex = current.User.Locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        if (IsPlayer)
        {
            await LoadProfileAsync();
        }
    }

    [RelayCommand]
    private Task ChangeOrganization() => Shell.Current.GoToAsync(AuthService.OrganizationRoute);

    [RelayCommand]
    private Task ChangePassword() => Shell.Current.GoToAsync(AuthService.ChangePasswordRoute);

    [RelayCommand]
    private async Task SaveProfile()
    {
        var saved = IsPlayer ? await SaveFormAsync() : await SaveOwnAccountAsync();
        if (!saved)
        {
            return;
        }

        try
        {
            await auth.RefreshSessionAsync();
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
        }

        LocalizationManager.Instance.SetLanguage(LanguageIndex == 0 ? "tr" : "en");
        DisplayName = session.Current?.User.DisplayName ?? DisplayName;
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

    private async Task<bool> SaveOwnAccountAsync()
    {
        Message = "";
        if (DisplayName.Trim().Length == 0)
        {
            ShowError(L["Auth_Required"]);
            return false;
        }

        IsBusy = true;
        try
        {
            var phone = string.IsNullOrWhiteSpace(Phone) ? "" : Phone.Trim();
            await api.PutAsync("api/mobile/me/profile", new UpdateProfileRequest(DisplayName.Trim(), LanguageIndex == 1 ? "en-US" : "tr-TR", phone));
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
}
