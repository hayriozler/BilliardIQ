namespace BilliardIQ.Mobile.Services.Api;

public static class ApiSettings
{
    private const string BaseUrlKey = "api_base_url";
    private const string DefaultBaseUrl = "https://www.billiardiq.com/";

    public static string BaseUrl
    {
        get
        {
            var value = Preferences.Default.Get(BaseUrlKey, DefaultBaseUrl);
            return value.EndsWith('/') ? value : value + "/";
        }
        set => Preferences.Default.Set(BaseUrlKey, value);
    }
}
