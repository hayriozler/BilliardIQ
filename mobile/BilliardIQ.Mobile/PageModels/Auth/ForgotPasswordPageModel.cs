using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class ForgotPasswordPageModel(AuthService auth) : BasePageModel
{
    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial string Code { get; set; } = "";

    [ObservableProperty]
    public partial string NewPassword { get; set; } = "";

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmailStep))]
    public partial bool CodeSent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordHidden), nameof(PasswordToggleGlyph))]
    public partial bool ShowPassword { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    public partial bool MessageIsError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    public bool IsEmailStep => !CodeSent;

    public bool IsPasswordHidden => !ShowPassword;

    public string PasswordToggleGlyph => ShowPassword ? "🙈" : "👁";

    public bool HasMessage => Message.Length > 0;

    public bool IsIdle => !IsBusy;

    [RelayCommand]
    private void Appearing()
    {
        Email = "";
        Code = "";
        NewPassword = "";
        ConfirmPassword = "";
        CodeSent = false;
        ShowPassword = false;
        Message = "";
    }

    [RelayCommand]
    private async Task SendCode()
    {
        Message = "";
        MessageIsError = true;
        if (Email.Trim().Length == 0)
        {
            Message = L["Auth_Required"];
            return;
        }

        IsBusy = true;
        try
        {
            await auth.RequestPasswordResetAsync(Email.Trim());
            CodeSent = true;
            MessageIsError = false;
            Message = L["Forgot_CodeSent"];
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
    private async Task ResetPassword()
    {
        Message = "";
        MessageIsError = true;
        if (Code.Trim().Length == 0 || NewPassword.Length == 0)
        {
            Message = L["Auth_Required"];
            return;
        }

        if (NewPassword.Length < 6)
        {
            Message = L["Pwd_TooShort"];
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            Message = L["Pwd_Mismatch"];
            return;
        }

        IsBusy = true;
        try
        {
            await auth.ResetPasswordAsync(Email.Trim(), Code.Trim(), NewPassword);
            await Shell.Current.GoToAsync(AuthService.LoginRoute);
            await AppShell.DisplayToastAsync(L["Forgot_Done"]);
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
    private void TogglePassword() => ShowPassword = !ShowPassword;

    [RelayCommand]
    private Task Back() => Shell.Current.GoToAsync("..");
}
