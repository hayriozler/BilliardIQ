using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class AccountPageModel(AuthService auth, SessionStore session) : BasePageModel
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
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    public partial bool MessageIsError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    public bool HasMessage => Message.Length > 0;

    public bool IsIdle => !IsBusy;

    [RelayCommand]
    private void Appearing()
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
        NewEmail = "";
        EmailPassword = "";
        Message = "";
    }

    [RelayCommand]
    private Task ChangeOrganization() => Shell.Current.GoToAsync(AuthService.OrganizationRoute);

    [RelayCommand]
    private Task ChangePassword() => Shell.Current.GoToAsync(AuthService.ChangePasswordRoute);

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
            Appearing();
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
}
