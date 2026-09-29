using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class TeamService(DataContext db, ClubService clubs)
{
    public Task<List<Team>> ListForOrganizationAsync(int organizationId) =>
        db.TeamSet
            .Include(t => t.Club)
            .Include(t => t.Members)
            .ThenInclude(m => m.Player)
            .Where(t => t.Club.OrganizationId == organizationId && t.DeletedAt == null)
            .OrderBy(t => t.Club.Name).ThenBy(t => t.Name)
            .ToListAsync();

    public async Task<Team> UpsertAsync(int organizationId, int id, string name, int? clubId = null)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Takım adı gerekli.");
        }

        var team = id != 0 ? await FindAsync(organizationId, id) : null;

        // Resolve the club before the new team is tracked: creating a default club saves changes.
        if (clubId is not null || team is null)
        {
            var club = clubId is null
                ? await clubs.EnsureDefaultClubAsync(organizationId)
                : await clubs.GetAsync(organizationId, clubId.Value) ?? throw new ArgumentException("Kulüp bulunamadı.");
            if (team is null)
            {
                team = new Team();
                db.TeamSet.Add(team);
            }

            team.ClubId = club.Id;
        }

        team.Name = name;
        team.ShortName = DefaultShortName(name);
        await db.SaveChangesAsync();

        return await LoadWithPlayersAsync(team.Id);
    }

    public async Task<bool> DeleteAsync(int organizationId, int id)
    {
        var team = await FindAsync(organizationId, id);
        if (team is null)
        {
            return false;
        }

        db.TeamMemberSet.RemoveRange(await db.TeamMemberSet.Where(m => m.TeamId == id).ToListAsync());
        team.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<Team?> SetPlayersAsync(int organizationId, int id, int[] playerIds)
    {
        var team = await db.TeamSet
            .Include(t => t.Club)
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id && t.Club.OrganizationId == organizationId && t.DeletedAt == null);
        if (team is null)
        {
            return null;
        }

        var distinctIds = playerIds.Distinct().ToArray();
        var found = await db.PlayerSet
            .Where(p => p.CreatedInOrganizationId == organizationId && p.DeletedAt == null && distinctIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
        if (found.Count != distinctIds.Length)
        {
            throw new ArgumentException("Bir veya daha fazla oyuncu bu salonda bulunamadı.");
        }

        db.TeamMemberSet.RemoveRange(team.Members.Where(m => !distinctIds.Contains(m.PlayerId)));
        var existing = team.Members.Select(m => m.PlayerId).ToHashSet();
        foreach (var playerId in distinctIds.Where(p => !existing.Contains(p)))
        {
            db.TeamMemberSet.Add(new TeamMember
            {
                TeamId = team.Id,
                PlayerId = playerId,
                Role = TeamMemberRole.Player,
                JoinedAt = DateOnly.FromDateTime(DateTime.UtcNow)
            });
        }

        await db.SaveChangesAsync();
        return await LoadWithPlayersAsync(team.Id);
    }

    public async Task<Team?> RemovePlayerAsync(int organizationId, int teamId, int playerId)
    {
        var team = await db.TeamSet
            .Include(t => t.Club)
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId && t.Club.OrganizationId == organizationId && t.DeletedAt == null);
        var member = team?.Members.FirstOrDefault(m => m.PlayerId == playerId);
        if (team is null || member is null)
        {
            return null;
        }

        db.TeamMemberSet.Remove(member);
        await db.SaveChangesAsync();
        return await LoadWithPlayersAsync(team.Id);
    }

    private Task<Team?> FindAsync(int organizationId, int id) =>
        db.TeamSet.FirstOrDefaultAsync(t => t.Id == id && t.Club.OrganizationId == organizationId && t.DeletedAt == null);

    private Task<Team> LoadWithPlayersAsync(int teamId) =>
        db.TeamSet
            .Include(t => t.Members)
            .ThenInclude(m => m.Player)
            .FirstAsync(t => t.Id == teamId);

    private static string DefaultShortName(string name)
    {
        var letters = new string(name.Where(char.IsLetterOrDigit).Take(4).ToArray());
        return letters.Length == 0 ? "TKM" : letters.ToUpperInvariant();
    }
}
