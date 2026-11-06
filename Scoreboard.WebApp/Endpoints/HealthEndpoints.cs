using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Endpoints;

public static class HealthEndpoints
{
    public const string BasePath = "/health";
    public const string DatabaseTag = "database";

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", HealthStatus.Unhealthy, [DatabaseTag]);
        return services;
    }

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks($"{BasePath}/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteAsync
        }).AllowAnonymous();

        app.MapHealthChecks($"{BasePath}/pg-health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(DatabaseTag),
            ResponseWriter = WriteAsync
        }).AllowAnonymous();

        app.MapHealthChecks(BasePath, new HealthCheckOptions
        {
            ResponseWriter = WriteAsync
        }).AllowAnonymous();

        return app;
    }

    private static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        var body = new
        {
            status = report.Status.ToString(),
            totalMs = (long)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = (long)e.Value.Duration.TotalMilliseconds
            })
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}

public sealed class PostgresHealthCheck(IServiceScopeFactory scopeFactory, ILogger<PostgresHealthCheck> logger) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            return await db.Database.CanConnectAsync(timeout.Token)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot connect to Postgres.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Postgres health check failed.");
            return HealthCheckResult.Unhealthy("Postgres check failed.");
        }
    }
}
