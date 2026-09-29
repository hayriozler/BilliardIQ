using Scoreboard.WebApp.Models;
using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class ClubsEndpoints
{
    public static RouteGroupBuilder MapClubsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clubs").WithTags("Clubs");

        group.MapGet("/", async (ClubService clubs) =>
            (await clubs.ListAsync()).Select(ToDto).ToList());

        group.MapGet("/{id:int}", async (int id, ClubService clubs) =>
            await clubs.GetAsync(id) is { } club ? Results.Ok(ToDto(club)) : Results.NotFound());

        group.MapPost("/", async (CreateClubRequest request, ClubService clubs) =>
        {
            try
            {
                var club = await clubs.CreateAsync(request.Name);
                return Results.Created($"/api/clubs/{club.Id}", ToDto(club));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, ClubService clubs) =>
            await clubs.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

        return group;
    }

    private static ClubDto ToDto(Club club) => new(club.Id, club.Code, club.Name, club.CreatedAt);
}
