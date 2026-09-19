namespace BilliardIQ.Mobile.Services;

// Hands out a sequential Id for each player sent to the scoreboard hardware this app
// session. BilliardIQ never persists player data itself — the Pi is the source of truth.
public class ScoreboardPlayerSession
{
    private int _nextId = 1;

    public int NextId() => _nextId++;
}
