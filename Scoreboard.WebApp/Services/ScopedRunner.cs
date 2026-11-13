using Microsoft.AspNetCore.Components.Authorization;
using Scoreboard.WebApp.Security;

namespace Scoreboard.WebApp.Services;

public class ScopedRunner : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuthenticationStateProvider _authState;
    private int? _organizationId;
    private bool _resolved;

    public ScopedRunner(IServiceScopeFactory scopeFactory, AuthenticationStateProvider authState)
    {
        _scopeFactory = scopeFactory;
        _authState = authState;
        authState.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) => _resolved = false;

    public void Dispose()
    {
        _authState.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        GC.SuppressFinalize(this);
    }

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
        if (!_resolved)
        {
            var user = (await _authState.GetAuthenticationStateAsync()).User;
            _organizationId = user.Identity?.IsAuthenticated == true && user.FindFirst(AuthClaims.OrganizationId) is { } claim
                ? int.Parse(claim.Value)
                : null;
            _resolved = true;
        }

        var scope = _scopeFactory.CreateScope();
        if (_organizationId is { } organizationId)
        {
            scope.ServiceProvider.GetRequiredService<OrganizationService>().Override = organizationId;
        }

        return scope;
    }
}
