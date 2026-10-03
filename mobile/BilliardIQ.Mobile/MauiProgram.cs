
using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.PageModels.Auth;
using BilliardIQ.Mobile.Pages.Auth;
using BilliardIQ.Mobile.Services.Api;
using BilliardIQ.Mobile.PageModels.Admin;
using BilliardIQ.Mobile.PageModels.Manage;
using BilliardIQ.Mobile.PageModels.Ranking;
using BilliardIQ.Mobile.Pages.Ranking;
using BilliardIQ.Mobile.Pages.Manage;
using BilliardIQ.Mobile.PageModels.Analyzers;
using BilliardIQ.Mobile.PageModels.ConnectionPageModels;
using BilliardIQ.Mobile.PageModels.GamePageModels;
using BilliardIQ.Mobile.PageModels.PlayerPageModels;
using BilliardIQ.Mobile.PageModels.PlayPageModels;
using BilliardIQ.Mobile.PageModels.ScoreboardPageModels;
using BilliardIQ.Mobile.PageModels.Ssh;
using BilliardIQ.Mobile.PageModels.Stats;
using BilliardIQ.Mobile.Pages.Admin;
using BilliardIQ.Mobile.Pages.Analyzers;
using BilliardIQ.Mobile.Pages.Connection;
using BilliardIQ.Mobile.Pages.Games;
using BilliardIQ.Mobile.Pages.Play;
using BilliardIQ.Mobile.Pages.Players;
using BilliardIQ.Mobile.Pages.Scoreboard;
using BilliardIQ.Mobile.Pages.Ssh;
using BilliardIQ.Mobile.Pages.Stats;
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
        builder.Services.AddTransient<DebugTableAnalysisPageModel>();
        builder.Services.AddTransient<DebugTableAnalysisViewPage>();
#endif
        builder.Services.AddSingleton<DatabaseExecutor>();
        builder.Services.AddSingleton<ScoreboardPlayerSession>();
        builder.Services.AddSingleton<GameRepository>();
        builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();
        builder.Services.AddSingleton<ScoreboardOcrService>();
        builder.Services.AddSingleton<BallDetectionService>();
        builder.Services.AddSingleton<OpenCvBallDetector>();
        builder.Services.AddSingleton<TableVisionService>();
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
        builder.Services.AddSingleton<SessionStore>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<LoginPageModel>();
        builder.Services.AddSingleton<LoginViewPage>();
        builder.Services.AddSingleton<OrganizationPageModel>();
        builder.Services.AddSingleton<OrganizationViewPage>();
        builder.Services.AddSingleton<ChangePasswordPageModel>();
        builder.Services.AddSingleton<ChangePasswordViewPage>();
        builder.Services.AddSingleton<AccountPageModel>();
        builder.Services.AddSingleton<AccountViewPage>();
        builder.Services.AddSingleton<AvatarImageService>();
        builder.Services.AddSingleton<AvatarPickerSession>();
        builder.Services.AddSingleton<PlayerHomePageModel>();
        builder.Services.AddSingleton<PlayerHomeViewPage>();
        builder.Services.AddSingleton<RankingPageModel>();
        builder.Services.AddSingleton<RankingViewPage>();
        builder.Services.AddSingleton<ManageListPageModel>();
        builder.Services.AddSingleton<ManageListViewPage>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransientWithShellRoute<RegisterViewPage, RegisterPageModel>("register");
        builder.Services.AddTransientWithShellRoute<ForgotPasswordViewPage, ForgotPasswordPageModel>("forgotpassword");
        builder.Services.AddTransientWithShellRoute<AvatarPickerViewPage, AvatarPickerPageModel>("avatarpicker");
        builder.Services.AddTransientWithShellRoute<PlayerEditViewPage, PlayerEditPageModel>("playeredit");
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

        return app;
    }
}
