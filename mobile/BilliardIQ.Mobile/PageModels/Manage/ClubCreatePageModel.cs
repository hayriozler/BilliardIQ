using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Manage;

public partial class ClubCreatePageModel(ApiClient api) : BasePageModel
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string ShortName { get; set; } = "";

    [ObservableProperty]
    public partial string City { get; set; } = "";

    [ObservableProperty]
    public partial string PrimaryColor { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool IsIdle => !IsBusy;

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = "";
        if (Name.Trim().Length == 0)
        {
            ErrorMessage = L["Create_ClubNameRequired"];
            return;
        }

        IsBusy = true;
        try
        {
            await api.PostAsync("api/mobile/manage/clubs", new CreateClubRequest(Name, ShortName, City, PrimaryColor));
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
    private Task Back() => Shell.Current.GoToAsync("..");
}
