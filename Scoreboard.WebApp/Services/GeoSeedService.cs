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

    public async Task EnsureAsync(int organizationId)
    {
        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == organizationId);
        var home = CountryCatalog.Find(organization.CountryCode) ?? CountryCatalog.Find("TR")!;
        var homeName = home.NameIn(organization.Language);

        if (!await db.CountrySet.AnyAsync(c => c.OrganizationId == organization.Id))
        {
            var country = new Country { OrganizationId = organization.Id, Name = homeName };
            db.CountrySet.Add(country);
            var cities = home.Code switch { "TR" => TurkeyCities, "NL" => NetherlandsCities, _ => [] };
            db.CitySet.AddRange(cities.Select(n => new City { OrganizationId = organization.Id, Country = country, Name = n }));
            await db.SaveChangesAsync();
        }

        if (!await db.RegionSet.AnyAsync(r => r.OrganizationId == organization.Id))
        {
            var regions = home.Code switch { "TR" => TurkeyRegions, "NL" => NetherlandsRegions, _ => [] };
            db.RegionSet.AddRange(regions.Select(n => new Region { OrganizationId = organization.Id, Name = n }));
            await db.SaveChangesAsync();
        }

        await LinkExistingPlayersAsync(organization);
    }

    public async Task EnsureAllAsync()
    {
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

        var countries = await db.CountrySet.Where(c => c.OrganizationId == organization.Id && c.DeletedAt == null).ToListAsync();
        var cities = await db.CitySet.Where(c => c.OrganizationId == organization.Id && c.DeletedAt == null).ToListAsync();
        var compare = CultureInfo.InvariantCulture.CompareInfo;
        bool Same(string? a, string? b) => a is not null && b is not null &&
            compare.Compare(a.Trim(), b.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;

        foreach (var player in players)
        {
            var country = countries.FirstOrDefault(c => Same(c.Name, player.Nationality));
            if (country is null && CountryCatalog.All.FirstOrDefault(e => e.HasName(player.Nationality)) is { } entry)
            {
                var wanted = entry.NameIn(organization.Language);
                country = countries.FirstOrDefault(c => Same(c.Name, wanted));
            }

            if (country is null)
            {
                continue;
            }

            player.CountryId = country.Id;
            player.Nationality = country.Name;
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
