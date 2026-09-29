using BillardIQ.Mobile.PageModels.PlayerPageModels;

namespace BillardIQ.Mobile.Pages.Players;

public partial class CitySearchPage : ContentPage
{
    public CitySearchPage(CitySearchPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
