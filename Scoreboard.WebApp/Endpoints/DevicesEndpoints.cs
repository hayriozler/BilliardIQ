using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

/// <summary>Admin API for pairing scoreboard devices. Requires the panel's cookie login.</summary>
public static class DevicesEndpoints
{
    public static RouteGroupBuilder MapDevicesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/devices").WithTags("Devices").RequireAuthorization();

        group.MapGet("/", async (DeviceService devices, HttpContext context) =>
            (await devices.ListAsync(context.User.GetOrganizationId())).Select(ToDto).ToList());

        group.MapPost("/", async (RegisterDeviceRequest request, DeviceService devices, HttpContext context) =>
        {
            try
            {
                var device = await devices.RegisterAsync(context.User.GetOrganizationId(), request.Name, request.TableId);
                return Results.Created($"/api/devices/{device.Id}", ToDto(device));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, DeviceService devices, HttpContext context) =>
            await devices.DeleteAsync(context.User.GetOrganizationId(), id) ? Results.NoContent() : Results.NotFound());

        return group;
    }

    private static DeviceDto ToDto(Device d) =>
        new(d.Id, d.PairingCode, d.Name, d.TableId, d.Table?.Number, d.CreatedAt, d.LastSeenAt);
}
