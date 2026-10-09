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

        Navigating += OnShellNavigating;
        _session.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(ApplyRole);
        api.SessionExpired += (_, _) => MainThread.BeginInvokeOnMainThread(() => GoToAsync(AuthService.LoginRoute).FireAndForgetSafeAsync());
        InviteLink.Received += (_, _) => MainThread.BeginInvokeOnMainThread(OpenPendingInvite);
        Loaded += (_, _) => OpenPendingInvite();
        ApplyRole();
    }

    private void OpenPendingInvite()
    {
        if (_session.Current is not null || InviteLink.Take() is not { } code)
        {
            return;
        }

        GoToAsync($"register?code={Uri.EscapeDataString(code)}").FireAndForgetSafeAsync();
    }

    private void ApplyRole()
    {
        var current = _session.Current;
        var ready = current is { MustChangePassword: false, NeedsOrganization: false };

        HomeContent.IsVisible = ready;
        HomeContent.Title = _session.IsPlayer ? "Games" : "Home";
        GameContent.IsVisible = ready;
        AccountContent.IsVisible = ready;
        RankingContent.IsVisible = ready;
        ManageContent.IsVisible = ready && _session.CanControlScoreboard;
        InviteContent.IsVisible = ready && _session.CanControlScoreboard;
        ScoreboardItem.IsVisible =ready && _session.CanControlScoreboard;
        ConnectionContent.IsVisible = ready && _session.CanControlScoreboard;
        SshTab.IsVisible = _session.CanUseSsh;
    }

    private static readonly string[] _controlRoutes =
        ["manage", "invite", "playeredit", "scoreboard", "admin-teams", "admin-player", "admin-stats", "addscoreboardplayer", "playerstatsdetail", "connect"];

    private static readonly string[] _sshRoutes = ["admin-ssh"];

    private static readonly string[] _debugRoutes = ["debugocr", "debugtable"];

    private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        var segments = e.Target.Location.OriginalString
            .Split(['/', '?'], StringSplitOptions.RemoveEmptyEntries);

        var blocked = segments.Any(s =>
            (!_session.CanControlScoreboard && _controlRoutes.Contains(s, StringComparer.OrdinalIgnoreCase)) ||
            (!_session.CanUseSsh && _sshRoutes.Contains(s, StringComparer.OrdinalIgnoreCase)) ||
            (_session.IsPlayer && _debugRoutes.Contains(s, StringComparer.OrdinalIgnoreCase)));

        if (blocked && e.CanCancel)
        {
            e.Cancel();
        }
    }

    public static async Task DisplayToastAsync(string message)
    {
        var toast = Toast.Make(message, textSize: 18);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await toast.Show(cts.Token);
    }
}
