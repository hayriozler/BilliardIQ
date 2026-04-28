using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

public class LocationRepository(ILogger<LocationRepository> Logger) : BaseRepo(Logger)
{
    private readonly string _tableCreationSql = @"
CREATE TABLE IF NOT EXISTS Countries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Cities (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CountryId INTEGER NOT NULL,
    Name TEXT NOT NULL
);";

    private static readonly (string Code, string Name)[] _seedCountries =
    [
        ("TR", "Türkiye"),
        ("NL", "Netherlands"),
    ];

    private static readonly (string CountryCode, string CityName)[] _seedCities =
    [
        ("TR", "Adana"), ("TR", "Ankara"), ("TR", "Antalya"), ("TR", "Aydın"),
        ("TR", "Balıkesir"), ("TR", "Bursa"), ("TR", "Denizli"), ("TR", "Diyarbakır"),
        ("TR", "Edirne"), ("TR", "Erzurum"), ("TR", "Eskişehir"), ("TR", "Gaziantep"),
        ("TR", "Hatay"), ("TR", "İstanbul"), ("TR", "İzmir"), ("TR", "Kahramanmaraş"),
        ("TR", "Kayseri"), ("TR", "Kocaeli"), ("TR", "Konya"), ("TR", "Malatya"),
        ("TR", "Manisa"), ("TR", "Mardin"), ("TR", "Mersin"), ("TR", "Muğla"),
        ("TR", "Ordu"), ("TR", "Sakarya"), ("TR", "Samsun"), ("TR", "Şanlıurfa"),
        ("TR", "Tekirdağ"), ("TR", "Trabzon"), ("TR", "Van"), ("TR", "Zonguldak"),
        ("TR", "Adıyaman"), ("TR", "Afyonkarahisar"), ("TR", "Aksaray"), ("TR", "Ardahan"),
        ("TR", "Artvin"), ("TR", "Batman"), ("TR", "Bartın"), ("TR", "Bingöl"),
        ("TR", "Bitlis"), ("TR", "Bolu"), ("TR", "Burdur"), ("TR", "Çanakkale"),
        ("TR", "Çorum"), ("TR", "Düzce"), ("TR", "Elazığ"), ("TR", "Giresun"),
        ("TR", "Iğdır"), ("TR", "Isparta"), ("TR", "Karabük"), ("TR", "Karaman"),
        ("TR", "Kilis"), ("TR", "Kırıkkale"), ("TR", "Kırklareli"), ("TR", "Kütahya"),
        ("TR", "Nevşehir"), ("TR", "Niğde"), ("TR", "Osmaniye"), ("TR", "Rize"),
        ("TR", "Siirt"), ("TR", "Sivas"), ("TR", "Şırnak"), ("TR", "Uşak"),
        ("TR", "Yalova"), ("TR", "Yozgat"),
        ("NL", "Amsterdam"), ("NL", "Rotterdam"), ("NL", "Den Haag"), ("NL", "Utrecht"),
        ("NL", "Eindhoven"), ("NL", "Tilburg"), ("NL", "Groningen"), ("NL", "Almere"),
        ("NL", "Breda"), ("NL", "Nijmegen"), ("NL", "Enschede"), ("NL", "Arnhem"),
        ("NL", "Haarlem"), ("NL", "Amersfoort"), ("NL", "Apeldoorn"), ("NL", "Zaandam"),
        ("NL", "Hoofddorp"), ("NL", "Maastricht"), ("NL", "Leiden"), ("NL", "Dordrecht"),
        ("NL", "Zoetermeer"), ("NL", "Zwolle"), ("NL", "Deventer"), ("NL", "Delft"),
        ("NL", "Alkmaar"), ("NL", "Heerlen"), ("NL", "Venlo"), ("NL", "Leeuwarden"),
        ("NL", "Emmen"), ("NL", "Helmond"), ("NL", "Ede"), ("NL", "Hilversum"),
        ("NL", "Gouda"), ("NL", "Roosendaal"), ("NL", "Almelo"), ("NL", "Lelystad"),
        ("NL", "Hoorn"), ("NL", "Purmerend"), ("NL", "Schiedam"), ("NL", "Vlaardingen"),
    ];

    private bool _seeded = false;

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;
        await InitAsync(_tableCreationSql);

        // Read count in isolated scope so the reader/connection is released before inserting
        int existingCount = 0;
        {
            using var countReader = await GetSqliteReaderAsync(_tableCreationSql,
                "SELECT COUNT(*) FROM Countries", []);
            if (await countReader.ReadAsync())
                existingCount = Convert.ToInt32(countReader[0]);
        }

        if (existingCount > 0)
        {
            _seeded = true;
            return;
        }

        foreach (var (code, name) in _seedCountries)
        {
            SqliteCommand? cmd = null;
            try
            {
                cmd = await GetSqliteCommandAsync(_tableCreationSql,
                    "INSERT OR IGNORE INTO Countries(Code, Name) VALUES(@Code, @Name)");
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@Name", name);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e) { Logger.LogError(e, "Error seeding country {code}", code); }
            finally { Disposer.Dispose(ref cmd); }
        }

        foreach (var (countryCode, cityName) in _seedCities)
        {
            SqliteCommand? cmd = null;
            try
            {
                cmd = await GetSqliteCommandAsync(_tableCreationSql,
                    "INSERT INTO Cities(CountryId, Name) SELECT Id, @City FROM Countries WHERE Code=@Code");
                cmd.Parameters.AddWithValue("@City", cityName);
                cmd.Parameters.AddWithValue("@Code", countryCode);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e) { Logger.LogError(e, "Error seeding city {city}", cityName); }
            finally { Disposer.Dispose(ref cmd); }
        }

        _seeded = true;
    }

    public async Task<IReadOnlyList<CountryItem>> GetCountriesAsync()
    {
        var result = new List<CountryItem>();
        try
        {
            await EnsureSeededAsync();
            using var reader = await GetSqliteReaderAsync(_tableCreationSql,
                "SELECT Id, Code, Name FROM Countries ORDER BY Name", []);
            while (await reader.ReadAsync())
            {
                result.Add(new CountryItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Code = reader["Code"].ToString()!,
                    Name = reader["Name"].ToString()!
                });
            }
            return result.AsReadOnly();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error fetching countries");
            return result.AsReadOnly();
        }
    }

    public async Task<IReadOnlyList<CityItem>> GetCitiesByCountryIdAsync(int countryId)
    {
        var result = new List<CityItem>();
        try
        {
            await EnsureSeededAsync();
            var parameters = new List<SqliteParameter> { new("@CountryId", countryId) };
            using var reader = await GetSqliteReaderAsync(_tableCreationSql,
                "SELECT Id, CountryId, Name FROM Cities WHERE CountryId=@CountryId ORDER BY Name",
                parameters);
            while (await reader.ReadAsync())
            {
                result.Add(new CityItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    CountryId = Convert.ToInt32(reader["CountryId"]),
                    Name = reader["Name"].ToString()!
                });
            }
            return result.AsReadOnly();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error fetching cities for country {id}", countryId);
            return result.AsReadOnly();
        }
    }
}
