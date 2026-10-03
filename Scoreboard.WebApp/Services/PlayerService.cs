using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class PlayerService(DataContext db, IWebHostEnvironment env)
{
    public Task<List<Player>> ListForOrganizationAsync() =>
        db.PlayerSet
            .Include(p => p.Association)
            .Include(p => p.Region)
            .Include(p => p.CountryRef)
            .Include(p => p.CityRef)
            .Where(p => p.DeletedAt == null)
            .OrderBy(p => p.DisplayName)
            .ToListAsync();

    public Task<List<Player>> ListByIdsAsync(IReadOnlyCollection<int> ids) =>
        db.PlayerSet.AsNoTracking()
            .Include(p => p.Association)
            .Include(p => p.Region)
            .Include(p => p.CountryRef)
            .Include(p => p.CityRef)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

    public Task<Dictionary<int, string?>> AccountEmailsAsync() =>
        db.PlayerSet.AsNoTracking()
            .Where(p => p.UserId != null && p.DeletedAt == null)
            .Select(p => new { p.Id, p.User!.Email })
            .ToDictionaryAsync(x => x.Id, x => x.Email);

    public Task<Player?> GetAsync(int id) =>
        db.PlayerSet.FirstOrDefaultAsync(p =>
            p.Id == id && p.DeletedAt == null);

    public async Task<Player> UpsertAsync(int id,
        string nickname,
        string name,
        int? avatarId,
        string email,
        Level level,
        string baseCountry,
        string baseCity,
        string? photoBase64,
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
                .Where(x => x.CountryId == countryId && x.DeletedAt == null)
                .Select(x => x.Country)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException("Ülke bulunamadı.");
        }

        if (cityId is not null)
        {
            city = await db.OrganizationCitySet
                .Where(x => x.CityId == cityId && x.DeletedAt == null)
                .Select(x => x.City)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException("Şehir bulunamadı.");
            if (country is null || city.CountryId != country.Id)
            {
                throw new ArgumentException("Şehir seçilen ülkeye ait değil.");
            }
        }

        if (regionId is not null && !await db.OrganizationRegionSet.AnyAsync(x =>
                x.RegionId == regionId && x.DeletedAt == null))
        {
            throw new ArgumentException("Bölge bulunamadı.");
        }

        if (associationId is not null && !await db.AssociationSet.AnyAsync(a =>
                a.Id == associationId && a.DeletedAt == null))
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
                p.ShortcutNumber == shortcutNumber && p.Id != id))
        {
            throw new LocalizedArgumentException("{0} numaralı kısayol başka bir oyuncuda kayıtlı.", shortcutNumber.Value);
        }

        var player = id != 0 ? await GetAsync(id) : null;
        if (id != 0 && player is null)
        {
            throw new ArgumentException("Oyuncu bulunamadı.");
        }

        if (player is { IsSystem: true })
        {
            throw new ArgumentException("Sistem oyuncuları değiştirilemez.");
        }

        if (player is null)
        {
            player = new Player { CreatedInOrganizationId = db.CurrentOrganizationId };
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
        player.AvatarId = avatarId;
        player.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        player.Level = level;
        player.Nationality = string.IsNullOrWhiteSpace(baseCountry) ? null : baseCountry.Trim();
        player.City = string.IsNullOrWhiteSpace(baseCity) ? null : baseCity.Trim();

        await db.SaveUniqueAsync("ShortcutNumber", () => new LocalizedArgumentException("{0} numaralı kısayol başka bir oyuncuda kayıtlı.", shortcutNumber!.Value));

        await SavePhotoAsync(player, photoBase64);

        return player;
    }

    public async Task<Player> UpdateOwnProfileAsync(int playerId, string name, string? nickname, int? avatarId, string? photoBase64)
    {
        if (avatarId is < 0 || avatarId >= AvatarGenerator.Count)
        {
            throw new ArgumentException("Geçersiz avatar.");
        }

        name = name.Trim();
        if (name.Length == 0 && string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException("Ad veya takma ad gerekli.");
        }

        var player = await GetAsync(playerId) ?? throw new ArgumentException("Oyuncu bulunamadı.");
        var (first, last) = SplitName(name);
        player.FirstName = first;
        player.LastName = last;
        player.Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim();
        player.DisplayName = player.Nickname ?? name;
        player.AvatarId = avatarId;
        await db.SaveChangesAsync();
        await SavePhotoAsync(player, photoBase64);
        return player;
    }

    private const int MaxPhotoBytes = 5 * 1024 * 1024;

    private static string? DetectImageExtension(byte[] bytes) => bytes switch
    {
        [0xFF, 0xD8, 0xFF, ..] => "jpg",
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => "png",
        [0x47, 0x49, 0x46, 0x38, 0x37 or 0x39, 0x61, ..] => "gif",
        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => "webp",
        _ => null
    };

    private async Task SavePhotoAsync(Player player, string? photoBase64)
    {
        if (string.IsNullOrEmpty(photoBase64))
        {
            return;
        }

        if (photoBase64.Length > MaxPhotoBytes / 3 * 4 + 4)
        {
            throw new ArgumentException("Fotoğraf en fazla 5 MB olabilir.");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(photoBase64);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Geçersiz fotoğraf verisi.");
        }

        if (bytes.Length > MaxPhotoBytes)
        {
            throw new ArgumentException("Fotoğraf en fazla 5 MB olabilir.");
        }

        var extension = DetectImageExtension(bytes) ?? throw new ArgumentException("Fotoğraf JPEG, PNG, GIF veya WebP biçiminde olmalı.");
        var folder = Path.Combine(env.WebRootPath, "Players", db.CurrentOrganizationId.ToString());
        Directory.CreateDirectory(folder);
        foreach (var previous in Directory.GetFiles(folder, $"{player.Id}.*"))
        {
            File.Delete(previous);
        }

        var fileName = $"{player.Id}.{extension}";
        await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);
        player.PhotoUrl = $"Players/{db.CurrentOrganizationId}/{fileName}";

        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var player = await GetAsync(id);
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
