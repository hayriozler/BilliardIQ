using BillardIQ.Mobile.PageModels.GamePageModels;

namespace BillardIQ.Mobile.Pages.Games;

public partial class NewGameViewPage : BasePage
{
    public NewGameViewPage(NewGamePageModel newGamePageModel) : base(newGamePageModel) => InitializeComponent();
    protected override void OnAppearing() => base.OnAppearing();
}