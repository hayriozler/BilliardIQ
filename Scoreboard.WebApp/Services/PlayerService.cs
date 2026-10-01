using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class PlayerService(DataContext db, IWebHostEnvironment env)
{
    public Task<List<Player>> ListForOrganizationAsync(int organizationId) =>
        db.PlayerSet
            .Include(p => p.Association)
            .Include(p => p.Region)
            .Include(p => p.CountryRef)
            .Include(p => p.CityRef)
            .Where(p => (p.CreatedInOrganizationId == organizationId || p.IsSystem) && p.DeletedAt == null)
            .OrderBy(p => p.DisplayName)
            .ToListAsync();

    public Task<Player?> GetAsync(int organizationId, int id) =>
        db.PlayerSet.FirstOrDefaultAsync(p =>
            p.Id == id && (p.CreatedInOrganizationId == organizationId || p.IsSystem) && p.DeletedAt == null);

    public async Task<Player> UpsertAsync(
        int organizationId,
        int id,
        string nickname,
        string name,
        int? avatarId,
        string email,
        Level level,
        string baseCountry,
        string baseCity,
        string? photoBase64,
        string? photoExtension,
        int? shortcutNumber = null,
        string? licenseNo = null,
        DateOnly? licenseValidUntil = null,
        int? associationId = null,
        int? regionId = null,
        int? countryId = null,
        int? cityId = null)
    {
        if (avatarId is < 0 || avatarId >= AvatarGenerator.Count)
        {
            throw new ArgumentException("Geçersiz avatar.");
        }

        Country? country = null;
        City? city = null;
        if (countryId is not null)
        {
            country = await db.OrganizationCountrySet
                .Where(x => x.CountryId == countryId && x.OrganizationId == organizationId && x.DeletedAt == null)
                .Select(x => x.Country)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException("Ülke bulunamadı.");
        }

        if (cityId is not null)
        {
            city = await db.OrganizationCitySet
                .Where(x => x.CityId == cityId && x.OrganizationId == organizationId && x.DeletedAt == null)
                .Select(x => x.City)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException("Şehir bulunamadı.");
            if (country is null || city.CountryId != country.Id)
            {
                throw new ArgumentException("Şehir seçilen ülkeye ait değil.");
            }
        }

        if (regionId is not null && !await db.OrganizationRegionSet.AnyAsync(x =>
                x.RegionId == regionId && x.OrganizationId == organizationId && x.DeletedAt == null))
        {
            throw new ArgumentException("Bölge bulunamadı.");
        }

        if (associationId is not null && !await db.AssociationSet.AnyAsync(a =>
                a.Id == associationId && a.OrganizationId == organizationId && a.DeletedAt == null))
        {
            throw new ArgumentException("Dernek / federasyon bulunamadı.");
        }

        licenseNo = string.IsNullOrWhiteSpace(licenseNo) ? null : licenseNo.Trim();
        if (licenseNo is { Length: > 50 })
        {
            throw new ArgumentException("Lisans no en fazla 50 karakter olabilir.");
        }

        name = name.Trim();
        if (name.Length == 0 && string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException("Ad veya takma ad gerekli.");
        }

        if (shortcutNumber is < 1 or > 9999)
        {
            throw new ArgumentException("Kısayol numarası 1-9999 arasında olmalı.");
        }

        if (shortcutNumber is not null && await db.PlayerSet.AnyAsync(p =>
                p.CreatedInOrganizationId == organizationId && p.ShortcutNumber == shortcutNumber && p.Id != id))
        {
            throw new LocalizedArgumentException("{0} numaralı kısayol başka bir oyuncuda kayıtlı.", shortcutNumber.Value);
        }

        var player = id != 0 ? await GetAsync(organizationId, id) : null;
        if (player is { IsSystem: true })
        {
            throw new ArgumentException("Sistem oyuncuları değiştirilemez.");
        }

        if (player is null)
        {
            player = new Player { CreatedInOrganizationId = organizationId };
            db.PlayerSet.Add(player);
        }

        var (first, last) = SplitName(name);
        player.FirstName = first;
        player.LastName = last;
        player.Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
        player.DisplayName = player.Nickname ?? name;
        player.ShortcutNumber = shortcutNumber;
        player.FederationLicenseNo = licenseNo;
        player.LicenseValidUntil = licenseValidUntil;
        player.AssociationId = associationId;
        player.RegionId = regionId;
        player.CountryId = country?.Id;
        player.CityId = city?.Id;
        if (country is not null)
        {
            player.Nationality = country.Name;
            player.City = city?.Name;
        }
        player.AvatarId = avatarId;
        player.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        player.Level = level;
        player.Nationality = string.IsNullOrWhiteSpace(baseCountry) ? null : baseCountry.Trim();
        player.City = string.IsNullOrWhiteSpace(baseCity) ? null : baseCity.Trim();

        await db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(photoBase64))
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(photoBase64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Geçersiz fotoğraf verisi.");
            }

            var extension = string.IsNullOrWhiteSpace(photoExtension) ? "jpg" : photoExtension.TrimStart('.');
            var folder = Path.Combine(env.WebRootPath, "Players", organizationId.ToString());
            Directory.CreateDirectory(folder);
            var fileName = $"{player.Id}.{extension}";
            await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);
            player.PhotoUrl = $"Players/{organizationId}/{fileName}";

            await db.SaveChangesAsync();
        }

        return player;
    }

    public async Task<bool> DeleteAsync(int organizationId, int id)
    {
        var player = await GetAsync(organizationId, id);
        if (player is null)
        {
            return false;
        }

        if (player.IsSystem)
        {
            throw new InvalidOperationException("Sistem oyuncuları silinemez.");
        }

        db.TeamMemberSet.RemoveRange(await db.TeamMemberSet.Where(m => m.PlayerId == id).ToListAsync());
        player.ShortcutNumber = null;
        player.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private static (string First, string Last) SplitName(string name)
    {
        var index = name.LastIndexOf(' ');
        return index < 0 ? (name, "") : (name[..index].Trim(), name[(index + 1)..].Trim());
    }
}
