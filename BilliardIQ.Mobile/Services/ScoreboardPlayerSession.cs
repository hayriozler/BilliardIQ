namespace BilliardIQ.Mobile.Services;

public class ScoreboardPlayerSession
{
    private int _nextId = 1;

    public int NextId() => _nextId++;
}
