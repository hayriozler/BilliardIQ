using Microsoft.AspNetCore.Components.Authorization;
using Scoreboard.WebApp.Security;

namespace Scoreboard.WebApp.Services;

public class ScopedRunner(IServiceScopeFactory scopeFactory, AuthenticationStateProvider authState)
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
        var user = (await authState.GetAuthenticationStateAsync()).User;
        var scope = scopeFactory.CreateScope();
        if (user.Identity?.IsAuthenticated == true && user.FindFirst(AuthClaims.OrganizationId) is { } claim)
        {
            scope.ServiceProvider.GetRequiredService<OrganizationService>().Override = int.Parse(claim.Value);
        }

        return scope;
    }
}
