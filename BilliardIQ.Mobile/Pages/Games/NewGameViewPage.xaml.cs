using BilliardIQ.Mobile.PageModels.GamePageModels;

namespace BilliardIQ.Mobile.Pages.Games;

public partial class NewGameViewPage : BasePage
{
    public NewGameViewPage(NewGamePageModel newGamePageModel) : base(newGamePageModel) => InitializeComponent();
    protected override void OnAppearing()
	{
		base.OnAppearing();                 
    }
}