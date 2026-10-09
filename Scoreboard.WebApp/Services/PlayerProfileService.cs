using Microsoft.EntityFrameworkCore;
using Scoreboard.Common;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public record PlayerDetailsUpdate(
    string? FirstName, string? LastName, string? Nickname, int? AvatarId, string? PhotoBase64, bool RemovePhoto,
    Level? Level, string? LicenseNo, DateOnly? LicenseValidUntil,
    string? Phone, string? Locale,
    int? CountryId = null, int? RegionId = null, int? CityId = null, int? AssociationId = null);

public record MobileFullProfile(
    int Id, string FirstName, string LastName, string? Nickname, string DisplayName, int? AvatarId, string? PhotoUrl,
    Level Level, int? ShortcutNumber,
    string? LicenseNo, DateOnly? LicenseValidUntil, string? AssociationName, string? RegionName, string? CountryName, string? CityName,
    string? Email, string? Phone, string? Locale, bool HasAccount, IReadOnlyList<string> Clubs, IReadOnlyList<string> Teams,
    int? CountryId, int? RegionId, int? CityId, int? AssociationId);

public class PlayerProfileService(DataContext db, PlayerService players, MobileAuthService mobile)
{
    public async Task<MobileFullProfile?> GetAsync(int playerId)
    {
        var player = await db.PlayerSet.AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Association)
            .Include(p => p.Region)
            .Include(p => p.CountryRef)
            .Include(p => p.CityRef)
            .FirstOrDefaultAsync(p => p.Id == playerId && p.DeletedAt == null && !p.IsSystem);
        if (player is null)
        {
            return null;
        }

        var clubs = await db.Set<ClubMembership>().AsNoTracking()
            .Where(m => m.PlayerId == playerId && m.LeftAt == null)
            .Select(m => m.Club.Name)
            .ToListAsync();
        var teams = await db.TeamMemberSet.AsNoTracking()
            .Where(m => m.PlayerId == playerId && m.LeftAt == null)
            .Select(m => m.Team.Name)
            .ToListAsync();

        return new MobileFullProfile(
            player.Id, player.FirstName, player.LastName, player.Nickname, player.DisplayName, player.AvatarId, player.PhotoUrl,
            player.Level, player.ShortcutNumber,
            player.FederationLicenseNo, player.LicenseValidUntil, player.Association?.Name, player.Region?.Name, player.CountryRef?.Name, player.CityRef?.Name,
            player.User?.Email ?? player.Email, player.User?.Phone, player.User?.Locale, player.UserId is not null, clubs, teams,
            player.CountryId, player.RegionId, player.CityId, player.AssociationId);
    }

    public async Task<MobileFullProfile> UpdateAsync(int playerId, PlayerDetailsUpdate update, bool canChangeName)
    {
        if (update.AvatarId is < 0 || update.AvatarId >= AvatarGenerator.Count)
        {
            throw new ArgumentException("Geçersiz avatar.");
        }

        if (update.Level is { } level && !Enum.IsDefined(level))
        {
            throw new ArgumentException("Geçersiz seviye.");
        }

        if (update.LicenseNo is { Length: > 50 })
        {
            throw new ArgumentException("Lisans numarası en fazla 50 karakter olabilir.");
        }

        if (update.Nickname is { Length: > 50 })
        {
            throw new ArgumentException("Takma ad en fazla 50 karakter olabilir.");
        }

        Country? country = null;
        City? city = null;
        if (update.CountryId is not null)
        {
            country = await db.OrganizationCountrySet
                .Where(x => x.CountryId == update.CountryId && x.DeletedAt == null)
                .Select(x => x.Country)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException("Ülke bulunamadı.");
        }

        if (update.CityId is not null)
        {
            city = await db.OrganizationCitySet
                .Where(x => x.CityId == update.CityId && x.DeletedAt == null)
                .Select(x => x.City)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException("Şehir bulunamadı.");
            if (country is null || city.CountryId != country.Id)
            {
                throw new ArgumentException("Şehir seçilen ülkeye ait değil.");
            }
        }

        if (update.RegionId is not null && !await db.OrganizationRegionSet.AnyAsync(x =>
                x.RegionId == update.RegionId && x.DeletedAt == null))
        {
            throw new ArgumentException("Bölge bulunamadı.");
        }

        if (update.AssociationId is not null && !await db.AssociationSet.AnyAsync(a =>
                a.Id == update.AssociationId && a.DeletedAt == null))
        {
            throw new ArgumentException("Dernek / federasyon bulunamadı.");
        }

        var player = await players.GetAsync(playerId);
        if (player is null || player.IsSystem)
        {
            throw new ArgumentException("Oyuncu bulunamadı.");
        }

        if (canChangeName && (update.FirstName is not null || update.LastName is not null))
        {
            var first = (update.FirstName ?? player.FirstName).Trim();
            var last = (update.LastName ?? player.LastName).Trim();
            if (first.Length == 0 && last.Length == 0)
            {
                throw new ArgumentException("Ad gerekli.");
            }

            player.FirstName = first;
            player.LastName = last;
        }

        player.Nickname = string.IsNullOrWhiteSpace(update.Nickname) ? null : update.Nickname.Trim();
        player.DisplayName = player.Nickname ?? $"{player.FirstName} {player.LastName}".Trim();
        player.AvatarId = update.AvatarId;
        player.Level = update.Level ?? player.Level;
        player.FederationLicenseNo = string.IsNullOrWhiteSpace(update.LicenseNo) ? null : update.LicenseNo.Trim();
        player.LicenseValidUntil = update.LicenseValidUntil;
        player.CountryId = country?.Id;
        player.CityId = city?.Id;
        player.RegionId = update.RegionId;
        player.AssociationId = update.AssociationId;
        await db.SaveChangesAsync();

        if (update.RemovePhoto)
        {
            await players.RemovePhotoAsync(player);
        }
        else
        {
            await players.SavePhotoAsync(player, update.PhotoBase64);
        }

        if (player.UserId is int userId && (update.Phone is not null || update.Locale is not null))
        {
            await mobile.UpdateProfileAsync(userId, null, update.Locale, update.Phone);
        }

        return await GetAsync(playerId) ?? throw new ArgumentException("Oyuncu bulunamadı.");
    }
}
