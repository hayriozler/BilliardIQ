using Scoreboard.Client.Services;

namespace Scoreboard.Client.Models;

public static class PlayerDisplayNameResolver
{
    public static string Resolve(Player player, LocalizationService localization)
    {
        if (player.IsSystem && player.SystemSlot is 1 or 2)
        {
            return localization.T(player.SystemSlot == 1 ? "Player1Label" : "Player2Label");
        }

        return string.IsNullOrWhiteSpace(player.Nickname) ? player.Name : player.Nickname;
    }
}
