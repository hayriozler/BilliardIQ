using BilliardIQ.Mobile.PageModels.Auth;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Manage;

public partial class PlayerEditPageModel(ApiClient api, AvatarImageService avatars, AvatarPickerSession picker)
    : PlayerProfileFormModel(avatars, picker), IQueryAttributable
{
    private int _playerId;

    protected override bool CanEditName => true;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("playerId", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            _playerId = id;
        }
    }

    protected override Task<FullProfileDto> FetchProfileAsync() =>
        api.GetAsync<FullProfileDto>($"api/mobile/manage/players/{_playerId}/profile");

    protected override Task<FullProfileDto> StoreProfileAsync(ProfileUpdateRequest request) =>
        api.PutAsync<FullProfileDto>($"api/mobile/manage/players/{_playerId}/profile", request);

    [RelayCommand]
    private async Task Appearing()
    {
        if (ConsumePickerReturn())
        {
            return;
        }

        Message = "";
        await LoadProfileAsync();
    }

    [RelayCommand]
    private Task Save() => SaveFormAsync();

    [RelayCommand]
    private Task Back() => Shell.Current.GoToAsync("..");
}
