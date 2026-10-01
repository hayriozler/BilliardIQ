using Microsoft.AspNetCore.Components.Authorization;
using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Security;

namespace Scoreboard.WebApp.Services;

/// <summary>
/// Runs a service call in its own DI scope (fresh DbContext). Blazor Server components live as long as the
/// circuit, so a shared DbContext would go stale and could be hit concurrently by timers and click handlers.
/// </summary>
public class ScopedRunner(IServiceScopeFactory scopeFactory, AuthenticationStateProvider authState, OrganizationScope circuitScope)
{
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
    {
        using var scope = await CreateScopeAsync();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task RunAsync<TService>(Func<TService, Task> action) where TService : notnull
    {
        using var scope = await CreateScopeAsync();
        await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private async Task<IServiceScope> CreateScopeAsync()
    {
        if (circuitScope.OrganizationId is null)
        {
            var user = (await authState.GetAuthenticationStateAsync()).User;
            if (user.Identity?.IsAuthenticated == true && user.FindFirst(AuthClaims.OrganizationId) is { } claim)
            {
                circuitScope.OrganizationId = int.Parse(claim.Value);
            }
        }

        var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<OrganizationScope>().OrganizationId = circuitScope.OrganizationId;
        return scope;
    }
}
