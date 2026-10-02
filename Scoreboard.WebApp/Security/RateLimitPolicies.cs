using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Scoreboard.WebApp.Security;

public static class RateLimitPolicies
{
    public const string Credentials = "credentials";
    public const string Refresh = "refresh";

    public static IServiceCollection AddCredentialRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Credentials, context => PerClient(context, 10));
            options.AddPolicy(Refresh, context => PerClient(context, 30));
        });

    private static RateLimitPartition<string> PerClient(HttpContext context, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
}
