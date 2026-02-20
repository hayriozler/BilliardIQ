using BilliardIQ.Mobile.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
namespace BilliardIQ.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        builder.UseMauiApp<App>()
            .UseMauiCommunityToolkit();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
