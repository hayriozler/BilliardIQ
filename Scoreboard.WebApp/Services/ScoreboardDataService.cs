using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Responses;

namespace Scoreboard.WebApp.Services;

public class ScoreboardDataService(DataContext db, ClubService clubs, PlayerService players, TeamService teams)
{
    public async Task<OrganizationDto> GetOrganizationAsync()
    {
        var o = await db.OrganizationSet.AsNoTracking().FirstAsync(x => x.Id == db.CurrentOrganizationId);
        return new OrganizationDto(o.Name, o.Language, o.CountryCode, o.Currency, o.TimeZone);
    }

    public async Task<List<ClubDto>> ListClubsAsync() =>
        [.. (await clubs.ListAsync()).Select(ToDto)];

    public async Task<List<PlayerDto>> ListPlayersAsync()
    {
        var language = await LanguageOfAsync();
        return [.. (await players.ListForOrganizationAsync()).Select(p => ToDto(p, language))];
    }

    public async Task<List<TeamDto>> ListTeamsAsync() =>
        [.. (await teams.ListForOrganizationAsync()).Select(ToDto)];

    private static readonly TimeSpan _fullSyncInterval = TimeSpan.FromHours(24);

    private sealed record PendingKey(string Entity, int Id, long Seq);

    private Task<int?> TableIdAsync(int tableNo) =>
        db.BilliardTableSet.AsNoTracking()
            .Where(t => t.ScoreboardNo == tableNo && t.DeletedAt == null)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

    public async Task<ChangeSetDto> GetChangesAsync(string instanceId, int tableNo, bool resync, string? ipAddress = null)
    {
        if (await TableIdAsync(tableNo) is not int tableId)
        {
            return await SnapshotAsync();
        }

        var now = DateTimeOffset.UtcNow;
        var state = await db.ClientSyncSet.FirstOrDefaultAsync(s => s.TableId == tableId);
        var full = resync || state is null || state.InstanceId != instanceId || state.PendingFull
            || now - state.LastFullSyncAt > _fullSyncInterval;

        var rows = await db.EntityChangeSet.AsNoTracking().Where(c => c.TableId == tableId).ToListAsync();
        var result = full ? await SnapshotAsync() : await DeltaAsync(rows);

        if (state is null)
        {
            state = new ClientSync { OrganizationId = db.CurrentOrganizationId, TableId = tableId, InstanceId = instanceId };
            db.ClientSyncSet.Add(state);
        }

        var keys = JsonSerializer.Serialize(rows.Select(r => new PendingKey(r.EntityName, r.EntityId, r.Seq)));
        var changed = full || rows.Count > 0 || state.PendingKeys != keys || (ipAddress is not null && state.IpAddress != ipAddress) || now - state.LastSyncAt > TimeSpan.FromMinutes(1);
        state.InstanceId = instanceId;
        state.PendingFull = full;
        state.IpAddress = ipAddress ?? state.IpAddress;
        state.PendingKeys = keys;
        state.LastSyncAt = now;
        if (changed)
        {
            await db.SaveChangesAsync();
        }

        return result;
    }

    public async Task AckChangesAsync(string instanceId, int tableNo)
    {
        if (await TableIdAsync(tableNo) is not int tableId)
        {
            return;
        }

        var state = await db.ClientSyncSet.FirstOrDefaultAsync(s => s.TableId == tableId && s.InstanceId == instanceId);
        if (state is null)
        {
            return;
        }

        var keys = JsonSerializer.Deserialize<List<PendingKey>>(state.PendingKeys) ?? [];
        if (keys.Count > 0)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(EntityChangeSql.Remove(
                state.OrganizationId, tableId, keys.Select(k => k.Entity).ToArray(), keys.Select(k => k.Id).ToArray(), keys.Select(k => k.Seq).ToArray()));
        }

        var now = DateTimeOffset.UtcNow;
        state.PendingKeys = "[]";
        state.LastSyncAt = now;
        if (state.PendingFull)
        {
            state.LastFullSyncAt = now;
            state.PendingFull = false;
        }

        await db.SaveChangesAsync();
    }

    private async Task<ChangeSetDto> SnapshotAsync()
    {
        var language = await LanguageOfAsync();
        return new ChangeSetDto(true,
        [
            Changed(nameof(Club), (await clubs.ListAsync()).Select(ToDto)),
            Changed(nameof(Team), (await teams.ListForOrganizationAsync()).Select(ToDto)),
            Changed(nameof(Player), (await players.ListForOrganizationAsync()).Select(p => ToDto(p, language)))
        ]);
    }

    private async Task<ChangeSetDto> DeltaAsync(List<EntityChange> rows)
    {
        var groups = new List<ChangeGroupDto>();

        var clubIds = IdsOf(rows, nameof(Club));
        var clubList = clubIds.Count == 0 ? [] : await clubs.ListByIdsAsync(clubIds);
        AddGroups(groups, nameof(Club), clubIds, clubList.Where(c => c.DeletedAt is null).Select(c => (c.Id, (object)ToDto(c))));

        var teamIds = IdsOf(rows, nameof(Team));
        var teamList = teamIds.Count == 0 ? [] : await teams.ListByIdsAsync(teamIds);
        AddGroups(groups, nameof(Team), teamIds, teamList.Where(t => t.DeletedAt is null).Select(t => (t.Id, (object)ToDto(t))));

        var playerIds = IdsOf(rows, nameof(Player));
        var language = playerIds.Count == 0 ? Loc.DefaultLanguage : await LanguageOfAsync();
        var playerList = playerIds.Count == 0 ? [] : await players.ListByIdsAsync(playerIds);
        AddGroups(groups, nameof(Player), playerIds, playerList.Where(p => p.DeletedAt is null).Select(p => (p.Id, (object)ToDto(p, language))));

        return new ChangeSetDto(false, groups);
    }

    private static List<int> IdsOf(List<EntityChange> rows, string entityType) =>
        rows.Where(r => r.EntityName == entityType).Select(r => r.EntityId).ToList();

    private static ChangeGroupDto Changed(string entityType, IEnumerable<object> items) =>
        new(entityType, ChangeState.Changed, items.ToList(), null);

    private static void AddGroups(List<ChangeGroupDto> groups, string entityType, List<int> requestedIds, IEnumerable<(int Id, object Dto)> alive)
    {
        var aliveList = alive.ToList();
        if (aliveList.Count > 0)
        {
            groups.Add(new ChangeGroupDto(entityType, ChangeState.Changed, aliveList.Select(a => a.Dto).ToList(), null));
        }

        var aliveIds = aliveList.Select(a => a.Id).ToHashSet();
        var gone = requestedIds.Where(id => !aliveIds.Contains(id)).ToList();
        if (gone.Count > 0)
        {
            groups.Add(new ChangeGroupDto(entityType, ChangeState.Deleted, null, gone));
        }
    }

    public async Task<string> LanguageOfAsync() =>
        await db.OrganizationSet.Where(o => o.Id == db.CurrentOrganizationId).Select(o => o.Language).FirstOrDefaultAsync() ?? Loc.DefaultLanguage;

    public static ClubDto ToDto(Club club) => new(club.Id, club.Name, club.ShortName, club.City, club.PrimaryColor);

    public static TeamDto ToDto(Team team) => new(
        team.Id, team.ClubId, team.Name, team.UpdatedAt,
        team.Members.Select(m => new TeamPlayerDto(
            m.Player.Id, m.Player.Nickname ?? m.Player.DisplayName, $"{m.Player.FirstName} {m.Player.LastName}".Trim())).ToList(),
        team.AvatarId);

    public static PlayerDto ToDto(Player p, string language)
    {
        var shown = p.IsSystem && p.SystemSlot is int slot ? SystemPlayerService.NameFor(slot, language) : null;
        return new PlayerDto(
            p.Id, null, shown ?? p.Nickname ?? p.DisplayName, shown ?? $"{p.FirstName} {p.LastName}".Trim(), p.PhotoUrl, p.AvatarId,
            p.Level, p.Nationality ?? string.Empty, p.City ?? string.Empty, p.UpdatedAt, p.ShortcutNumber,
            p.FederationLicenseNo, p.LicenseValidUntil, p.Association?.Name, p.IsSystem, p.SystemSlot, p.AssociationId, p.RegionId, p.Region?.Name, p.CountryId, p.CityId);
    }
}
