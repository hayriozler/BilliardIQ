namespace Scoreboard.Client.Services;

/// <summary>Tells the open board that the language changed behind its back (it follows the venue's language on the server).</summary>
public class LanguageSync
{
    public event Action? Changed;

    public void Notify() => Changed?.Invoke();
}
