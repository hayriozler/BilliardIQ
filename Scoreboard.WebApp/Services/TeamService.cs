using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Services;

public class TeamService(ScoreboardDbContext db)
{
    public Task<List<Team>> ListForClubAsync(int clubId) =>
        db.TeamSet
            .Where(t => t.ClubId == clubId)
            .Include(t => t.TeamPlayers)
            .ThenInclude(tp => tp.Player)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<Team> UpsertAsync(int clubId, int id, string name)
    {
        var team = id != 0 ? await db.TeamSet.FirstOrDefaultAsync(t => t.Id == id && t.ClubId == clubId) : null;
        if (team is null)
        {
            team = new Team { ClubId = clubId };
            db.TeamSet.Add(team);
        }

        team.Name = name;
        team.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await LoadWithPlayersAsync(team.Id);
    }

    public async Task<bool> DeleteAsync(int clubId, int id)
    {
        var team = await db.TeamSet.FirstOrDefaultAsync(t => t.Id == id && t.ClubId == clubId);
        if (team is null)
        {
            return false;
        }

        db.TeamSet.Remove(team);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Team?> SetPlayersAsync(int clubId, int id, int[] playerIds)
    {
        var team = await db.TeamSet
            .Include(t => t.TeamPlayers)
            .FirstOrDefaultAsync(t => t.Id == id && t.ClubId == clubId);
        if (team is null)
        {
            return null;
        }

        var distinctIds = playerIds.Distinct().ToArray();
        var players = await db.PlayerSet.Where(p => p.ClubId == clubId && distinctIds.Contains(p.Id)).ToListAsync();
        if (players.Count != distinctIds.Length)
        {
            throw new ArgumentException("One or more players were not found for this club.");
        }

        db.TeamPlayerSet.RemoveRange(team.TeamPlayers);
        foreach (var player in players)
        {
            db.TeamPlayerSet.Add(new TeamPlayer { TeamId = team.Id, PlayerId = player.Id });
        }

        await db.SaveChangesAsync();
        return await LoadWithPlayersAsync(team.Id);
    }

    public async Task<Team?> RemovePlayerAsync(int clubId, int teamId, int playerId)
    {
        var team = await db.TeamSet
            .Include(t => t.TeamPlayers)
            .FirstOrDefaultAsync(t => t.Id == teamId && t.ClubId == clubId);
        if (team is null)
        {
            return null;
        }

        var teamPlayer = team.TeamPlayers.FirstOrDefault(tp => tp.PlayerId == playerId);
        if (teamPlayer is null)
        {
            return null;
        }

        db.TeamPlayerSet.Remove(teamPlayer);
        await db.SaveChangesAsync();
        return await LoadWithPlayersAsync(team.Id);
    }

    private Task<Team> LoadWithPlayersAsync(int teamId) =>
        db.TeamSet
            .Include(t => t.TeamPlayers)
            .ThenInclude(tp => tp.Player)
            .FirstAsync(t => t.Id == teamId);
}
