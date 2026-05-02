using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using Microsoft.Data.Sqlite;

namespace BilliardIQ.Mobile.Data;

public class LocationRepository(DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task<IReadOnlyList<CountryItem>> GetCountriesAsync()
        => await dbExecutor.ReadDataAsync<CountryItem>("SELECT Id, Code, Name FROM Countries ORDER BY Name", []);

    public async Task<IReadOnlyList<CityItem>> GetCitiesByCountryIdAsync(int countryId)
    {
        var parameters = new List<SqliteParameter> { new("@CountryId", countryId) };
        return await dbExecutor.ReadDataAsync<CityItem>("SELECT Id, CountryId, Name FROM Cities WHERE CountryId=@CountryId ORDER BY Name", parameters);
    }
}
