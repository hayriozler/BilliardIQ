using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BilliardIQ.Mobile.Services.Api;

public sealed class ApiClient
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan _refreshMargin = TimeSpan.FromSeconds(60);

    private readonly HttpClient _http;
    private readonly SessionStore _session;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ApiClient(SessionStore session)
    {
        _session = session;
        _http = new HttpClient { BaseAddress = new Uri(ApiSettings.BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    public event EventHandler? SessionExpired;

    public Task<T> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, true, ct);

    public async Task<byte[]> GetBytesAsync(string path, CancellationToken ct = default)
    {
        using var response = await SendOnceAsync(HttpMethod.Get, path, null, false, ct);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public Task<T> PostAsync<T>(string path, object? body, bool authorize = true, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, authorize, ct);

    public Task PostAsync(string path, object? body, bool authorize = true, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, path, body, authorize, ct);

    public Task DeleteAsync(string path, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, path, null, true, ct);

    public Task PutAsync(string path, object? body, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, path, body, true, ct);

    public Task<T> PutAsync<T>(string path, object? body, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Put, path, body, true, ct);

    public async Task<bool> TryRefreshAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            return await RefreshCoreAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, bool authorize, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, path, body, authorize, ct);
        await EnsureSuccessAsync(response);
        var result = await response.Content.ReadFromJsonAsync<T>(_json, ct);
        return result ?? throw new ApiException((int)response.StatusCode, "Empty response.");
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, bool authorize, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, path, body, authorize, ct);
        await EnsureSuccessAsync(response);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, object? body, bool authorize, CancellationToken ct)
    {
        if (authorize)
        {
            await EnsureFreshTokenAsync(ct);
        }

        var response = await SendOnceAsync(method, path, body, authorize, ct);
        if (authorize && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await TryRefreshAsync(ct))
            {
                response.Dispose();
                response = await SendOnceAsync(method, path, body, authorize, ct);
            }
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string path, object? body, bool authorize, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.AcceptLanguage.ParseAdd(LocalizationManager.Instance.CurrentLanguage);
        if (authorize && _session.AccessToken is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: _json);
        }

        try
        {
            return await _http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is WebException or IOException)
        {
            throw new HttpRequestException(ex.Message, ex);
        }
    }

    private async Task EnsureFreshTokenAsync(CancellationToken ct)
    {
        if (_session.Current is { } current && current.AccessTokenExpiresAt - DateTimeOffset.UtcNow < _refreshMargin)
        {
            await TryRefreshAsync(ct);
        }
    }

    private async Task<bool> RefreshCoreAsync(CancellationToken ct)
    {
        var refreshToken = await _session.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken))
        {
            if (_session.IsSignedIn)
            {
                _session.Clear();
                SessionExpired?.Invoke(this, EventArgs.Empty);
            }

            return false;
        }

        try
        {
            using var response = await SendOnceAsync(HttpMethod.Post, "api/mobile/auth/refresh", new RefreshRequest(refreshToken), false, ct);
            if (response.IsSuccessStatusCode)
            {
                var session = await response.Content.ReadFromJsonAsync<ApiSession>(_json, ct);
                if (session is not null)
                {
                    await _session.SetAsync(session);
                    return true;
                }

                return false;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _session.Clear();
                SessionExpired?.Invoke(this, EventArgs.Empty);
            }

            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var text = await response.Content.ReadAsStringAsync();
        string message = text;
        try
        {
            message = JsonSerializer.Deserialize<string>(text, _json) ?? text;
        }
        catch (JsonException)
        {
        }

        throw new ApiException((int)response.StatusCode, message);
    }
}
