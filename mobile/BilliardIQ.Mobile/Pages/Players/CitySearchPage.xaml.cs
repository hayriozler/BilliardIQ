using BilliardIQ.Mobile.PageModels.PlayerPageModels;

namespace BilliardIQ.Mobile.Pages.Players;

public partial class CitySearchPage : ContentPage
{
    public CitySearchPage(CitySearchPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
