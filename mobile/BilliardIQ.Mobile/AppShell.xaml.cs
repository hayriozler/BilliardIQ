using BilliardIQ.Mobile.Pages.Debug;
using BilliardIQ.Mobile.PageModels.Debug;
using BilliardIQ.Mobile.Services.Api;
using BilliardIQ.Mobile.Utilities;
using CommunityToolkit.Maui.Alerts;

namespace BilliardIQ.Mobile;

public partial class AppShell : Shell
{
    private readonly SessionStore _session;

    public AppShell(SessionStore session, ApiClient api)
    {
        _session = session;
        InitializeComponent();

#if DEBUG
        Items.Add(new ShellContent
        {
            Title           = "🐛 Debug OCR",
            ContentTemplate = new DataTemplate(typeof(DebugOcrViewPage)),
            Route           = "debugocr"
        });
        Items.Add(new ShellContent
        {
            Title           = "🎱 Table Analysis",
            ContentTemplate = new DataTemplate(typeof(DebugTableAnalysisViewPage)),
            Route           = "debugtable"
        });
#endif

        _session.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(ApplyRole);
        api.SessionExpired += (_, _) => MainThread.BeginInvokeOnMainThread(() => GoToAsync(AuthService.LoginRoute).FireAndForgetSafeAsync());
        ApplyRole();
    }

    private void ApplyRole()
    {
        var current = _session.Current;
        var ready = current is { MustChangePassword: false, NeedsOrganization: false };

        PlayerHomeContent.IsVisible = ready && _session.IsPlayer;
        HomeContent.IsVisible = ready;
        HomeContent.Title = _session.IsPlayer ? "Games" : "Home";
        GameContent.IsVisible = ready;
        AccountContent.IsVisible = ready;
        ManageContent.IsVisible = ready && _session.CanControlScoreboard;
        ScoreboardItem.IsVisible =ready && _session.CanControlScoreboard;
        ConnectionContent.IsVisible = ready && _session.CanControlScoreboard;
        SshTab.IsVisible = _session.CanUseSsh;
    }

    public static async Task DisplayToastAsync(string message)
    {
        var toast = Toast.Make(message, textSize: 18);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await toast.Show(cts.Token);
    }
}
