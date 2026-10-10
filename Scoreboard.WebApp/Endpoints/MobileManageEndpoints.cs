using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public record MobileTable(int Id, int Number, int? ScoreboardNo, string? Label, TableType Type, TableStatus Status, DateTimeOffset? SessionOpenedAt);

public record MobileCreatePlayerRequest(
    string? Nickname, string Name, int? AvatarId, string? PhotoBase64, string? Email, Level Level,
    int? ShortcutNumber = null, string? LicenseNo = null, DateOnly? LicenseValidUntil = null,
    int? AssociationId = null, int? RegionId = null, int? CountryId = null, int? CityId = null);

public record MobileCreateTeamRequest(string Name, int? ClubId = null, int? AvatarId = null);

public record MobileCreateClubRequest(string Name, string? ShortName, string? City, string? PrimaryColor);

public static class MobileManageEndpoints
{
    public static IEndpointRouteBuilder MapMobileManageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/manage")
            .WithTags("MobileManage")
            .RequireAuthorization(JwtSettings.MobileManagerPolicy);

        group.MapGet("/tables", async (TableService tables) =>
            (await tables.ListAsync()).Select(v => new MobileTable(
                v.Table.Id, v.Table.Number, v.Table.ScoreboardNo, v.Table.Label, v.Table.Type, v.Table.Status, v.Session?.OpenedAt)));

        group.MapGet("/players", (ScoreboardDataService data) => data.ListPlayersAsync());

        group.MapGet("/teams", (ScoreboardDataService data) => data.ListTeamsAsync());

        group.MapGet("/clubs", (ScoreboardDataService data) => data.ListClubsAsync());

        var admin = app.MapGroup("/api/mobile/manage")
            .WithTags("MobileManage")
            .RequireAuthorization(JwtSettings.MobileAdminPolicy);

        admin.MapPost("/players", async (MobileCreatePlayerRequest request, PlayerService players, ScoreboardDataService data, Loc loc) =>
        {
            try
            {
                var player = await players.UpsertAsync(0, request.Nickname ?? "", request.Name ?? "", request.AvatarId,
                    request.Email ?? "", request.Level, "", "",
                    request.PhotoBase64, request.ShortcutNumber,
                    request.LicenseNo, request.LicenseValidUntil, request.AssociationId, request.RegionId, request.CountryId, request.CityId);
                return Results.Ok(ScoreboardDataService.ToDto(player, await data.LanguageOfAsync()));
            }
            catch (Exception ex) when (ex is ArgumentException or IOException)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        admin.MapPost("/teams", async (MobileCreateTeamRequest request, TeamService teams, Loc loc) =>
        {
            try
            {
                var team = await teams.UpsertAsync(0, request.Name ?? "", request.ClubId, request.AvatarId);
                return Results.Ok(ScoreboardDataService.ToDto(team));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        admin.MapPost("/clubs", async (MobileCreateClubRequest request, ClubService clubs, Loc loc) =>
        {
            try
            {
                var club = await clubs.UpsertAsync(0, request.Name ?? "", request.ShortName, request.City, request.PrimaryColor);
                return Results.Ok(ScoreboardDataService.ToDto(club));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        return app;
    }
}
