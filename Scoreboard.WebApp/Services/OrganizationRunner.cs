namespace Scoreboard.WebApp.Services;

public class OrganizationRunner(IServiceScopeFactory scopeFactory)
{
    public async Task RunAsync<TService>(int organizationId, Func<TService, Task> action) where TService : notnull
    {
        using var scope = CreateScope(organizationId);
        await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task<TResult> RunAsync<TService, TResult>(int organizationId, Func<TService, Task<TResult>> action) where TService : notnull
    {
        using var scope = CreateScope(organizationId);
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private IServiceScope CreateScope(int organizationId)
    {
        var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<OrganizationService>().Override = organizationId;
        return scope;
    }
}
