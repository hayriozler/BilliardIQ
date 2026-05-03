using BilliardIQ.Mobile.Pages.Debug;
using CommunityToolkit.Maui.Alerts;

namespace BilliardIQ.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

#if DEBUG
        Items.Add(new ShellContent
        {
            Title           = "🐛 Debug OCR",
            ContentTemplate = new DataTemplate(typeof(DebugOcrViewPage)),
            Route           = "debugocr"
        });
#endif
    }

    public static async Task DisplayToastAsync(string message)
    {
        var toast = Toast.Make(message, textSize: 18);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await toast.Show(cts.Token);
    }
}
