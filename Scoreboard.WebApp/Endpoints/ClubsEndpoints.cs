using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class ClubsEndpoints
{
    public static RouteGroupBuilder MapClubsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clubs").WithTags("Clubs").RequireAuthorization();

        group.MapGet("/", (ScoreboardDataService data) =>
            data.ListClubsAsync());

        group.MapPost("/", async (UpsertClubRequest request, ClubService clubs) =>
        {
            try
            {
                var club = await clubs.UpsertAsync(request.Id, request.Name, request.ShortName, request.City, request.PrimaryColor);
                return Results.Ok(ScoreboardDataService.ToDto(club));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, ClubService clubs) =>
        {
            try
            {
                return await clubs.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return group;
    }
}
