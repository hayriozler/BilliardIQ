using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels;

public partial class MainPageModel : BasePageModel
{
    [RelayCommand]
    private async Task TakePhoto()
    {
#if ANDROID
        ((Android.App.Activity)Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!)
            .RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;
#endif
        await Shell.Current.GoToAsync("camera");
    }
}

