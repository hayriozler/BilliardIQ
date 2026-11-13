using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class LoginPageModel(AuthService auth) : BasePageModel
{
    private bool _restoreAttempted;

    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Password { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordHidden))]
    [NotifyPropertyChangedFor(nameof(PasswordToggleGlyph))]
    public partial bool ShowPassword { get; set; }

    public bool IsPasswordHidden => !ShowPassword;

    public string PasswordToggleGlyph => ShowPassword ? "🙈" : "👁";

    public bool HasError =>ErrorMessage.Length > 0;

    public bool IsIdle => !IsBusy;

    [RelayCommand]
    private async Task Appearing()
    {
        if (_restoreAttempted)
        {
            return;
        }

        _restoreAttempted = true;
        IsBusy = true;
        try
        {
            if (await auth.RestoreAsync() is { } restored)
            {
                await Shell.Current.GoToAsync(AuthService.RouteFor(restored));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignIn()
    {
        ErrorMessage = "";
        if (Email.Trim().Length == 0 || Password.Length == 0)
        {
            ErrorMessage = L["Auth_Required"];
            return;
        }

        IsBusy = true;
        try
        {
            var session = await auth.LoginAsync(Email.Trim(), Password);
            Password = "";
            await Shell.Current.GoToAsync(AuthService.RouteFor(session));
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
    private void TogglePassword() => ShowPassword = !ShowPassword;

    [RelayCommand]
    private Task ForgotPassword() => Shell.Current.GoToAsync("forgotpassword");

    [RelayCommand]
    private Task Register() => Shell.Current.GoToAsync("register");
}
