using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

// SQLite integer columns come back as Int64 and DatabaseExecutor's reflection mapper only
// special-cases non-nullable Int32, so nullable int columns (TeamId, ShortcutNumber, RemoteId) are read
// as a plain int here (0 = none) and translated to nullable fields at the boundary.
internal class ScoreboardPlayerRow
{
    public int Id { get; set; }
    public int RemoteId { get; set; }
    public string NickName { get; set; } = "";
    public string Name { get; set; } = "";
    public byte[]? Photo { get; set; }
    public string? AvatarKey { get; set; }
    public int TeamId { get; set; }
    public int ShortcutNumber { get; set; }
}

public class ScoreboardPlayerRepository(ILogger<ScoreboardPlayerRepository> Logger, DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task UpsertAsync(ScoreboardPlayer player)
    {
        var parameters = new List<SqliteParameter>
        {
            new("@Id", player.Id),
            new("@RemoteId", (object?)player.RemoteId ?? DBNull.Value),
            new("@NickName", player.NickName),
            new("@Name", player.Name),
            new("@Photo", (object?)player.Photo ?? DBNull.Value),
            new("@AvatarKey", (object?)player.AvatarKey ?? DBNull.Value),
            new("@TeamId", player.TeamId ?? 0),
            new("@ShortcutNumber", (object?)player.ShortcutNumber ?? DBNull.Value),
        };
        var ok = await dbExecutor.ExecuteAsync(
            @"INSERT INTO ScoreboardPlayers(Id, RemoteId, NickName, Name, Photo, AvatarKey, TeamId, ShortcutNumber)
              VALUES(@Id, @RemoteId, @NickName, @Name, @Photo, @AvatarKey, @TeamId, @ShortcutNumber)
              ON CONFLICT(Id) DO UPDATE SET
                RemoteId = excluded.RemoteId,
                NickName = excluded.NickName,
                Name = excluded.Name,
                Photo = excluded.Photo,
                AvatarKey = excluded.AvatarKey,
                TeamId = excluded.TeamId,
                ShortcutNumber = excluded.ShortcutNumber",
            parameters);

        if (ok) Logger.LogInformation("Scoreboard player {Id} upserted", player.Id);
        else Logger.LogDebug("Something went wrong upserting scoreboard player {Id}", player.Id);
    }

    public async Task DeleteAllAsync()
    {
        var ok = await dbExecutor.ExecuteAsync("DELETE FROM ScoreboardPlayers;");
        if (ok) Logger.LogInformation("All scoreboard players deleted");
        else Logger.LogDebug("Something went wrong deleting all scoreboard players");
    }

    public async Task<IReadOnlyList<ScoreboardPlayer>> GetAllAsync()
    {
        var rows = await dbExecutor.ReadDataAsync<ScoreboardPlayerRow>(
            "SELECT Id, RemoteId, NickName, Name, Photo, AvatarKey, TeamId, ShortcutNumber FROM ScoreboardPlayers ORDER BY Id");

        return rows.Select(r => new ScoreboardPlayer
        {
            Id = r.Id,
            RemoteId = r.RemoteId == 0 ? null : r.RemoteId,
            NickName = r.NickName,
            Name = r.Name,
            Photo = r.Photo,
            AvatarKey = r.AvatarKey,
            TeamId = r.TeamId == 0 ? null : r.TeamId,
            ShortcutNumber = r.ShortcutNumber == 0 ? null : r.ShortcutNumber,
        }).ToList();
    }
}
