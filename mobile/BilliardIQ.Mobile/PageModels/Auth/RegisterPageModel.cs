using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class RegisterPageModel(AuthService auth) : BasePageModel
{
    [ObservableProperty]
    public partial string Code { get; set; } = "";

    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Password { get; set; } = "";

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool IsIdle => !IsBusy;

    [RelayCommand]
    private async Task Submit()
    {
        ErrorMessage = "";
        if (Code.Trim().Length == 0 || Email.Trim().Length == 0 || Password.Length == 0)
        {
            ErrorMessage = L["Auth_Required"];
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = L["Pwd_TooShort"];
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = L["Pwd_Mismatch"];
            return;
        }

        IsBusy = true;
        try
        {
            var session = await auth.RegisterWithInviteAsync(Code.Trim(), Email.Trim(), Password);
            Password = "";
            ConfirmPassword = "";
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
    private Task Back() => Shell.Current.GoToAsync("..");
}
