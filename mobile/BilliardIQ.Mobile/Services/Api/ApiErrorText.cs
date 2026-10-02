namespace BilliardIQ.Mobile.Services.Api;

public static class ApiErrorText
{
    public static string For(Exception ex, LocalizationManager l) => ex switch
    {
        ApiException { StatusCode: 401 } => l["Auth_InvalidCredentials"],
        ApiException { StatusCode: 403 } => l["Auth_Forbidden"],
        ApiException { Message.Length: > 0 } api => api.Message,
        ApiException => l["Auth_ServerError"],
        HttpRequestException or TaskCanceledException => l["Auth_NetworkError"],
        _ => ex.Message
    };

    public static bool IsExpected(Exception ex) => ex is ApiException or HttpRequestException or TaskCanceledException;
}
