using BilliardIQ.Mobile.Pages.Analyzers;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels;

public partial class MainPageModel : BasePageModel
{
    private readonly IServiceProvider _services;

    public MainPageModel(IServiceProvider services)
    {
        _services = services;
    }

    [RelayCommand]
    private async Task TakePhoto()
    {
#if ANDROID
        ((Android.App.Activity)Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!)
            .RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
#endif
        var page = _services.GetRequiredService<PhotoAnalyzerViewPage>();
        await Shell.Current.Navigation.PushModalAsync(page, animated: false);
    }
}
