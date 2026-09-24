using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

// See ScoreboardPlayerRepository for why RemoteId is read as a plain int (0 = none) here.
internal class TeamRow
{
    public int Id { get; set; }
    public int RemoteId { get; set; }
    public string Name { get; set; } = "";
}

public class TeamRepository(ILogger<TeamRepository> Logger, DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task UpsertAsync(ScoreboardTeam team)
    {
        var parameters = new List<SqliteParameter>
        {
            new("@Id", team.Id),
            new("@RemoteId", (object?)team.RemoteId ?? DBNull.Value),
            new("@Name", team.Name),
        };
        var ok = await dbExecutor.ExecuteAsync(
            @"INSERT INTO Teams(Id, RemoteId, Name) VALUES(@Id, @RemoteId, @Name)
              ON CONFLICT(Id) DO UPDATE SET RemoteId = excluded.RemoteId, Name = excluded.Name",
            parameters);

        if (ok) Logger.LogInformation("Team {Id} upserted", team.Id);
        else Logger.LogDebug("Something went wrong upserting team {Id}", team.Id);
    }

    public async Task<IReadOnlyList<ScoreboardTeam>> GetAllAsync()
    {
        var rows = await dbExecutor.ReadDataAsync<TeamRow>("SELECT Id, RemoteId, Name FROM Teams ORDER BY Id");

        return rows.Select(r => new ScoreboardTeam
        {
            Id = r.Id,
            RemoteId = r.RemoteId == 0 ? null : r.RemoteId,
            Name = r.Name,
        }).ToList();
    }
}
