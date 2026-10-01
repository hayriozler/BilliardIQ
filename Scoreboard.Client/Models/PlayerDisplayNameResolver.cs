using Scoreboard.Client.Services;

namespace Scoreboard.Client.Models;

public static class PlayerDisplayNameResolver
{
    public static string Resolve(Player player, LocalizationService localization)
    {
        // Player 1 / Player 2 are named in the client's own language, whatever language the server created them in
        // (the venue's system players come with the venue language, e.g. "Speler 1").
        if (player.IsSystem && player.SystemSlot is 1 or 2)
        {
            return localization.T(player.SystemSlot == 1 ? "Player1Label" : "Player2Label");
        }

        return string.IsNullOrWhiteSpace(player.Nickname) ? player.Name : player.Nickname;
    }
}
