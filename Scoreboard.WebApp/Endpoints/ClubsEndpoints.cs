using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

/// <summary>Clubs of the salon the calling kiosk is paired with.</summary>
public static class ClubsEndpoints
{
    public static RouteGroupBuilder MapClubsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clubs").WithTags("Clubs");

        group.MapGet("/", async (ClubService clubs, HttpContext context) =>
            (await clubs.ListAsync(context.GetOrganizationId())).Select(ToDto).ToList());

        group.MapPost("/", async (UpsertClubRequest request, ClubService clubs, HttpContext context) =>
        {
            try
            {
                var club = await clubs.UpsertAsync(
                    context.GetOrganizationId(), request.Id, request.Name, request.ShortName, request.City, request.PrimaryColor);
                return Results.Ok(ToDto(club));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, ClubService clubs, HttpContext context) =>
        {
            try
            {
                return await clubs.DeleteAsync(context.GetOrganizationId(), id) ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return group;
    }

    private static ClubDto ToDto(Club club) => new(club.Id, club.Name, club.ShortName, club.City, club.PrimaryColor);
}
