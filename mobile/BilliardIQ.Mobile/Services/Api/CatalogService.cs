namespace BilliardIQ.Mobile.Services.Api;

public sealed class CatalogService(ApiClient api)
{
    private CatalogDto? _catalog;

    public async Task<CatalogDto> GetAsync(bool refresh = false)
    {
        if (_catalog is null || refresh)
        {
            _catalog = await api.GetAsync<CatalogDto>("api/mobile/catalog");
        }

        return _catalog;
    }

    public void Clear() => _catalog = null;
}
