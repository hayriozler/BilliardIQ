using BilliardIQ.Mobile.Models;
using System.Collections.ObjectModel;

namespace BilliardIQ.Mobile.Services;

public class ScoreboardPlayerSession
{
    private int _nextId = 1;

    public ObservableCollection<ScoreboardPlayer> Players { get; } = [];

    public int NextId() => _nextId++;

    // Players synced from the Pi (UpsertPlayer) carry an explicit Id rather than one from
    // NextId() — advance the counter past it so a later locally-added player can't collide.
    public void Add(ScoreboardPlayer player)
    {
        Players.Add(player);
        if (player.Id >= _nextId) _nextId = player.Id + 1;
    }

    public void Remove(ScoreboardPlayer player) => Players.Remove(player);

    public ScoreboardPlayer? FindById(int id) => Players.FirstOrDefault(p => p.Id == id);

    public ScoreboardPlayer? FindByShortcut(int shortcutNumber) => Players.FirstOrDefault(p => p.ShortcutNumber == shortcutNumber);

    public ScoreboardPlayer? FindByRemoteId(int remoteId) => Players.FirstOrDefault(p => p.RemoteId == remoteId);

    public void LoadExisting(IEnumerable<ScoreboardPlayer> players)
    {
        foreach (var player in players) Players.Add(player);
        if (Players.Count > 0) _nextId = Players.Max(p => p.Id) + 1;
    }
}
