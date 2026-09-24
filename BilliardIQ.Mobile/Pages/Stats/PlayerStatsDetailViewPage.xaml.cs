using BilliardIQ.Mobile.PageModels.Stats;

namespace BilliardIQ.Mobile.Pages.Stats;

public partial class PlayerStatsDetailViewPage : BasePage
{
    private readonly PlayerStatsDetailPageModel _model;

    public PlayerStatsDetailViewPage(PlayerStatsDetailPageModel model) : base(model)
    {
        InitializeComponent();
        _model = model;
        _model.ChartUpdated += OnChartUpdated;
    }

    private void OnChartUpdated() => MainThread.BeginInvokeOnMainThread(() => Chart.Invalidate());
}
