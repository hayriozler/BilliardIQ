using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Manage;

public partial class TeamCreatePageModel(ApiClient api, AvatarImageService avatars, AvatarPickerSession picker)
    : CreateFormPageModel(avatars, picker)
{
    private List<ManageClubDto> _clubs = [];

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial IReadOnlyList<string> ClubNames { get; set; } = [];

    [ObservableProperty]
    public partial int ClubIndex { get; set; }

    [RelayCommand]
    private async Task Appearing()
    {
        if (ConsumePickerReturn())
        {
            return;
        }

        ErrorMessage = "";
        try
        {
            _clubs = await api.GetAsync<List<ManageClubDto>>("api/mobile/manage/clubs");
            ClubNames = [L["Create_FirstClub"], .. _clubs.Select(c => c.Name)];
            ClubIndex = 0;
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ErrorMessage = ApiErrorText.For(ex, L);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = "";
        if (Name.Trim().Length == 0)
        {
            ErrorMessage = L["Create_TeamNameRequired"];
            return;
        }

        IsBusy = true;
        try
        {
            int? clubId = ClubIndex > 0 && ClubIndex - 1 < _clubs.Count ? _clubs[ClubIndex - 1].Id : null;
            await api.PostAsync("api/mobile/manage/teams", new CreateTeamRequest(Name, clubId, AvatarId));
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
}
