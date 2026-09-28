using Microsoft.EntityFrameworkCore;
using Zeymera.BillardIQ.WebApp.Data;
using Zeymera.BillardIQ.WebApp.Models;
using Zeymera.BillardIQ.WebApp.Requests;
using Zeymera.BillardIQ.WebApp.Responses;

namespace Zeymera.BillardIQ.WebApp.Endpoints;

public static class ScoreboardClientsEndpoints
{
    public static RouteGroupBuilder MapScoreboardClientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients");

        group.MapGet("/", async (ScoreboardDbContext db, int? clubId) =>
        {
            var query = db.ScoreboardClientSet.AsQueryable();
            if (clubId is not null)
            {
                query = query.Where(c => c.ClubId == clubId);
            }

            return await query
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ScoreboardClientDto(c.Id, c.Name, c.ClubId, c.TableNumber, c.CreatedAt, c.LastSeenAt))
                .ToListAsync();
        });

        group.MapPost("/", async (RegisterScoreboardClientRequest request, ScoreboardDbContext db) =>
        {
            if (request.ClubId is not null && await db.ClubSet.FindAsync(request.ClubId) is null)
            {
                return Results.BadRequest("Club does not exist.");
            }

            string code;
            do
            {
                code = PairingCodeGenerator.Generate();
            } while (await db.ScoreboardClientSet.AnyAsync(c => c.Id == code));

            var client = new ScoreboardClient
            {
                Id = code,
                Name = request.Name?.Trim(),
                ClubId = request.ClubId,
                TableNumber = request.TableNumber
            };
            db.ScoreboardClientSet.Add(client);
            await db.SaveChangesAsync();

            return Results.Created($"/api/clients/{client.Id}", new ScoreboardClientDto(client.Id, client.Name, client.ClubId, client.TableNumber, client.CreatedAt, client.LastSeenAt));
        });

        group.MapDelete("/{id}", async (string id, ScoreboardDbContext db) =>
        {
            var client = await db.ScoreboardClientSet.FindAsync(id);
            if (client is null)
            {
                return Results.NotFound();
            }

            db.ScoreboardClientSet.Remove(client);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
