using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Auth;

public partial class OrganizationPageModel(AuthService auth, SessionStore session) : BasePageModel
{
    [ObservableProperty]
    public partial IReadOnlyList<ApiOrganization> Organizations { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    [RelayCommand]
    private void Appearing()
    {
        ErrorMessage = "";
        Organizations = session.Current?.Organizations ?? [];
    }

    [RelayCommand]
    private async Task Select(ApiOrganization? organization)
    {
        if (organization is null || IsBusy)
        {
            return;
        }

        ErrorMessage = "";
        IsBusy = true;
        try
        {
            var updated = await auth.SelectOrganizationAsync(organization.Id);
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
}
