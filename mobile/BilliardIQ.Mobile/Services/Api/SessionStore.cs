namespace BilliardIQ.Mobile.Services.Api;

public sealed class SessionStore
{
    private const string RefreshTokenKey = "api_refresh_token";

    public ApiSession? Current { get; private set; }

    public string? AccessToken => Current?.AccessToken;

    public bool IsSignedIn => Current is not null;

    public bool IsAdmin => Current?.Role == ApiRoles.Admin;

    public bool IsManager => Current?.Role == ApiRoles.Manager;

    public bool IsPlayer => Current?.Role == ApiRoles.Player;

    public bool CanControlScoreboard => IsAdmin || IsManager;

    public bool CanUseSsh => IsAdmin;

    public event EventHandler? Changed;

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(ApiSession session)
    {
        Current = session;
        if (session.RefreshToken is { Length: > 0 } refresh)
        {
            try
            {
                await SecureStorage.Default.SetAsync(RefreshTokenKey, refresh);
            }
            catch
            {
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Update(ApiSession session)
    {
        Current = session with { RefreshToken = Current?.RefreshToken ?? session.RefreshToken };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Current = null;
        try
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
        }
        catch
        {
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
