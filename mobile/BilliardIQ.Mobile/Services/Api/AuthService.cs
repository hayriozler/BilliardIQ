namespace BilliardIQ.Mobile.Services.Api;

public sealed class AuthService(ApiClient api, SessionStore session)
{
    public const string LoginRoute = "//login";
    public const string OrganizationRoute = "//organization";
    public const string ChangePasswordRoute = "//changepassword";
    public const string PlayerHomeRoute = "//playerhome";
    public const string HomeRoute = "//home";

    public static string RouteFor(ApiSession current) =>
        current.MustChangePassword ? ChangePasswordRoute
        : current.NeedsOrganization ? OrganizationRoute
        : current.Role == ApiRoles.Player ? PlayerHomeRoute
        : HomeRoute;

    public async Task<ApiSession> LoginAsync(string email, string password)
    {
        var result = await api.PostAsync<ApiSession>("api/mobile/auth/login", new LoginRequest(email, password, DeviceName()), authorize: false);
        await session.SetAsync(result);
        return result;
    }

    public async Task<ApiSession> RegisterWithInviteAsync(string code, string email, string password)
    {
        var result = await api.PostAsync<ApiSession>("api/mobile/auth/register-player", new InviteRegisterRequest(code, email, password, DeviceName()), authorize: false);
        await session.SetAsync(result);
        return result;
    }

    public async Task<ApiSession?> RestoreAsync()
    {
        if (session.Current is { } existing)
        {
            return existing;
        }

        if (string.IsNullOrEmpty(await session.GetRefreshTokenAsync()))
        {
            return null;
        }

        return await api.TryRefreshAsync() ? session.Current : null;
    }

    public async Task<ApiSession> SelectOrganizationAsync(int organizationId)
    {
        var result = await api.PostAsync<ApiSession>("api/mobile/auth/organization", new SelectOrganizationRequest(organizationId));
        session.Update(result);
        return session.Current!;
    }

    public Task RequestPasswordResetAsync(string email) =>
        api.PostAsync("api/mobile/auth/forgot-password", new ForgotPasswordRequest(email), authorize: false);

    public Task ResetPasswordAsync(string email, string code, string newPassword) =>
        api.PostAsync("api/mobile/auth/reset-password", new ResetPasswordRequest(email, code, newPassword), authorize: false);

    public async Task<ApiSession> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var result = await api.PostAsync<ApiSession>("api/mobile/auth/password", new ChangePasswordRequest(currentPassword, newPassword));
        session.Update(result);
        return session.Current!;
    }

    public async Task ChangeEmailAsync(string email, string password)
    {
        await api.PutAsync("api/mobile/me/email", new ChangeEmailRequest(email, password));
        var refreshed = await api.GetAsync<ApiSession>("api/mobile/me/");
        session.Update(refreshed);
    }

    public async Task SignOutAsync()
    {
        var refreshToken = await session.GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try
            {
                await api.PostAsync("api/mobile/auth/logout", new RefreshRequest(refreshToken), authorize: false);
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
            }
        }

        session.Clear();
    }

    private static string DeviceName() => $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();
}
