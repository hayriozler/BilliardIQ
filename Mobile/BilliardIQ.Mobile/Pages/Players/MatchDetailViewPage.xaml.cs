using BilliardIQ.Mobile.PageModels.PlayerPageModels;

namespace BilliardIQ.Mobile.Pages.Players;

public partial class MatchDetailViewPage : BasePage
{
    public MatchDetailViewPage(MatchDetailPageModel model) : base(model)
    {
        InitializeComponent();
        model.ChartUpdated += () => MainThread.BeginInvokeOnMainThread(() =>
        {
            Player1InningsChart.Invalidate();
            Player2InningsChart.Invalidate();
            Player1PaceChart.Invalidate();
            Player2PaceChart.Invalidate();
        });
    }
}
