using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Manage;

public abstract partial class CreateFormPageModel(AvatarImageService avatars, AvatarPickerSession picker) : BasePageModel
{
    private bool _returningFromPicker;

    [ObservableProperty]
    public partial int? AvatarId { get; set; }

    [ObservableProperty]
    public partial ImageSource? Picture { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPicture))]
    public partial bool HasPicture { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    public bool IsIdle => !IsBusy;

    public bool HasNoPicture => !HasPicture;

    protected bool ConsumePickerReturn()
    {
        var returning = _returningFromPicker;
        _returningFromPicker = false;
        return returning;
    }

    protected virtual void OnAvatarChosen()
    {
    }

    protected virtual async Task RefreshPictureAsync()
    {
        Picture = await avatars.GetAsync(AvatarId, null);
        HasPicture = Picture is not null;
    }

    [RelayCommand]
    private Task PickAvatar()
    {
        _returningFromPicker = true;
        picker.Begin(id =>
        {
            AvatarId = id;
            OnAvatarChosen();
            _ = RefreshPictureAsync();
        });
        return Shell.Current.GoToAsync("avatarpicker");
    }

    [RelayCommand]
    private Task Back() => Shell.Current.GoToAsync("..");
}
