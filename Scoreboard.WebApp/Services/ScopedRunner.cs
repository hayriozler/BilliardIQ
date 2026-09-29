namespace Scoreboard.WebApp.Services;

/// <summary>
/// Runs a service call in its own DI scope (fresh DbContext). Blazor Server components live as long as the
/// circuit, so a shared DbContext would go stale and could be hit concurrently by timers and click handlers.
/// </summary>
public class ScopedRunner(IServiceScopeFactory scopeFactory)
{
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
    {
        using var scope = scopeFactory.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task RunAsync<TService>(Func<TService, Task> action) where TService : notnull
    {
        using var scope = scopeFactory.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<TService>());
    }
}
