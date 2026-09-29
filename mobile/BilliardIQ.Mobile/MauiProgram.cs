
using BillardIQ.Mobile.Data;
using BillardIQ.Mobile.PageModels.Admin;
using BillardIQ.Mobile.PageModels.Analyzers;
using BillardIQ.Mobile.PageModels.ConnectionPageModels;
using BillardIQ.Mobile.PageModels.GamePageModels;
using BillardIQ.Mobile.PageModels.PlayerPageModels;
using BillardIQ.Mobile.PageModels.PlayPageModels;
using BillardIQ.Mobile.PageModels.ScoreboardPageModels;
using BillardIQ.Mobile.PageModels.Ssh;
using BillardIQ.Mobile.PageModels.Stats;
using BillardIQ.Mobile.Pages.Admin;
using BillardIQ.Mobile.Pages.Analyzers;
using BillardIQ.Mobile.Pages.Connection;
using BillardIQ.Mobile.Pages.Games;
using BillardIQ.Mobile.Pages.Play;
using BillardIQ.Mobile.Pages.Players;
using BillardIQ.Mobile.Pages.Scoreboard;
using BillardIQ.Mobile.Pages.Ssh;
using BillardIQ.Mobile.Pages.Stats;
using BillardIQ.Mobile.Services;
using Plugin.Maui.OCR;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

#if DEBUG
using BillardIQ.Mobile.PageModels.Debug;
using BillardIQ.Mobile.Pages.Debug;
#endif

#if ANDROID
using BillardIQ.Mobile.Platforms.Android;
#endif
#if IOS
using BillardIQ.Mobile.Platforms.iOS;
#endif

namespace BillardIQ.Mobile;

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
        builder.Services.AddTransient<DebugTableAnalysisPageModel>();
        builder.Services.AddTransient<DebugTableAnalysisViewPage>();
#endif
        builder.Services.AddSingleton<DatabaseExecutor>();
        builder.Services.AddSingleton<PlayerRepository>();
        builder.Services.AddSingleton<ScoreboardPlayerSession>();
        builder.Services.AddSingleton<GameRepository>();
        builder.Services.AddSingleton<LocationRepository>();
        builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();
        builder.Services.AddSingleton<IAlertHandler, ShowAlertHandler>();
        builder.Services.AddSingleton<ScoreboardOcrService>();
        builder.Services.AddSingleton<BallDetectionService>();
        builder.Services.AddSingleton<OpenCvBallDetector>();
        builder.Services.AddSingleton<TableVisionService>();
        builder.Services.AddSingleton<PlayerProfilePageModel>();
        builder.Services.AddSingleton<PlayerProfileViewPage>();
        builder.Services.AddTransient<CitySearchPageModel>();
        builder.Services.AddTransient<CitySearchPage>();
        builder.Services.AddSingleton<GameListPageModel>();
        builder.Services.AddSingleton<GameListViewPage>();
        builder.Services.AddSingleton<IUnityBridgeService, UnityBridgeService>();
        builder.Services.AddSingleton<GamePlayPageModel>();
        builder.Services.AddSingleton<GamePlayViewPage>();
        builder.Services.AddSingleton<IPiTransportFactory, PiTransportFactory>();
        builder.Services.AddSingleton<IRaspberryPiConnectionService, RaspberryPiConnectionService>();
        builder.Services.AddSingleton<ConnectionPageModel>();
        builder.Services.AddSingleton<ConnectionViewPage>();
        builder.Services.AddSingleton<ScoreboardPageModel>();
        builder.Services.AddSingleton<ScoreboardViewPage>();
        builder.Services.AddSingleton<TeamRepository>();
        builder.Services.AddSingleton<ScoreboardPlayerRepository>();
        builder.Services.AddSingleton<MatchResultRepository>();
        builder.Services.AddSingleton<MatchScoreStatRepository>();
        builder.Services.AddSingleton<TeamSession>();
        builder.Services.AddSingleton<TeamListPageModel>();
        builder.Services.AddSingleton<TeamListViewPage>();
        builder.Services.AddSingleton<PlayerListPageModel>();
        builder.Services.AddSingleton<PlayerListViewPage>();
        builder.Services.AddSingleton<PlayerStatsListPageModel>();
        builder.Services.AddSingleton<PlayerStatsListViewPage>();
        builder.Services.AddSingleton<SshConnectionRepository>();
        builder.Services.AddSingleton<ISshClientService, SshClientService>();
        builder.Services.AddSingleton<SshConsolePageModel>();
        builder.Services.AddSingleton<SshConsoleViewPage>();
        builder.Services.AddSingleton(FileSystem.Current);
        builder.Services.AddTransientWithShellRoute<NewGameViewPage, NewGamePageModel>("newgame");
        builder.Services.AddTransientWithShellRoute<AddScoreboardPlayerViewPage, AddScoreboardPlayerPageModel>("addscoreboardplayer");
        builder.Services.AddTransientWithShellRoute<PlayerStatsDetailViewPage, PlayerStatsDetailPageModel>("playerstatsdetail");
        builder.Services.AddTransient<PhotoAnalyzerPageModel>();
        builder.Services.AddTransient<PhotoAnalyzerViewPage>();
        new DatabaseMigrationService(builder.Services.BuildServiceProvider().GetRequiredService<DatabaseExecutor>()).RunMigrationAsync().GetAwaiter().GetResult();

        var app = builder.Build();

        var teamSession = app.Services.GetRequiredService<TeamSession>();
        teamSession.LoadExisting(app.Services.GetRequiredService<TeamRepository>().GetAllAsync().GetAwaiter().GetResult());

        var playerSession = app.Services.GetRequiredService<ScoreboardPlayerSession>();
        playerSession.LoadExisting(app.Services.GetRequiredService<ScoreboardPlayerRepository>().GetAllAsync().GetAwaiter().GetResult());

        return app;
    }
}
