
using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.PageModels.Analyzers;
using BilliardIQ.Mobile.PageModels.GamePageModels;
using BilliardIQ.Mobile.PageModels.PlayerPageModels;
using BilliardIQ.Mobile.PageModels.PlayPageModels;
using BilliardIQ.Mobile.Pages.Analyzers;
using BilliardIQ.Mobile.Pages.Games;
using BilliardIQ.Mobile.Pages.Play;
using BilliardIQ.Mobile.Pages.Players;
using BilliardIQ.Mobile.Services;
using Plugin.Maui.OCR;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

#if DEBUG
using BilliardIQ.Mobile.PageModels.Debug;
using BilliardIQ.Mobile.Pages.Debug;
#endif

#if ANDROID
using BilliardIQ.Mobile.Platforms.Android;
#endif
#if IOS
using BilliardIQ.Mobile.Platforms.iOS;
#endif



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
            .UseOcr()
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
            builder.Services.AddTransient<DebugOcrPageModel>();
            builder.Services.AddTransient<DebugOcrViewPage>();
#endif
        builder.Services.AddSingleton<DatabaseExecutor>();
        builder.Services.AddSingleton<PlayerRepository>();
        builder.Services.AddSingleton<GameRepository>();
        builder.Services.AddSingleton<LocationRepository>();
        builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();
        builder.Services.AddSingleton<IAlertHandler, ShowAlertHandler>();
        builder.Services.AddSingleton<ScoreboardOcrService>();
        builder.Services.AddSingleton<BallDetectionService>();
        builder.Services.AddSingleton<PlayerProfilePageModel>();
        builder.Services.AddSingleton<PlayerProfileViewPage>();
        builder.Services.AddTransient<CitySearchPageModel>();
        builder.Services.AddTransient<CitySearchPage>();
        builder.Services.AddSingleton<GameListPageModel>();
        builder.Services.AddSingleton<GameListViewPage>();
        builder.Services.AddSingleton<IUnityBridgeService, UnityBridgeService>();
        builder.Services.AddSingleton<GamePlayPageModel>();
        builder.Services.AddSingleton<GamePlayViewPage>();
        builder.Services.AddSingleton(FileSystem.Current);
        builder.Services.AddTransientWithShellRoute<NewGameViewPage, NewGamePageModel>("newgame");
        builder.Services.AddTransient<PhotoAnalyzerPageModel>();
        builder.Services.AddTransient<PhotoAnalyzerViewPage>();
        new DatabaseMigrationService(builder.Services.BuildServiceProvider().GetRequiredService<DatabaseExecutor>()).RunMigrationAsync().GetAwaiter().GetResult();
        return builder.Build();
    }
}
