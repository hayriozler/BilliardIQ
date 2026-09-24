using BilliardIQ.Mobile.Models;
using System.Collections.ObjectModel;

namespace BilliardIQ.Mobile.Services;

public class ScoreboardPlayerSession
{
    private int _nextId = 1;

    public ObservableCollection<ScoreboardPlayer> Players { get; } = [];

    public int NextId() => _nextId++;

    public void Add(ScoreboardPlayer player) => Players.Add(player);

    public ScoreboardPlayer? FindById(int id) => Players.FirstOrDefault(p => p.Id == id);

    public ScoreboardPlayer? FindByShortcut(int shortcutNumber) => Players.FirstOrDefault(p => p.ShortcutNumber == shortcutNumber);

    public ScoreboardPlayer? FindByRemoteId(int remoteId) => Players.FirstOrDefault(p => p.RemoteId == remoteId);

    public void LoadExisting(IEnumerable<ScoreboardPlayer> players)
    {
        foreach (var player in players) Players.Add(player);
        if (Players.Count > 0) _nextId = Players.Max(p => p.Id) + 1;
    }

    public void Clear()
    {
        // Remove one at a time (not Players.Clear()) — Clear() raises a Reset notification that
        // forces a bulk rebind of every visible row in any CollectionView bound to this collection,
        // which crashes a RelativeSource-ancestor binding mid-recycle (see PlayerStatsListPageModel).
        for (var i = Players.Count - 1; i >= 0; i--) Players.RemoveAt(i);
        _nextId = 1;
    }
}
