using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class GeoSeedService(DataContext db, OrganizationRunner runner)
{
    private static readonly string[] TurkeyCities = ["Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya", "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane", "Hakkâri", "Hatay", "Iğdır", "Isparta", "İstanbul", "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kırıkkale", "Kırklareli", "Kırşehir", "Kilis", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa", "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye", "Rize", "Sakarya", "Samsun", "Siirt", "Sinop", "Sivas", "Şanlıurfa", "Şırnak", "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"];

    private static readonly string[] NetherlandsCities = ["Amsterdam", "Rotterdam", "Den Haag", "Utrecht", "Eindhoven", "Groningen", "Tilburg", "Almere", "Breda", "Nijmegen", "Apeldoorn", "Haarlem", "Arnhem", "Enschede", "Amersfoort", "Zaandam", "'s-Hertogenbosch", "Hoofddorp", "Zwolle", "Zoetermeer", "Leiden", "Maastricht", "Dordrecht", "Ede", "Alphen aan den Rijn", "Naaldwijk", "Alkmaar", "Emmen", "Delft", "Venlo", "Deventer", "Sittard", "Leeuwarden", "Amstelveen", "Hilversum", "Heerlen", "Oss", "Roosendaal", "Helmond", "Lelystad", "Purmerend", "Gouda", "Schiedam", "Vlaardingen", "Hoorn", "Spijkenisse", "Kerkrade", "Bergen op Zoom", "Almelo", "Assen", "Middelburg", "Vlissingen", "Den Helder", "Hengelo", "Nieuwegein", "Veenendaal", "Capelle aan den IJssel", "Heerhugowaard", "Katwijk", "Woerden", "Zutphen", "Doetinchem", "Roermond", "Weert", "Zeist", "Harderwijk", "Gorinchem", "Hoogeveen", "Meppel", "Sneek", "Terneuzen", "Goes", "Tiel", "Culemborg", "Barneveld", "Volendam", "Bussum", "Naarden", "Wageningen", "Ermelo", "Kampen", "Harlingen"];

    private static readonly string[] TurkeyRegions = ["Marmara", "Ege", "Akdeniz", "İç Anadolu", "Karadeniz", "Doğu Anadolu", "Güneydoğu Anadolu"];

    private static readonly string[] NetherlandsRegions = ["Groningen", "Friesland", "Drenthe", "Overijssel", "Flevoland", "Gelderland", "Utrecht", "Noord-Holland", "Zuid-Holland", "Zeeland", "Noord-Brabant", "Limburg"];

    public async Task EnsureDefinitionsAsync()
    {
        foreach (var entry in CountryCatalog.All)
        {
            var country = await db.CountrySet.FirstOrDefaultAsync(c => c.Code == entry.Code);
            if (country is null)
            {
                country = new Country { Code = entry.Code, Name = entry.En };
                db.CountrySet.Add(country);
                await db.SaveChangesAsync();
            }

            var cityNames = entry.Code switch { "TR" => TurkeyCities, "NL" => NetherlandsCities, _ => [] };
            var existingCities = (await db.CitySet.Where(c => c.CountryId == country.Id).Select(c => c.Name).ToListAsync()).ToHashSet();
            db.CitySet.AddRange(cityNames.Where(n => !existingCities.Contains(n)).Select(n => new City { CountryId = country.Id, Name = n }));

            var regionNames = entry.Code switch { "TR" => TurkeyRegions, "NL" => NetherlandsRegions, _ => [] };
            var existingRegions = (await db.RegionSet.Where(r => r.CountryId == country.Id).Select(r => r.Name).ToListAsync()).ToHashSet();
            db.RegionSet.AddRange(regionNames.Where(n => !existingRegions.Contains(n)).Select(n => new Region { CountryId = country.Id, Name = n }));

            await db.SaveChangesAsync();
        }
    }

    public async Task EnsureAsync(int organizationId)
    {
        await EnsureDefinitionsAsync();
        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == organizationId);
        var home = CountryCatalog.Find(organization.CountryCode) ?? CountryCatalog.Find("TR")!;

        if (!await db.OrganizationCountrySet.AnyAsync(x => x.OrganizationId == organization.Id))
        {
            var country = await db.CountrySet.FirstAsync(c => c.Code == home.Code);
            db.OrganizationCountrySet.Add(new OrganizationCountry { OrganizationId = organization.Id, CountryId = country.Id });
            db.OrganizationCitySet.AddRange(await db.CitySet.Where(c => c.CountryId == country.Id)
                .Select(c => new OrganizationCity { OrganizationId = organization.Id, CityId = c.Id }).ToListAsync());
            db.OrganizationRegionSet.AddRange(await db.RegionSet.Where(r => r.CountryId == country.Id)
                .Select(r => new OrganizationRegion { OrganizationId = organization.Id, RegionId = r.Id }).ToListAsync());
            await db.SaveChangesAsync();
        }

        await LinkExistingPlayersAsync(organization);
    }

    public async Task EnsureAllAsync()
    {
        await EnsureDefinitionsAsync();
        foreach (var organizationId in await db.OrganizationSet.Where(o => o.DeletedAt == null).Select(o => o.Id).ToListAsync())
        {
            await runner.RunAsync<GeoSeedService>(organizationId, seed => seed.EnsureAsync(organizationId));
        }
    }

    private async Task LinkExistingPlayersAsync(Organization organization)
    {
        var players = await db.PlayerSet
            .Where(p => p.CreatedInOrganizationId == organization.Id && p.CountryId == null && p.Nationality != null && p.Nationality != "")
            .ToListAsync();
        if (players.Count == 0)
        {
            return;
        }

        var countries = await db.OrganizationCountrySet.Where(x => x.DeletedAt == null).Select(x => x.Country).ToListAsync();
        var cities = await db.OrganizationCitySet.Where(x => x.DeletedAt == null).Select(x => x.City).ToListAsync();
        var compare = CultureInfo.InvariantCulture.CompareInfo;
        bool Same(string? a, string? b) => a is not null && b is not null &&
            compare.Compare(a.Trim(), b.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;

        foreach (var player in players)
        {
            var code = CountryCatalog.All.FirstOrDefault(e => e.HasName(player.Nationality))?.Code;
            var country = code is null ? null : countries.FirstOrDefault(c => c.Code == code);
            if (country is null)
            {
                continue;
            }

            player.CountryId = country.Id;
            player.Nationality = CountryCatalog.Find(country.Code)!.NameIn(organization.Language);
            var city = cities.FirstOrDefault(c => c.CountryId == country.Id && Same(c.Name, player.City));
            if (city is not null)
            {
                player.CityId = city.Id;
                player.City = city.Name;
            }
        }

        await db.SaveChangesAsync();
    }
}
