namespace Scoreboard.Client.Services;

public class LanguageSync
{
    public event Action? Changed;

    public void Notify() => Changed?.Invoke();
}
