namespace BilliardIQ.Mobile.Services.Api;

public static class ApiSettings
{
    private const string _baseUrlKey = "api_base_url";
#if DEBUG
    private const string _defaultBaseUrl = "http://localhost:8080/";
#else
    private const string _defaultBaseUrl = "https://www.billiardiq.com/";
#endif

    public static string BaseUrl
    {
        get
        {
            var value = Preferences.Default.Get(_baseUrlKey, _defaultBaseUrl);
            return value.EndsWith('/') ? value : value + "/";
        }
        set => Preferences.Default.Set(_baseUrlKey, value);
    }
}
