using System.Collections.ObjectModel;
using BilliardIQ.Mobile.Models;

namespace BilliardIQ.Mobile.Services;

public class TeamSession
{
    private int _nextId = 1;

    public ObservableCollection<ScoreboardTeam> Teams { get; } = [];

    public int NextId() => _nextId++;

    public ScoreboardTeam? FindById(int id) => Teams.FirstOrDefault(t => t.Id == id);

    public ScoreboardTeam? FindByRemoteId(int remoteId) => Teams.FirstOrDefault(t => t.RemoteId == remoteId);

    // Teams synced from the Pi (UpsertTeam) carry an explicit Id rather than one from NextId() —
    // advance the counter past it so a later locally-added team can't collide.
    public void Add(ScoreboardTeam team)
    {
        Teams.Add(team);
        if (team.Id >= _nextId) _nextId = team.Id + 1;
    }

    public void Remove(ScoreboardTeam team) => Teams.Remove(team);

    public void LoadExisting(IEnumerable<ScoreboardTeam> teams)
    {
        foreach (var team in teams) Teams.Add(team);
        if (Teams.Count > 0) _nextId = Teams.Max(t => t.Id) + 1;
    }
}
