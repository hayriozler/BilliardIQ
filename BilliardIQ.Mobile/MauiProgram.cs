
using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.PageModels;
using BilliardIQ.Mobile.PageModels.Analyzers;
using BilliardIQ.Mobile.PageModels.GamePageModels;
using BilliardIQ.Mobile.PageModels.PlayerPageModels;
using BilliardIQ.Mobile.Pages;
using BilliardIQ.Mobile.Pages.Analyzers;
using BilliardIQ.Mobile.Pages.Games;
using BilliardIQ.Mobile.Pages.Players;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace BilliardIQ.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitCamera()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        #if DEBUG
		        builder.Logging.AddDebug();
		        builder.Services.AddLogging(configure => configure.AddDebug());
#endif
        builder.Services.AddSingleton<PlayerRepository>();
        builder.Services.AddSingleton<GameRepository>();
        builder.Services.AddSingleton<ModalErrorHandler>();
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<MainViewPage>();
        builder.Services.AddSingleton<PlayerProfilePageModel>();
        builder.Services.AddSingleton<PlayerProfileViewPage>();
        builder.Services.AddSingleton<GameListPageModel>();
        builder.Services.AddSingleton<GameListViewPage>();
        builder.Services.AddSingleton<NewGamePageModel>();
        builder.Services.AddSingleton<NewGameViewPage>();
        builder.Services.AddSingleton<IFileSystem>(FileSystem.Current);
        builder.Services.AddTransientWithShellRoute<NewGameViewPage, NewGamePageModel>("newgame");
        builder.Services.AddTransientWithShellRoute<PhotoAnalyzerViewPage, PhotoAnalyzerPageModel>("camera");
        DatabaseService.InitializeDatabaseAsync().GetAwaiter().GetResult();

        return builder.Build();
    }
}
