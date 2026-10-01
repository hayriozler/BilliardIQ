using BillardIQ.Mobile.PageModels.PlayerPageModels;

namespace BillardIQ.Mobile.Pages.Players;

public partial class PlayerProfileViewPage : BasePage
{
    public PlayerProfileViewPage(PlayerProfilePageModel playerProfileViewModel) : base(playerProfileViewModel) => InitializeComponent();
}