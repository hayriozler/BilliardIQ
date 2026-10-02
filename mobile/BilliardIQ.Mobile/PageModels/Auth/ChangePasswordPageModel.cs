using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class ChangePasswordPageModel(AuthService auth, SessionStore session) : BasePageModel
{
    [ObservableProperty]
    public partial string CurrentPassword { get; set; } = "";

    [ObservableProperty]
    public partial string NewPassword { get; set; } = "";

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    public partial bool IsForced { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool IsIdle => !IsBusy;

    public bool CanCancel => !IsForced;

    [RelayCommand]
    private void Appearing()
    {
        IsForced = session.Current?.MustChangePassword ?? false;
        CurrentPassword = "";
        NewPassword = "";
        ConfirmPassword = "";
        ErrorMessage = "";
    }

    [RelayCommand]
    private async Task Submit()
    {
        ErrorMessage = "";
        if (CurrentPassword.Length == 0 || NewPassword.Length == 0)
        {
            ErrorMessage = L["Auth_Required"];
            return;
        }

        if (NewPassword.Length < 6)
        {
            ErrorMessage = L["Pwd_TooShort"];
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = L["Pwd_Mismatch"];
            return;
        }

        IsBusy = true;
        try
        {
            var updated = await auth.ChangePasswordAsync(CurrentPassword, NewPassword);
            CurrentPassword = "";
            NewPassword = "";
            ConfirmPassword = "";
            await Shell.Current.GoToAsync(AuthService.RouteFor(updated));
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
    private Task Cancel() => Shell.Current.GoToAsync("//account");
}
