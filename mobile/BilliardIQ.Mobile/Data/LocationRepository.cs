using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using Microsoft.Data.Sqlite;

namespace BilliardIQ.Mobile.Data;

public class LocationRepository(DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task<IReadOnlyList<CountryItem>> GetCountriesAsync()
        => await dbExecutor.ReadDataAsync<CountryItem>("SELECT Code, Name FROM Countries ORDER BY Name", []);

    public async Task<IReadOnlyList<CityItem>> GetCitiesByCountryAsync(string countryCode)
    {
        var parameters = new List<SqliteParameter> { new("@CountryCode", countryCode) };
        return await dbExecutor.ReadDataAsync<CityItem>("SELECT Id, CountryCode, Name FROM Cities WHERE CountryCode=@CountryCode ORDER BY Name", parameters);
    }
}
