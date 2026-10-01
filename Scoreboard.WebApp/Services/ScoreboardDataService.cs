using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Responses;

namespace Scoreboard.WebApp.Services;

public class ScoreboardDataService(DataContext db, IOrganizationService organizationService, ClubService clubs, PlayerService players, TeamService teams)
{
    private int OrganizationId => organizationService.GetCurrentOrganizationId() ?? throw new InvalidOperationException("No organization for this request.");

    public async Task<OrganizationDto> GetOrganizationAsync()
    {
        var o = await db.OrganizationSet.AsNoTracking().FirstAsync(x => x.Id == OrganizationId);
        return new OrganizationDto(o.Name, o.Language, o.CountryCode, o.Currency, o.TimeZone);
    }

    public async Task<List<ClubDto>> ListClubsAsync() =>
        [.. (await clubs.ListAsync(OrganizationId)).Select(ToDto)];

    public async Task<List<PlayerDto>> ListPlayersAsync()
    {
        var language = await LanguageOfAsync();
        return [.. (await players.ListForOrganizationAsync(OrganizationId)).Select(p => ToDto(p, language))];
    }

    public async Task<List<TeamDto>> ListTeamsAsync() =>
        [.. (await teams.ListForOrganizationAsync(OrganizationId)).Select(ToDto)];

    public async Task<string> LanguageOfAsync() =>
        await db.OrganizationSet.Where(o => o.Id == OrganizationId).Select(o => o.Language).FirstOrDefaultAsync() ?? Loc.DefaultLanguage;

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
            p.Email ?? string.Empty, p.Level, p.Nationality ?? string.Empty, p.City ?? string.Empty, p.UpdatedAt, p.ShortcutNumber,
            p.FederationLicenseNo, p.LicenseValidUntil, p.Association?.Name, p.IsSystem, p.SystemSlot, p.AssociationId, p.RegionId, p.Region?.Name, p.CountryId, p.CityId);
    }
}
