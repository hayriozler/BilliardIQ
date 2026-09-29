using BilliardIQ.Mobile.PageModels.PlayerPageModels;

namespace BilliardIQ.Mobile.Pages.Players;

public partial class PlayerProfileViewPage : BasePage
{
    public PlayerProfileViewPage(PlayerProfilePageModel playerProfileViewModel) : base(playerProfileViewModel) => InitializeComponent();
}