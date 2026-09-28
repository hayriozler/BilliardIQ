using Zeymera.Scoreboard.Client.Services;

namespace Zeymera.Scoreboard.Client.Models;

public static class PlayerDisplayNameResolver
{
    public static string Resolve(Player player, LocalizationService localization)
    {
        if (player.Id is 1 or 2)
        {
            return localization.T(player.Id == 1 ? "Player1Label" : "Player2Label");
        }

        return string.IsNullOrWhiteSpace(player.Nickname) ? player.Name : player.Nickname;
    }
}
